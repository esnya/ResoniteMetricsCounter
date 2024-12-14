using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using ResoniteMetricsCounter.Metrics;
using ResoniteMetricsCounter.UIX.Pages;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ResoniteMetricsCounter.UIX;
internal sealed class MetricsPanel
{

    private static readonly List<KeyValuePair<string, IMetricsPage>> pages = new()
    {
        new("Detailed", new DetailedMetricsPanelPage()),
        new("ObjectRoot", new ObjectRootPage()),
    };

    public static float DEFAULT_ITEM_SIZE = 32;
    public static float PADDING = 4;

    private readonly MetricsCounter metricsCounter;
    private readonly Slot slot;
    private readonly Sync<string> statisticsField;
    private readonly int maxItems;
    private readonly Slot? pagesButtonContainer;
    private readonly Slot? pagesContainer;

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

        var activePage = pages[0].Key;
        pagesButtonContainer = uiBuilder.HorizontalLayout(PADDING).Slot;

        uiBuilder.PushStyle();
        uiBuilder.Style.ForceExpandWidth = false;
        foreach (var keyValuePair in pages)
        {
            BuildPageButtonUI(uiBuilder, keyValuePair.Key, keyValuePair.Key == activePage);
        }
        uiBuilder.PopStyle();
        uiBuilder.NestOut();

        uiBuilder.PushStyle();
        uiBuilder.Style.FlexibleHeight = 1.0f;
        pagesContainer = uiBuilder.Panel().Slot;
        uiBuilder.PopStyle();

        foreach (var keyValuePair in pages)
        {
            uiBuilder.NestInto(pagesContainer);
            BuildPageUI<DetailedMetricsPanelPage>(uiBuilder, keyValuePair.Value, keyValuePair.Key, keyValuePair.Key == activePage);
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

    private void BuildStopButtonUI(in UIBuilder uiBuilder)
    {
        var button = uiBuilder.Button("Stop Profiling", RadiantUI_Constants.Hero.RED);
        button.IsPressed.Changed += (_) =>
        {
            if (button.IsPressed)
            {
                foreach (var page in pages)
                {
                    page.Value.Update(metricsCounter, maxItems);
                }
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

    private void BuildPageButtonUI(in UIBuilder uiBuilder, string label, bool defaultActive)
    {

        uiBuilder.Style.FlexibleWidth = defaultActive ? 3.0f : 1.0f;
        uiBuilder.Style.PreferredWidth = 0.0f;

        var button = uiBuilder.Button(label);
        button.Slot.Name = label;

        button.IsPressed.Changed += (_) =>
        {
            if (!button.IsPressed) return;

            if (pagesButtonContainer is not null)
            {
                foreach (var slot in pagesButtonContainer.Children)
                {
                    var layout = slot.GetComponent<LayoutElement>();
                    if (layout is null) continue;

                    layout.FlexibleWidth.Value = slot.Name == label ? 3.0f : 1.0f;
                }
            }

            if (pagesContainer is not null)
            {
                foreach (var slot in pagesContainer.Children)
                {
                    slot.ActiveSelf = slot.Name == label;
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
        page.BuildUI(uiBuilder);
        uiBuilder.PopStyle();
        uiBuilder.NestOut();
    }

    public void Update()
    {
        if (slot?.IsDisposed ?? true) return;

        var totalTicks = metricsCounter.TotalTicks;
        var maxTicks = metricsCounter.MaxTicks;

        if (statisticsField.IsDisposed) return;
        statisticsField.Value = $"Total:\t{1000.0 * totalTicks / Stopwatch.Frequency:0.00}ms<br>Max:\t{1000.0 * maxTicks / Stopwatch.Frequency:0.00}ms<br>Entities:\t{metricsCounter.Metrics.Count}";

        foreach (var page in pages)
        {
            if (page.Value.IsActive())
            {
                page.Value.Update(metricsCounter, maxItems);
            }
        }
    }
}
