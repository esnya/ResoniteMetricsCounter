using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using ResoniteMetricsCounter.Metrics;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ResoniteMetricsCounter.UIX;
internal sealed class MetricsPanel
{
    public static float DEFAULT_ITEM_SIZE = 32;
    public static float PADDING = 4;

    private readonly MetricsCounter metricsCounter;
    private readonly Slot slot;
    private readonly List<MetricsPanelItem?> items;
    private readonly Slot itemsContainer;
    private readonly Sync<string> statisticsField;
    private readonly int maxItems;

    public MetricsPanel(MetricsCounter metricsCounter, in float2 size, int maxItems)
    {
        this.maxItems = maxItems;
        this.metricsCounter = metricsCounter;
        items = Enumerable.Repeat<MetricsPanelItem?>(null, maxItems).ToList();

        slot = Engine.Current.WorldManager.FocusedWorld.LocalUserSpace.AddSlot("Resonite Profile Metrics", persistent: false);
        slot.OnPrepareDestroy += (_) => ResoniteMetricsCounterMod.Stop();
        metricsCounter.IgnoreHierarchy(slot);

        var uiBuilder = RadiantUI_Panel.SetupPanel(slot, "Metrics", size, pinButton: true);
        slot.Tag = "Developer";
        slot.LocalScale = float3.One * 0.00075f;
        slot.PositionInFrontOfUser();

        uiBuilder.Style.TextAutoSizeMin = 0;
        uiBuilder.Style.MinHeight = DEFAULT_ITEM_SIZE;
        uiBuilder.VerticalLayout(PADDING, forceExpandHeight: false);

        var stopButton = uiBuilder.Button("Stop Profiling", RadiantUI_Constants.Hero.RED);
        stopButton.IsPressed.Changed += (_) =>
        {
            if (stopButton.IsPressed)
            {
                ResoniteMetricsCounterMod.Stop();
                stopButton.Enabled = false;
            }
        };

        var statisticsText = uiBuilder.Text("Profiling", bestFit: true, alignment: Alignment.MiddleLeft);
        statisticsText.Color.Value = RadiantUI_Constants.TEXT_COLOR;
        statisticsText.Size.Value = 24;
        statisticsField = statisticsText.Content;

        uiBuilder.Style.FlexibleHeight = 1.0f;
        uiBuilder.ScrollArea(Alignment.TopLeft);
        uiBuilder.FitContent(SizeFit.Disabled, SizeFit.MinSize);

        itemsContainer = uiBuilder.VerticalLayout(spacing: 8).Slot;
    }

    public void Update()
    {
        if (slot.IsDisposed) return;

        var totalTicks = metricsCounter.TotalTicks;
        var maxTicks = metricsCounter.MaxTicks;
        statisticsField.Value = $"Total:\t{1000.0 * totalTicks / Stopwatch.Frequency:0.00}ms<br>Max:\t{1000.0 * maxTicks / Stopwatch.Frequency:0.00}ms<br>Entities:\t{metricsCounter.Metrics.Count}";


        var i = 0;
        foreach (var metric in metricsCounter.Metrics.Values.OrderByDescending(a => a.Ticks).Take(maxItems))
        {
            var item = items[i];
            if (item == null)
            {
                items[i] = new MetricsPanelItem(itemsContainer, metric, maxTicks, totalTicks, maxItems);
            }
            else
            {
                if (!item.Update(metric, maxTicks, totalTicks, maxItems))
                {
                    metricsCounter.Remove(metric);
                    items[i] = null;
                }
            }
            i++;
        }
    }
}
