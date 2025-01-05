using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using ResoniteMetricsCounter.Metrics;
using ResoniteMetricsCounter.UIX.Item;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ResoniteMetricsCounter.UIX.Pages;

internal sealed class ObjectRootPage : IMetricsPage
{
    private static List<IMetricsPage.ColumnDefinition> Columns => new() {
        new ("Object Root", flexWidth: 1.0f),
        new ("Time", minWidth: 32 * 3),
        new ("%", minWidth: 32 * 3),
    };

    private sealed class Item : MetricItemBase<Metric<Slot>>
    {
        protected override List<IMetricsPage.ColumnDefinition> Columns => ObjectRootPage.Columns;

        public Item(Slot container) : base(container)
        {
        }


        protected override IWorldElement? GetReference(in Metric<Slot> metric)
        {
            return metric.Target;
        }
        protected override long GetTicks(in Metric<Slot> metric)
        {
            return metric.Ticks;
        }

        protected override void UpdateColumn(in Metric<Slot> metric, Sync<string> column, int i, long maxTicks, long totalTicks, long frameCount)
        {
            switch (i)
            {
                case 0:
                    column.Value = metric.Target.Name;
                    break;
                case 1:
                    column.Value = $"{1000.0 * metric.Ticks / Stopwatch.Frequency / frameCount:0.00}ms";
                    break;
                case 2:
                    column.Value = $"{(double)metric.Ticks / maxTicks:0.00%}";
                    break;
            }
        }
    }

    private Slot? container;
    private readonly List<Item?> items = new();


    public void BuildUI(UIBuilder uiBuilder)
    {
        container = uiBuilder.VerticalLayout(RadiantUI_Constants.GRID_PADDING).Slot;

        uiBuilder.PushStyle();

        var hori = uiBuilder.HorizontalLayout(RadiantUI_Constants.GRID_PADDING);
        hori.Slot.OrderOffset = long.MinValue;
        foreach (var column in Columns)
        {
            uiBuilder.Style.FlexibleWidth = column.FlexWidth;
            uiBuilder.Style.MinWidth = column.MinWidth;
            uiBuilder.Text(column.Label, alignment: Alignment.MiddleCenter);
        }
        uiBuilder.NestOut();

        uiBuilder.PopStyle();

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

        var maxTicks = metricsCounter.ByObjectRoot.Max;
        var totalTicks = metricsCounter.ByObjectRoot.Total;
        var frameCount = metricsCounter.FrameCount;

        int i = 0;
        foreach (var metric in metricsCounter.ByObjectRoot.Metrics.OrderByDescending(m => m.Ticks).Take(maxItems))
        {
            var item = items[i] ??= new Item(container!);
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
