using System;
using System.Collections.Generic;
using System.Diagnostics;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.UIX;
using ResoniteMetricsCounter.Metrics;
using ResoniteMetricsCounter.UIX.Pages;

namespace ResoniteMetricsCounter.UIX;

internal sealed class MetricsPanel
{
    private readonly List<KeyValuePair<string, MetricsPageBase>> pages = new()
    {
        new("Detailed", new DetailedPage()),
        new("Hierarchy", new HierarchyPage()),
    };

    public const float DEFAULTITEMSIZE = 32;
    public const float PADDING = 4;
    public const float DEFAULTSEPARATION = 0.1f;

    private readonly MetricsCounter metricsCounter;
    private readonly Slot slot;
    private readonly int maxItems;
    private readonly Slot? pagesButtonContainer;
    private readonly Slot? pagesContainer;
    private Button? stopButton;

    private Sync<string>? framesField;
    private Sync<string>? elapsedTimeField;
    private Sync<string>? totalTimeField;
    private Sync<string>? maxTimeField;
    private Sync<string>? countField;
    private Sync<string>? frameIntervalField;
    private Sync<string>? avgTotalTimeField;
    private Sync<string>? avgMaxTimeField;
    private Sync<string>? fpsField;
    private float nextUpdateTime;

    private static bool isProfiling = true;

    public MetricsPanel(Slot slot, MetricsCounter metricsCounter, in float2 size, int maxItems)
    {
        if (slot is null)
        {
            throw new ArgumentNullException(nameof(slot));
        }

        if (metricsCounter is null)
        {
            throw new ArgumentNullException(nameof(metricsCounter));
        }

        this.maxItems = maxItems;
        this.metricsCounter = metricsCounter;

        var uiBuilder = CreatePanel(slot, size);
        this.slot = uiBuilder.Root;
        metricsCounter.IgnoreHierarchy(slot);

        uiBuilder.Style.MinHeight = DEFAULTITEMSIZE;
        uiBuilder.Style.ForceExpandHeight = false;
        uiBuilder.Style.TextColor = RadiantUI_Constants.TEXT_COLOR;
        uiBuilder.Style.TextAutoSizeMin = 0;
        uiBuilder.Style.TextAutoSizeMax = 24;

        uiBuilder.VerticalLayout(PADDING, forceExpandHeight: false);

        BuildStopButtonUI(uiBuilder);
        BuildHeaderUI(uiBuilder);

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
            BuildPageUI(
                uiBuilder,
                keyValuePair.Value,
                keyValuePair.Key,
                keyValuePair.Key == activePage
            );
        }
    }

    private static UIBuilder CreatePanel(in Slot slot, in float2 size)
    {
        slot.PersistentSelf = true;
        slot.OnPrepareDestroy += (_) =>
        {
            if (isProfiling)
            {
                ResoniteMetricsCounterMod.SetRunning(false);
            }
        };

        slot.Tag = "Developer";
        slot.LocalScale = float3.One * 0.00075f;

        var uiBuilder = RadiantUI_Panel.SetupPanel(slot, "Metrics", size, pinButton: true);

        return uiBuilder;
    }

    private void BuildStopButtonUI(in UIBuilder uiBuilder)
    {
        var button = uiBuilder.Button("Stop Profiling", RadiantUI_Constants.Hero.RED);
        stopButton = button;

        button.LocalPressed += (_, _) =>
        {
            foreach (var page in pages)
            {
                page.Value.Update(metricsCounter, maxItems);
            }

            var shouldRun = !isProfiling;
            ResoniteMetricsCounterMod.SetRunning(shouldRun);
            isProfiling = shouldRun;

            button.LabelText = isProfiling ? "Stop Profiling" : "Restart Profiler";
        };
    }

    private void BuildHeaderUI(UIBuilder uiBuilder)
    {
        uiBuilder.PushStyle();
        uiBuilder.Style.TextAlignment = Alignment.MiddleLeft;

        const float spacing = 0.25f;

        uiBuilder.HorizontalLayout();
        uiBuilder.HorizontalElementWithLabel(
            "Frames:",
            spacing,
            () =>
            {
                var text = uiBuilder.Text("0");
                framesField = text.Content;
                return text;
            }
        );
        uiBuilder.HorizontalElementWithLabel(
            "Avg:",
            spacing,
            () =>
            {
                var text = uiBuilder.Text("0.00FPS");
                fpsField = text.Content;
                return text;
            }
        );
        uiBuilder.NestOut();

        uiBuilder.HorizontalLayout();
        uiBuilder.HorizontalElementWithLabel(
            "Elapsed:",
            spacing,
            () =>
            {
                var text = uiBuilder.Text("0.00ms");
                elapsedTimeField = text.Content;
                return text;
            }
        );
        uiBuilder.HorizontalElementWithLabel(
            "Avg:",
            spacing,
            () =>
            {
                var text = uiBuilder.Text("0.00ms");
                frameIntervalField = text.Content;
                return text;
            }
        );
        uiBuilder.NestOut();

        uiBuilder.HorizontalLayout();
        uiBuilder.HorizontalElementWithLabel(
            "Sum:",
            spacing,
            () =>
            {
                var text = uiBuilder.Text("0.00ms");
                totalTimeField = text.Content;
                return text;
            }
        );
        uiBuilder.HorizontalElementWithLabel(
            "Avg:",
            spacing,
            () =>
            {
                var text = uiBuilder.Text("0.00ms");
                avgTotalTimeField = text.Content;
                return text;
            }
        );
        uiBuilder.NestOut();

        uiBuilder.HorizontalLayout();
        uiBuilder.HorizontalElementWithLabel(
            "Max:",
            spacing,
            () =>
            {
                var text = uiBuilder.Text("0.00ms");
                maxTimeField = text.Content;
                return text;
            }
        );
        uiBuilder.HorizontalElementWithLabel(
            "Avg:",
            spacing,
            () =>
            {
                var text = uiBuilder.Text("0.00ms");
                avgMaxTimeField = text.Content;
                return text;
            }
        );
        uiBuilder.NestOut();

        uiBuilder.HorizontalLayout();
        uiBuilder.HorizontalElementWithLabel(
            "Count:",
            spacing,
            () =>
            {
                var text = uiBuilder.Text("0");
                countField = text.Content;
                return text;
            }
        );
        uiBuilder.Spacer(0.5f);
        uiBuilder.NestOut();

        uiBuilder.PopStyle();
    }

    private void BuildPageButtonUI(in UIBuilder uiBuilder, string label, bool defaultActive)
    {
        uiBuilder.Style.FlexibleWidth = defaultActive ? 3.0f : 1.0f;
        uiBuilder.Style.PreferredWidth = 0.0f;

        var button = uiBuilder.Button(label);
        button.Slot.Name = label;

        button.LocalPressed += (_, _) =>
        {
            if (pagesButtonContainer is not null)
            {
                foreach (var slot in pagesButtonContainer.Children)
                {
                    var layout = slot.GetComponent<LayoutElement>();
                    if (layout is null)
                    {
                        continue;
                    }

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

    private static void BuildPageUI(
        UIBuilder uiBuilder,
        in MetricsPageBase page,
        in string label,
        bool active
    )
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
        if (slot?.IsDisposed ?? true)
        {
            return;
        }

        metricsCounter.OnUpdate();

        var worldTime = slot.World.Time.WorldTimeFloat;
        if (worldTime < nextUpdateTime)
        {
            return;
        }

        nextUpdateTime = worldTime + ResoniteMetricsCounterMod.UiUpdateInterval;

        var frames = metricsCounter.FrameCount;

        if (framesField is not null && !framesField.IsDisposed)
        {
            framesField.Value = $"{frames}";
        }

        var elapsedTime = metricsCounter.ElapsedMilliseconds;

        if (fpsField is not null && !fpsField.IsDisposed)
        {
            fpsField.Value = $"{1000 * frames / elapsedTime:0.0}FPS";
        }

        if (elapsedTimeField is not null && !elapsedTimeField.IsDisposed)
        {
            elapsedTimeField.Value = $"{elapsedTime}ms";
        }

        if (frameIntervalField is not null && !frameIntervalField.IsDisposed)
        {
            frameIntervalField.Value = $"{elapsedTime / frames}ms";
        }

        var totalTime = 1000.0 * metricsCounter.ByElement.Total / Stopwatch.Frequency;

        if (totalTimeField is not null && !totalTimeField.IsDisposed)
        {
            totalTimeField.Value = $"{totalTime:0.00}ms";
        }

        if (avgTotalTimeField is not null && !avgTotalTimeField.IsDisposed)
        {
            avgTotalTimeField.Value = $"{totalTime / frames:0.000}ms";
        }

        var maxTime = 1000.0 * metricsCounter.ByElement.Max / Stopwatch.Frequency;

        if (avgMaxTimeField is not null && !avgMaxTimeField.IsDisposed)
        {
            avgMaxTimeField.Value = $"{maxTime / frames:0.000}ms";
        }

        if (maxTimeField is not null && !maxTimeField.IsDisposed)
        {
            maxTimeField.Value = $"{maxTime:0.00}ms";
        }

        if (countField is not null && !countField.IsDisposed)
        {
            countField.Value = $"{metricsCounter.ByElement.Count}";
        }

        foreach (var page in pages)
        {
            if (page.Value.IsActive())
            {
                page.Value.Update(metricsCounter, maxItems);
            }
        }
    }

    public void Dispose()
    {
        slot?.Dispose();
    }

    public void DisableStopButton()
    {
        if (stopButton is null || stopButton.IsDisposed)
        {
            return;
        }

        stopButton.Enabled = false;
    }
}
