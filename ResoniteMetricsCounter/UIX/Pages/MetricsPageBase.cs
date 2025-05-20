using FrooxEngine;
using FrooxEngine.UIX;
using ResoniteMetricsCounter.Metrics;
using System;
using System.Collections.Generic;

namespace ResoniteMetricsCounter.UIX.Pages;

internal abstract class MetricsPageBase : IDisposable
{
    protected abstract List<MetricColumnDefinition> Columns { get; }

    public abstract void Update(in MetricsCounter metricsCounter, int maxItems);

    protected Slot? container { get; private set; }
    public bool IsActive()
    {
        return container?.IsActive ?? false;
    }

    public void BuildUI(UIBuilder uiBuilder)
    {
        container = uiBuilder.VerticalLayout(RadiantUI_Constants.GRID_PADDING).Slot;
        container.Disposing += _ => container = null;

        foreach (var _ in MetricColumnDefinition.Build(uiBuilder, Columns, static c => c.Slot.OrderOffset = long.MinValue))
        {

        }

        uiBuilder.NestOut();
    }

    public void Dispose()
    {
        container?.Dispose();
    }
}

