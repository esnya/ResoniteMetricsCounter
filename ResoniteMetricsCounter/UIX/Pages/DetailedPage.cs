using FrooxEngine;
using FrooxEngine.ProtoFlux;
using ResoniteMetricsCounter.Metrics;
using ResoniteMetricsCounter.UIX.Item;
using ResoniteMetricsCounter.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ResoniteMetricsCounter.UIX.Pages;

internal sealed class DetailedPage : MetricsPageBase
{
    private sealed class Item : MetricPageItemBase<Metric<IWorldElement>>
    {
        public Item(Slot container, List<MetricColumnDefinition> columns) : base(container, columns)
        {
        }

        protected override IWorldElement? GetReference(in Metric<IWorldElement> metric)
        {
            var slot = metric.Target.GetSlotFast();
            return metric.Target is ProtoFluxNode ? slot?.Parent : slot;
        }

        protected override long GetTicks(in Metric<IWorldElement> metric)
        {
            return metric.Ticks;
        }
        protected override void UpdateColumn(
            in Metric<IWorldElement> metric,
            Sync<string> column,
            int i,
            long maxTicks,
            long totalTicks,
            long frameCount)
        {
            switch (i)
            {
                case 0:
                    column.Value = metric.Target.GetMetricObjectRoot()?.Name!;
                    break;
                case 1:
                    column.Value = metric.Target.GetSlotFast()?.Parent?.Name!;
                    break;
                case 2:
                    column.Value = GetReference(metric)?.Name!;
                    break;
                case 3:
                    column.Value = metric.Target is ProtoFluxNode node ? node.Group.Name : metric.Target.Name;
                    break;
                case 4:
                    column.Value = $"{metric.Stage}";
                    break;
                case 5:
                    column.Value = $"{1000.0 * metric.Ticks / Stopwatch.Frequency / frameCount:0.000}ms";
                    break;
                case 6:
                    column.Value = $"{(double)metric.Ticks / totalTicks:0.000%}";
                    break;
            }
        }
    }

    protected override List<MetricColumnDefinition> Columns => new() {
            new("Object Root", flexWidth: Constants.FLEX),
            new("Parent", flexWidth: Constants.FLEX),
            new("Slot", flexWidth: Constants.FLEX),
            new("Component", flexWidth: Constants.FLEX),
            new("Stage", flexWidth: Constants.FLEX),
            new("Time", minWidth: Constants.FIXEDWIDTH),
            new("%", minWidth: Constants.FIXEDWIDTH),
    };

    private List<Item?>? items;
    public override void Update(in MetricsCounter metricsCounter, int maxItems)
    {
        if (container is null || container.IsDisposed)
        {
            return;
        }

        var maxTicks = metricsCounter.ByElement.Max;
        var totalTicks = metricsCounter.ByElement.Total;
        var frameCount = metricsCounter.FrameCount;

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
            var item = items[i] ?? (items[i] = new Item(container, Columns));

            if (!item.Update(metric, maxTicks, totalTicks, frameCount))
            {
                metricsCounter.ByElement.Remove(metric.Target);
            }
            i++;
        }
    }
}
