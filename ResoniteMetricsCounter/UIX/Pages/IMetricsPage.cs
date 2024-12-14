using FrooxEngine.UIX;
using ResoniteMetricsCounter.Metrics;
using System;

namespace ResoniteMetricsCounter.UIX.Pages;

internal interface IMetricsPage : IDisposable
{
    bool IsActive();
    void BuildUI(UIBuilder uiBuilder);
    void Update(in MetricsCounter metricsCounter, int maxItems);
}
