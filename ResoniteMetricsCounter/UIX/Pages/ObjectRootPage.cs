using FrooxEngine;
using FrooxEngine.UIX;
using ResoniteMetricsCounter.Metrics;
using ResoniteMetricsCounter.UIX.Item;
using ResoniteMetricsCounter.Utils;
using System.Collections.Generic;
using System.Linq;

namespace ResoniteMetricsCounter.UIX.Pages;

internal sealed class ObjectRootPage : IMetricsPage
{
    private sealed class Item : MetricItemBase<Metric<Slot>>
    {
        public Item(Slot container) : base(container)
        {
        }
        protected override string GetLabel(in Metric<Slot> metric)
        {
            return $"{metric.Target.GetNameFast()}";
        }
        protected override IWorldElement? GetReference(in Metric<Slot> metric)
        {
            return metric.Target;
        }
        protected override long GetTicks(in Metric<Slot> metric)
        {
            return metric.Ticks;
        }
    }

    private Slot? container;
    private readonly List<Item?> items = new();


    public void BuildUI(UIBuilder uiBuilder)
    {
        container = uiBuilder.VerticalLayout(RadiantUI_Constants.GRID_PADDING).Slot;
        container.Destroyed += (_) => Dispose();
    }

    public void Dispose()
    {
        if (container is not null && !container.IsDisposed)
        {
            container.Destroy();
        }
    }

    public bool IsActive()
    {
        return container is not null && !container.IsDisposed && container.IsActive;
    }

    public void Update(in MetricsCounter metricsCounter, int maxItems)
    {
        if (container is null || container.IsDisposed)
        {
            return;
        }

        if (items.Count < maxItems)
        {
            items.AddRange(Enumerable.Repeat<Item?>(null, maxItems - items.Count));
        }

        long maxTicks = metricsCounter.ByObjectRoot.Max;
        var elapsedTicks = metricsCounter.ElapsedTicks;

        int i = 0;
        foreach (var metric in metricsCounter.ByObjectRoot.Metrics.OrderByDescending(m => m.Ticks).Take(maxItems))
        {
            var item = items[i] ??= new Item(container!);
            if (i == 0)
            {
                maxTicks = metric.Ticks;
            }

            if (!item.Update(metric, maxTicks, elapsedTicks))
            {
                metricsCounter.ByObjectRoot.Remove(metric.Target);
            }

            i++;
        }
    }
}
