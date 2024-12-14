using FrooxEngine;
using FrooxEngine.UIX;
using ResoniteMetricsCounter.Metrics;
using ResoniteMetricsCounter.UIX.Item;
using System.Collections.Generic;
using System.Linq;

namespace ResoniteMetricsCounter.UIX.Pages;

internal sealed class ObjectRootPage : IMetricsPage
{
    private struct ObjectRootMetrics
    {
        public Slot? ObjectRoot;
        public long Ticks;
    }

    private sealed class Item : MetricItemBase<ObjectRootMetrics>
    {
        public Item(Slot container) : base(container)
        {
        }
        protected override string? GetLabel(in ObjectRootMetrics metric)
        {
            return metric.ObjectRoot?.Name;
        }
        protected override Slot? GetReference(in ObjectRootMetrics metric)
        {
            return metric.ObjectRoot;
        }
        protected override long GetTicks(in ObjectRootMetrics metric)
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
        if (container is null || container.IsDisposed) return;

        if (items.Count < maxItems)
        {
            items.AddRange(Enumerable.Repeat<Item?>(null, maxItems - items.Count));
        }

        var metrics = from metric in metricsCounter.Metrics.Values
                      group metric by metric.ObjectRoot into g
                      select new ObjectRootMetrics
                      {
                          ObjectRoot = g.Key,
                          Ticks = g.Sum(a => a.Ticks)
                      } into om
                      orderby om.Ticks descending
                      select om;

        long maxTicks = 0;
        var totalTicks = metricsCounter.TotalTicks;

        int i = 0;
        foreach (var metric in metrics.Take(maxItems))
        {
            var item = items[i] ??= new Item(container!);
            if (i == 0) maxTicks = metric.Ticks;

            if (!item.Update(metric, maxTicks, metricsCounter.TotalTicks))
            {
                var toRemove = metricsCounter.Metrics.Values.FirstOrDefault(m => m.ObjectRoot == metric.ObjectRoot);
                metricsCounter.Remove(toRemove);
            }

            i++;
        }
    }
}
