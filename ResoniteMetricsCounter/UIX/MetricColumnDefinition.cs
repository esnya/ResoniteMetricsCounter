using System;
using System.Collections.Generic;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using ResoniteMetricsCounter.Utils;

namespace ResoniteMetricsCounter.UIX;

internal struct MetricColumnDefinition
{
    public string Label;
    public float FlexWidth;
    public float MinWidth;
    public Alignment Alignment;

    public MetricColumnDefinition(
        string label,
        Alignment alignment = Alignment.MiddleLeft,
        float flexWidth = -1,
        float minWidth = -1
    )
    {
        Label = label;
        FlexWidth = flexWidth;
        MinWidth = minWidth;
        Alignment = alignment;
    }

    public static IEnumerable<Text> Build(
        UIBuilder uiBuilder,
        IEnumerable<MetricColumnDefinition> columns,
        Action<HorizontalLayout>? containerModifier = null
    )
    {
        uiBuilder.PushStyle();

        uiBuilder.Style.MinHeight = Constants.ROWHEIGHT;
        uiBuilder.Style.TextColor = RadiantUI_Constants.TEXT_COLOR;
        uiBuilder.Style.TextAlignment = Alignment.MiddleCenter;
        uiBuilder.Style.ForceExpandWidth = false;

        var horizontalLayout = uiBuilder.HorizontalLayout(Constants.SPACING, Constants.PADDING);
        containerModifier?.Invoke(horizontalLayout);

        foreach (var column in columns)
        {
            uiBuilder.Style.FlexibleWidth = column.FlexWidth;
            uiBuilder.Style.MinWidth = column.MinWidth;
            uiBuilder.Panel();
            yield return uiBuilder.Text(column.Label);
            uiBuilder.NestOut();
        }
        uiBuilder.NestOut();

        uiBuilder.PopStyle();
    }
}
