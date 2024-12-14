using FrooxEngine;
using FrooxEngine.UIX;
using ResoniteMetricsCounter.Metrics;
using ResoniteMetricsCounter.UIX.Item;
using System.Collections.Generic;
using System.Linq;

namespace ResoniteMetricsCounter.UIX.Pages;

internal sealed class DetailedMetricsPanelPage : IMetricsPage
{
    private sealed class Item : MetricItemBase<Metric>
    {
        public Item(Slot container) : base(container)
        {
        }

        protected override string GetLabel(in Metric metric)
        {
            return metric.Label;
        }

        protected override Slot GetReference(in Metric metric)
        {
            return metric.Slot;
        }

        protected override long GetTicks(in Metric metric)
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

        var maxTicks = metricsCounter.MaxTicks;
        var totalTicks = metricsCounter.TotalTicks;

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
        foreach (var metric in metricsCounter.Metrics.Values.OrderByDescending(a => a.Ticks).Take(maxItems))
        {
            var item = items[i] ?? (items[i] = new Item(container!));

            if (!item.Update(metric, maxTicks, totalTicks))
            {
                metricsCounter.Remove(metric);
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
