using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FrooxEngine;
using ResoniteMetricsCounter.Metrics;
using ResoniteMetricsCounter.UIX.Item;

namespace ResoniteMetricsCounter.UIX.Pages;

internal sealed class HierarchyPage : MetricsPageBase
{
    private sealed class Item : MetricPageItemBase<Metric<Slot>>
    {
        public Item(Slot container, List<MetricColumnDefinition> columns)
            : base(container, columns) { }

        protected override IWorldElement? GetReference(in Metric<Slot> metric)
        {
            return metric.Target;
        }

        protected override long GetTicks(in Metric<Slot> metric)
        {
            return metric.Ticks;
        }

        protected override void UpdateColumn(
            in Metric<Slot> metric,
            Sync<string> column,
            int i,
            long maxTicks,
            long totalTicks,
            long frameCount
        )
        {
            switch (i)
            {
                case 0:
                    column.Value = metric.Target.Name;
                    break;
                case 1:
                    column.Value =
                        $"{1000.0 * metric.Ticks / Stopwatch.Frequency / frameCount:0.000}ms";
                    break;
                case 2:
                    column.Value = $"{(double)metric.Ticks / maxTicks:0.00%}";
                    break;
            }
        }
    }

    protected override List<MetricColumnDefinition> Columns =>
        new()
        {
            new("Hierarchy", flexWidth: 1.0f),
            new("Time", minWidth: 32 * 3),
            new("%", minWidth: 32 * 3),
        };

    private readonly List<Item?> items = new();

    public override void Update(in MetricsCounter metricsCounter, int maxItems)
    {
        if (container is null || container.IsDisposed)
        {
            return;
        }

        if (items.Count < maxItems)
        {
            items.AddRange(Enumerable.Repeat<Item?>(null, maxItems - items.Count));
        }

        var maxTicks = metricsCounter.ByObjectRoot.Max;
        var totalTicks = metricsCounter.ByObjectRoot.Total;
        var frameCount = metricsCounter.FrameCount;

        int i = 0;
        foreach (
            var metric in metricsCounter
                .ByObjectRoot.Metrics.OrderByDescending(m => m.Ticks)
                .Take(maxItems)
        )
        {
            var item = items[i] ??= new Item(container, Columns);
            if (i == 0)
            {
                maxTicks = metric.Ticks;
            }

            if (!item.Update(metric, maxTicks, totalTicks, frameCount))
            {
                metricsCounter.ByObjectRoot.Remove(metric.Target);
            }

            i++;
        }
    }
}
