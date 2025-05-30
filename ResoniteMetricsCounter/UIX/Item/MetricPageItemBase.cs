using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using ResoniteMetricsCounter.Utils;

namespace ResoniteMetricsCounter.UIX.Item;

internal abstract class MetricPageItemBase<T>
{
    private readonly Slot slot;
    private readonly Sync<colorX> metricTint;
    private readonly ReferenceProxySource referenceProxySource;
    private readonly RectTransform metricRect;

    protected readonly List<Sync<string>> LabelFields = new();

    public MetricPageItemBase(Slot container, List<MetricColumnDefinition> columns)
    {
        var uiBuilder = new UIBuilder(container);

        uiBuilder.Style.MinHeight = Constants.ROWHEIGHT;
        slot = uiBuilder.Panel(RadiantUI_Constants.Neutrals.DARK).Slot;

        slot.AttachComponent<Button>();
        referenceProxySource = slot.AttachComponent<ReferenceProxySource>();

        var metricImage = uiBuilder.Image();
        metricRect = metricImage.RectTransform;
        metricTint = metricImage.Tint;

        LabelFields.Capacity = columns.Count;
        foreach (var label in MetricColumnDefinition.Build(uiBuilder, columns))
        {
            LabelFields.Add(label.Content);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract long GetTicks(in T metric);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract IWorldElement? GetReference(in T metric);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract void UpdateColumn(
        in T metric,
        Sync<string> column,
        int i,
        long maxTicks,
        long elapsedTicks,
        long frameCount
    );

    public bool Update(in T metric, long maxTicks, long elapsedTicks, long frameCount)
    {
        if (slot.IsDisposed)
        {
            return false;
        }

        var ticks = GetTicks(metric);
        var maxRatio = (float)ticks / maxTicks;
        slot.OrderOffset = -ticks;

        for (int i = 0; i < LabelFields.Count; i++)
        {
            UpdateColumn(metric, LabelFields[i], i, maxTicks, elapsedTicks, frameCount);
        }

        metricTint.Value = MathX.Lerp(
            RadiantUI_Constants.DarkLight.GREEN,
            RadiantUI_Constants.DarkLight.RED,
            MathX.Sqrt(maxRatio)
        );
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
