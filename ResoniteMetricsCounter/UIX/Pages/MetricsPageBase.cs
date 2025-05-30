using System;
using System.Collections.Generic;
using FrooxEngine;
using FrooxEngine.UIX;
using ResoniteMetricsCounter.Metrics;

namespace ResoniteMetricsCounter.UIX.Pages;

internal abstract class MetricsPageBase : IDisposable
{
    protected abstract List<MetricColumnDefinition> Columns { get; }

    public abstract void Update(in MetricsCounter metricsCounter, int maxItems);

    protected Slot? Container { get; private set; }

    public bool IsActive()
    {
        return Container?.IsActive ?? false;
    }

    public void BuildUI(UIBuilder uiBuilder)
    {
        Container = uiBuilder.VerticalLayout(RadiantUI_Constants.GRID_PADDING).Slot;
        Container.Disposing += _ => Container = null;

        foreach (
            var _ in MetricColumnDefinition.Build(
                uiBuilder,
                Columns,
                static c => c.Slot.OrderOffset = long.MinValue
            )
        ) { }

        uiBuilder.NestOut();
    }

    public void Dispose()
    {
        Container?.Dispose();
    }
}
