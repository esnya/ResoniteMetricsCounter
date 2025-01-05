using FrooxEngine;
using FrooxEngine.UIX;
using ResoniteMetricsCounter.Metrics;
using ResoniteMetricsCounter.UIX.Item;
using ResoniteMetricsCounter.Utils;
using System.Collections.Generic;
using System.Linq;

namespace ResoniteMetricsCounter.UIX.Pages;

internal sealed class DetailedMetricsPanelPage : IMetricsPage
{
    private sealed class LabelValueCache : CachedValueBase<StageMetric<IWorldElement>, int, string>
    {
        protected override int GetKey(in StageMetric<IWorldElement> metric)
        {
            return metric.Stage.GetHashCode() ^ metric.Target.ReferenceID.GetHashCode();
        }

        protected override string GetValue(in StageMetric<IWorldElement> metric)
        {
            var target = metric.Target;
            var slot = target.GetSlotFast();
            var parent = slot?.Parent;
            var objectRoot = slot?.GetMetricObjectRoot() ?? target.World.RootSlot;

            if (parent is null)
            {
                return $"{objectRoot.GetNameFast()}/../{slot?.GetNameFast()}.{target?.GetNameFast()} [{metric.Stage}]";
            }

            if (slot == objectRoot)
            {
                return $"{objectRoot.GetNameFast()} {parent.GetNameFast()}/{slot?.GetNameFast()}.{target?.GetNameFast()} [{metric.Stage}]";
            }

            if (parent == objectRoot)
            {
                return $"{objectRoot.GetNameFast()}/{slot?.GetNameFast()}.{target?.GetNameFast()} [{metric.Stage}]";
            }
            return $"{objectRoot.GetNameFast()}/../{parent.GetNameFast()}/{slot?.GetNameFast()}.{target?.GetNameFast()} [{metric.Stage}]";
        }
    }

    private readonly LabelValueCache labelCache = new();

    private sealed class Item : MetricItemBase<StageMetric<IWorldElement>>
    {
        private readonly LabelValueCache labelCache;

        public Item(Slot container, LabelValueCache labelValueCache) : base(container)
        {
            labelCache = labelValueCache;
        }

        protected override string GetLabel(in StageMetric<IWorldElement> metric)
        {
            return labelCache.GetOrCache(metric);
        }

        protected override IWorldElement? GetReference(in StageMetric<IWorldElement> metric)
        {
            return metric.Target.GetSlotFast();
        }

        protected override long GetTicks(in StageMetric<IWorldElement> metric)
        {
            return metric.Ticks;
        }
    }

    private Slot? container;
    private List<Item?>? items;

    public bool IsActive()
    {
        return container?.IsActive ?? false;
    }

    public void BuildUI(UIBuilder uiBuilder)
    {
        container = uiBuilder.VerticalLayout(spacing: 8).Slot;
    }

    public void Update(in MetricsCounter metricsCounter, int maxItems)
    {
        if (container is null || container.IsDisposed)
        {
            return;
        }

        var maxTicks = metricsCounter.ByElement.Max;
        var elapsedTicks = metricsCounter.ElapsedTicks;

        if (items?.Count != maxItems)
        {
            if (items is null)
            {
                items = new(Enumerable.Repeat<Item?>(null, maxItems));
            }
            else
            {
                items.AddRange(Enumerable.Repeat<Item?>(null, maxItems - items.Count));
            }
        }

        var i = 0;
        foreach (var metric in metricsCounter.ByElement.Metrics.OrderByDescending(m => m.Ticks).Take(maxItems))
        {
            var item = items[i] ?? (items[i] = new Item(container!, labelCache));

            if (!item.Update(metric, maxTicks, elapsedTicks))
            {
                metricsCounter.ByElement.Remove(metric.Target);
            }
            i++;
        }
    }

    public void Dispose()
    {
        if (container is not null && !container.IsDisposed)
        {
            container.Destroy();
        }
    }
}
