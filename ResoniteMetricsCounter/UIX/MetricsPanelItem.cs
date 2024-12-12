using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using ResoniteMetricsCounter.Metrics;
using System.Diagnostics;

namespace ResoniteMetricsCounter.UIX;
internal sealed class MetricsPanelItem
{
    private const float DEFAULT_ITEM_SIZE = 32;
    private const float DEFAULT_PADDING = 4;

    private readonly Slot slot;
    private readonly Sync<string> labelField, timeField;
    private readonly Sync<colorX> metricTint;
    private readonly ReferenceProxySource referenceProxySource;
    private readonly RectTransform metricRect;
    private readonly Sync<string> percentageField;

    public MetricsPanelItem(Slot container, in Metric metric, long maxTicks, long totalTicks, int maxItems)
    {
        var uiBuilder = new UIBuilder(container);

        uiBuilder.Style.MinHeight = DEFAULT_ITEM_SIZE;
        uiBuilder.Style.TextAutoSizeMin = 0;

        slot = uiBuilder.Panel(RadiantUI_Constants.Neutrals.DARK).Slot;

        slot.AttachComponent<Button>();
        referenceProxySource = slot.AttachComponent<ReferenceProxySource>();

        var metricImage = uiBuilder.Image();
        metricRect = metricImage.RectTransform;
        metricTint = metricImage.Tint;

        uiBuilder.HorizontalLayout(DEFAULT_PADDING);
        uiBuilder.Style.FlexibleWidth = 0.0f;

        var labelText = uiBuilder.Text(metric.GetName(), bestFit: true, alignment: Alignment.MiddleLeft);
        labelText.RectTransform.AnchorMax.Value = new float2(0.8f, 1.0f);
        labelText.Color.Value = RadiantUI_Constants.TEXT_COLOR;
        labelField = labelText.Content;

        uiBuilder.Style.PreferredWidth= uiBuilder.Style.MinWidth = DEFAULT_ITEM_SIZE * 3;
        uiBuilder.Style.FlexibleWidth = -1.0f;
        var timeText = uiBuilder.Text("0", bestFit: false, size: 24, alignment: Alignment.MiddleRight);
        timeText.ParseRichText.Value = false;
        timeText.Color.Value = RadiantUI_Constants.TEXT_COLOR;
        timeField = timeText.Content;

        var percentageText = uiBuilder.Text("0.000%", bestFit: false, size: 24, alignment: Alignment.MiddleRight);
        percentageText.ParseRichText.Value = false;
        percentageText.Color.Value = RadiantUI_Constants.TEXT_COLOR;
        percentageField = percentageText.Content;

        uiBuilder.Style.PreferredWidth = uiBuilder.Style.MinWidth = DEFAULT_ITEM_SIZE;

        var deactivateButton = uiBuilder.Button(OfficialAssets.Common.Icons.Cross, RadiantUI_Constants.Hero.RED);
        deactivateButton.IsPressed.Changed += (_) =>
        {
            if (deactivateButton.IsPressed)
            {
                slot.Destroy();
            }
        };

        Update(metric, maxTicks, totalTicks, maxItems);
    }

    public bool Update(in Metric metric, long maxTicks, long totalTicks, int maxItems)
    {
        if (slot.IsDisposed) return false;

        var maxRatio = (float)metric.Ticks / maxTicks;

        slot.OrderOffset = -metric.Ticks;
        labelField.Value = metric.GetName();
        timeField.Value = $"{((double)1000.0 * metric.Ticks / Stopwatch.Frequency):0.0}ms";
        percentageField.Value = $"{(double)metric.Ticks / totalTicks:P3}";
        metricTint.Value = MathX.Lerp(RadiantUI_Constants.DarkLight.GREEN, RadiantUI_Constants.DarkLight.RED, maxRatio);
        metricRect.AnchorMax.Value = new float2(maxRatio, 1.0f);

        slot.ActiveSelf = slot.ChildIndex < maxItems;

        if (metric.Slot?.IsDestroyed ?? true)
        {
            referenceProxySource.Enabled = false;
        }
        else
        {
            referenceProxySource.Reference.Target = metric.Slot!;
        }

        return true;
    }
}
