using Elements.Core;
using FrooxEngine.UIX;
using ResoniteMetricsCounter.Metrics;
using System;

namespace ResoniteMetricsCounter.UIX.Pages;

internal interface IMetricsPage : IDisposable
{
    public struct ColumnDefinition
    {
        public string Label;
        public float FlexWidth;
        public float MinWidth;
        public Alignment Alignment;

        public ColumnDefinition(string label, Alignment alignment = Alignment.MiddleLeft, float flexWidth = -1, float minWidth = -1)
        {
            Label = label;
            FlexWidth = flexWidth;
            MinWidth = minWidth;
            Alignment = alignment;
        }
    }

    bool IsActive();
    void BuildUI(UIBuilder uiBuilder);
    void Update(in MetricsCounter metricsCounter, int maxItems);
}
