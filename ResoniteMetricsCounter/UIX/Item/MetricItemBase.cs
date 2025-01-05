using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using ResoniteMetricsCounter.UIX.Pages;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ResoniteMetricsCounter.UIX.Item;

internal abstract class MetricItemBase<T>
{
    private const float DEFAULT_ITEM_SIZE = 32;
    private const float DEFAULT_PADDING = 4;

    private readonly Slot slot;
    private readonly Sync<colorX> metricTint;
    private readonly ReferenceProxySource referenceProxySource;
    private readonly RectTransform metricRect;

    protected readonly List<Sync<string>> LabelFields = new();
    protected abstract List<IMetricsPage.ColumnDefinition> Columns { get; }

    public MetricItemBase(Slot container)
    {
        var uiBuilder = new UIBuilder(container);

        uiBuilder.Style.MinHeight = DEFAULT_ITEM_SIZE;
        uiBuilder.Style.TextAutoSizeMin = 0;
        uiBuilder.Style.TextAutoSizeMax = 24;
        uiBuilder.Style.TextColor = RadiantUI_Constants.TEXT_COLOR;

        slot = uiBuilder.Panel(RadiantUI_Constants.Neutrals.DARK).Slot;

        slot.AttachComponent<Button>();
        referenceProxySource = slot.AttachComponent<ReferenceProxySource>();

        var metricImage = uiBuilder.Image();
        metricRect = metricImage.RectTransform;
        metricTint = metricImage.Tint;

        uiBuilder.HorizontalLayout(DEFAULT_PADDING);

        uiBuilder.Style.ForceExpandWidth = uiBuilder.Style.ForceExpandHeight = false;

        LabelFields.Capacity = Columns.Count;
        for (int i = 0; i < Columns.Count; i++)
        {
            var column = Columns[i];
            uiBuilder.Style.FlexibleWidth = column.FlexWidth;
            uiBuilder.Style.MinWidth = column.MinWidth;
            LabelFields.Add(uiBuilder.Text(column.Label, alignment: column.Alignment).Content);
        }

        //uiBuilder.PushStyle();
        //uiBuilder.Style.FlexibleWidth = -1;
        //uiBuilder.Style.PreferredWidth = uiBuilder.Style.MinWidth = DEFAULT_ITEM_SIZE;

        //var deactivateButton = uiBuilder.Button(OfficialAssets.Common.Icons.Cross, RadiantUI_Constants.Hero.RED);
        //deactivateButton.LocalPressed += (_, _) => slot.Destroy();

        //uiBuilder.PopStyle();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract long GetTicks(in T metric);


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract IWorldElement? GetReference(in T metric);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract void UpdateColumn(in T metric, Sync<string> column, int i, long maxTicks, long elapsedTicks, long frameCount);

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
