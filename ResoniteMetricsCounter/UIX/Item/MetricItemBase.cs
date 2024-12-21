using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ResoniteMetricsCounter.UIX.Item;

internal abstract class MetricItemBase<T>
{
    private const float DEFAULT_ITEM_SIZE = 32;
    private const float DEFAULT_PADDING = 4;

    private readonly Slot slot;
    private readonly Sync<string> labelField, timeField;
    private readonly Sync<colorX> metricTint;
    private readonly ReferenceProxySource referenceProxySource;
    private readonly RectTransform metricRect;
    private readonly Sync<string> percentageField;

    public MetricItemBase(Slot container)
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
        uiBuilder.Style.FlexibleWidth = 1.0f;

        var labelText = uiBuilder.Text(null, bestFit: true, alignment: Alignment.MiddleLeft);
        labelText.RectTransform.AnchorMax.Value = new float2(0.8f, 1.0f);
        labelText.Color.Value = RadiantUI_Constants.TEXT_COLOR;
        labelField = labelText.Content;

        uiBuilder.Style.PreferredWidth = uiBuilder.Style.MinWidth = DEFAULT_ITEM_SIZE * 3;
        uiBuilder.Style.FlexibleWidth = -1.0f;
        var timeText = uiBuilder.Text(null, bestFit: false, size: 24, alignment: Alignment.MiddleRight);
        timeText.ParseRichText.Value = false;
        timeText.Color.Value = RadiantUI_Constants.TEXT_COLOR;
        timeField = timeText.Content;

        var percentageText = uiBuilder.Text(null, bestFit: false, size: 24, alignment: Alignment.MiddleRight);
        percentageText.ParseRichText.Value = false;
        percentageText.Color.Value = RadiantUI_Constants.TEXT_COLOR;
        percentageField = percentageText.Content;

        uiBuilder.Style.PreferredWidth = uiBuilder.Style.MinWidth = DEFAULT_ITEM_SIZE;

        var deactivateButton = uiBuilder.Button(OfficialAssets.Common.Icons.Cross, RadiantUI_Constants.Hero.RED);
        deactivateButton.LocalPressed += (_, _) => slot.Destroy();
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract long GetTicks(in T metric);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract string? GetLabel(in T metric);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract IWorldElement? GetReference(in T metric);

    public bool Update(in T metric, long maxTicks, long totalTicks)
    {
        if (slot.IsDisposed) return false;

        var ticks = GetTicks(metric);
        var maxRatio = (float)ticks / maxTicks;

        slot.OrderOffset = -ticks;
        var label = GetLabel(metric);
        if (label is null) return false;

        labelField.Value = label;

        var doubleTicks = (double)ticks;
        timeField.Value = $"{1000.0 * doubleTicks / Stopwatch.Frequency:0.0}ms";
        percentageField.Value = $"{doubleTicks / totalTicks:P3}";
        metricTint.Value = MathX.Lerp(RadiantUI_Constants.DarkLight.GREEN, RadiantUI_Constants.DarkLight.RED, maxRatio);
        metricRect.AnchorMax.Value = new float2(maxRatio, 1.0f);

        var reference = GetReference(metric);
        if (reference is null || reference.IsRemoved)
        {
            referenceProxySource.Enabled = false;
        }
        else
        {
            referenceProxySource.Reference.Target = reference;
        }

        return true;
    }
}
