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
    private sealed class Item : MetricItemBase<StageMetric<IWorldElement>>
    {
        public Item(Slot container) : base(container)
        {
        }

        protected override string GetLabel(in StageMetric<IWorldElement> metric)
        {
            var target = metric.Target;
            var slot = target.GetSlotFast();
            var parent = slot?.Parent;
            var objectRoot = slot?.GetExactObjectRootOrWorldRootFast() ?? target.World.RootSlot;

            if (parent is null)
            {
                return $"[{metric.Stage}] {objectRoot.GetNameFast()}/../{slot?.GetNameFast()}.{target?.GetNameFast()}";
            }
            return $"[{metric.Stage}] {objectRoot.GetNameFast()}/../{parent.GetNameFast()}/{slot?.GetNameFast()}.{target?.GetNameFast()}";
        }

        protected override IWorldElement GetReference(in StageMetric<IWorldElement> metric)
        {
            return metric.Target;
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
        if (container is null || container.IsDisposed) return;

        var maxTicks = metricsCounter.ByElement.Max;
        var totalTicks = metricsCounter.ByElement.Total;

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
            var item = items[i] ?? (items[i] = new Item(container!));

            if (!item.Update(metric, maxTicks, totalTicks))
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
