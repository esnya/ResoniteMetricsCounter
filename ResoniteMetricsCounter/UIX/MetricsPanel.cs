using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using ResoniteMetricsCounter.Metrics;
using ResoniteMetricsCounter.UIX.Pages;
using ResoniteModLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ResoniteMetricsCounter.UIX;
internal sealed class MetricsPanel
{
    private static readonly List<KeyValuePair<string, IMetricsPage>> pages = new()
    {
        new("Detailed", new DetailedMetricsPanelPage()),
    };

    public static float DEFAULT_ITEM_SIZE = 32;
    public static float PADDING = 4;

    private readonly MetricsCounter metricsCounter;
    private readonly Slot slot;
    private readonly List<Slot> pagesContainer = new();
    private readonly Sync<string> statisticsField;
    private readonly int maxItems;


    public MetricsPanel(MetricsCounter metricsCounter, in float2 size, int maxItems)
    {
        this.maxItems = maxItems;
        this.metricsCounter = metricsCounter;

        var uiBuilder = CreatePanel(size);
        slot = uiBuilder.Root;
        if (slot is null) throw new InvalidOperationException("Slot is null");
        metricsCounter.IgnoreHierarchy(slot);

        uiBuilder.VerticalLayout(PADDING, forceExpandHeight: false);

        BuildStopButtonUI(uiBuilder);
        statisticsField = BuildStatisticsUI(uiBuilder);

        uiBuilder.HorizontalLayout(PADDING);
        foreach (var keyValuePair in pages)
        {
            BuildPageButtonUI(uiBuilder, keyValuePair.Key);
        }
        uiBuilder.NestOut();

        uiBuilder.PushStyle();
        uiBuilder.Style.FlexibleHeight = 1.0f;
        uiBuilder.Panel();
        uiBuilder.PopStyle();

        foreach (var keyValuePair in pages)
        {
            BuildPageUI<DetailedMetricsPanelPage>(uiBuilder, keyValuePair.Value, keyValuePair.Key, keyValuePair.Key == "Detailed");
        }
    }

    private static UIBuilder CreatePanel(in float2 size)
    {
        var slot = Engine.Current.WorldManager.FocusedWorld.LocalUserSpace.AddSlot("Resonite Profile Metrics", persistent: false);
        slot.OnPrepareDestroy += (_) => ResoniteMetricsCounterMod.Stop();
        slot.Tag = "Developer";
        slot.LocalScale = float3.One * 0.00075f;
        slot.PositionInFrontOfUser();
        slot.GlobalRotation = slot.LocalUserRoot.ViewRotation;

        var uiBuilder = RadiantUI_Panel.SetupPanel(slot, "Metrics", size, pinButton: true);
        uiBuilder.Style.TextAutoSizeMin = 0;
        uiBuilder.Style.MinHeight = DEFAULT_ITEM_SIZE;
        uiBuilder.Style.ForceExpandHeight = false;

        return uiBuilder;
    }

    private static void BuildStopButtonUI(in UIBuilder uiBuilder)
    {
        var button = uiBuilder.Button("Stop Profiling", RadiantUI_Constants.Hero.RED);
        button.IsPressed.Changed += (_) =>
        {
            if (button.IsPressed)
            {
                ResoniteMetricsCounterMod.Stop();
                button.Enabled = false;
            }
        };
    }

    private static Sync<string> BuildStatisticsUI(in UIBuilder uiBuilder)
    {
        var statisticsText = uiBuilder.Text("Profiling", bestFit: true, alignment: Alignment.MiddleLeft);
        statisticsText.Color.Value = RadiantUI_Constants.TEXT_COLOR;
        statisticsText.Size.Value = 24;
        return statisticsText.Content;
    }

    private void BuildPageButtonUI(in UIBuilder uiBuilder, string label)
    {
        var button = uiBuilder.Button(label);
        var buttonTextColor = button.Slot.GetComponentInChildren<Text>().Color;
        button.IsPressed.Changed += (changable) =>
        {
            if (changable is Button button && button.IsPressed)
            {
                foreach (var slot in pagesContainer)
                {
                    var active = slot.Name == label;
                    slot.ActiveSelf = active;
                    buttonTextColor.Value = active ? RadiantUI_Constants.TEXT_COLOR : RadiantUI_Constants.DISABLED_COLOR;
                }
            }
        };
    }

    private static void BuildPageUI<T>(UIBuilder uiBuilder, in IMetricsPage page, in string label, bool active) where T : IMetricsPage, new()
    {
        var slot = uiBuilder.Next(label);
        slot.ActiveSelf = active;

        uiBuilder.NestInto(slot);

        uiBuilder.ScrollArea();

        uiBuilder.FitContent(SizeFit.Disabled, SizeFit.MinSize);

        uiBuilder.PushStyle();
        ResoniteMod.Debug("Building UI");
        page.BuildUI(uiBuilder);
        uiBuilder.PopStyle();
        uiBuilder.NestOut();
    }

    public void Update()
    {
        if (slot?.IsDisposed ?? true) return;

        ResoniteMod.Debug($"Updating Metrics Panel: {metricsCounter}");
        var totalTicks = metricsCounter.TotalTicks;
        var maxTicks = metricsCounter.MaxTicks;

        if (statisticsField.IsDisposed) return;
        ResoniteMod.DebugFunc(() => $"Updating Statistics Field: {statisticsField}");
        statisticsField.Value = $"Total:\t{1000.0 * totalTicks / Stopwatch.Frequency:0.00}ms<br>Max:\t{1000.0 * maxTicks / Stopwatch.Frequency:0.00}ms<br>Entities:\t{metricsCounter.Metrics.Count}";

        ResoniteMod.DebugFunc(() => $"Updating Pages: {pages}");
        foreach (var page in pages)
        {
            ResoniteMod.DebugFunc(() => $"Updating {page.Key}");
            if (page.Value.IsActive())
            {
                ResoniteMod.DebugFunc(() => $"Updating {page.Key}");
                page.Value.Update(metricsCounter, maxItems);
            }
        }
    }
}
