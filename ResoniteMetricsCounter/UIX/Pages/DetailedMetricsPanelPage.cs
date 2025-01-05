using FrooxEngine;
using FrooxEngine.UIX;
using ResoniteMetricsCounter.Metrics;
using ResoniteMetricsCounter.UIX.Item;
using ResoniteMetricsCounter.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ResoniteMetricsCounter.UIX.Pages;

internal sealed class DetailedMetricsPanelPage : IMetricsPage
{
    private static List<IMetricsPage.ColumnDefinition> Columns => new() {
            new("Object Root", flexWidth: 1.0f),
            new("Parent", flexWidth: 1.0f),
            new("Slot", flexWidth: 1.0f),
            new("Component", flexWidth: 1.0f),
            new("Stage", flexWidth: 0.5f),
            new("Time", minWidth: 32*3),
            new("%", minWidth: 32*3),
    };

    private sealed class Item : MetricItemBase<StageMetric<IWorldElement>>
    {
        protected override List<IMetricsPage.ColumnDefinition> Columns => DetailedMetricsPanelPage.Columns;

        public Item(Slot container) : base(container)
        {
        }

        protected override IWorldElement? GetReference(in StageMetric<IWorldElement> metric)
        {
            return metric.Target.GetSlotFast();
        }

        protected override long GetTicks(in StageMetric<IWorldElement> metric)
        {
            return metric.Ticks;
        }
        protected override void UpdateColumn(in StageMetric<IWorldElement> metric, Sync<string> column, int i, long maxTicks, long totalTicks, long frameCount)
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
                    column.Value = metric.Target.GetSlotFast()?.Name!;
                    break;
                case 3:
                    column.Value = metric.Target.Name;
                    break;
                case 4:
                    column.Value = $"{metric.Stage}";
                    break;
                case 5:
                    column.Value = $"{1000.0 * metric.Ticks / Stopwatch.Frequency / frameCount:0.00}ms";
                    break;
                case 6:
                    column.Value = $"{(double)metric.Ticks / totalTicks:0.000%}";
                    break;
            }
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

        uiBuilder.PushStyle();

        var hori = uiBuilder.HorizontalLayout();
        hori.Slot.OrderOffset = long.MinValue;

        foreach (var column in Columns)
        {
            uiBuilder.Style.FlexibleWidth = column.FlexWidth;
            uiBuilder.Style.MinWidth = column.MinWidth;
            uiBuilder.Text(column.Label);
        }
        uiBuilder.NestOut();

        uiBuilder.PopStyle();
    }

    public void Update(in MetricsCounter metricsCounter, int maxItems)
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
            var item = items[i] ?? (items[i] = new Item(container));

            if (!item.Update(metric, maxTicks, totalTicks, frameCount))
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
