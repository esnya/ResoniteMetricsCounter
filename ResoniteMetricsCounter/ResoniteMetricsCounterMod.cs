using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Elements.Core;

using HarmonyLib;

using ResoniteMetricsCounter.Metrics;
using ResoniteMetricsCounter.Patch;
using ResoniteMetricsCounter.UIX;

using ResoniteModLoader;
using FrooxEngine;
using ResoniteMetricsCounter.Utils;
using System;
using System.Runtime.CompilerServices;



#if DEBUG
using ResoniteHotReloadLib;
#endif

namespace ResoniteMetricsCounter;

public class ResoniteMetricsCounterMod : ResoniteMod
{
    private static Assembly ModAssembly => typeof(ResoniteMetricsCounterMod).Assembly;

    private const string MENU_ACTION = "Performance Metrics Counter (Mod)";

    public override string Name => ModAssembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
    public override string Author => ModAssembly.GetCustomAttribute<AssemblyCompanyAttribute>().Company;
    public override string Version => ModAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
    public override string Link => ModAssembly.GetCustomAttributes<AssemblyMetadataAttribute>().First(meta => meta.Key == "RepositoryUrl").Value;

    private static ModConfiguration? config;

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<string> blackListKey = new("BlackList", "Ignore those components. Commas separated.", computeDefault: () => string.Join(",", new[] {
        nameof(InteractionHandler),
        nameof(InteractionLaser),
        nameof(HandPoser),
        nameof(LocomotionController),
        nameof(UserPoseController),
        nameof(PhotoCaptureManager),
        nameof(TipTouchSource),
     }));

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<float2> panelSizeKey = new("PanelSize", "Size of the panel.", computeDefault: () => new float2(1200, 1200));

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<int> maxItemsKey = new("MaxItems", "Max items to show in the panel.", computeDefault: () => 256);

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<bool> writeToFileKey = new("WriteToFile", "Write metrics to file.", computeDefault: () => false);

    private static readonly Harmony harmony = new($"com.nekometer.esnya.{ModAssembly.GetName()}");
    internal static MetricsPanel? Panel { get; private set; }
    internal static MetricsCounter? Writer { get; private set; }
    private static string menuActionLabel = MENU_ACTION;
    private static readonly Dictionary<MetricStage, ModConfigurationKey<bool>> stageConfigKeys = new();
    private static readonly Dictionary<MetricStage, bool> collectStage = new();
    private static bool isRunning;
    private static Slot? old_slot;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool GetStageConfigValue(MetricStage stage)
    {
        return !stageConfigKeys.TryGetValue(stage, out var key) || (config?.GetValue(key) ?? true);
    }

    public override void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        foreach (var stage in MetricStageUtils.Collectables)
        {
            var defaultValue = MetricStageUtils.Defaults.Contains(stage);
            var key = new ModConfigurationKey<bool>($"Collect {stage}", $"Collect metrics for {stage}.", computeDefault: () => defaultValue);
            key.OnChanged += value => collectStage[stage] = (bool)value!;
            builder.Key(key);
            stageConfigKeys[stage] = key;
        }
    }

    public override void OnEngineInit()
    {
        Init(this);

#if DEBUG
        HotReloader.RegisterForHotReload(this);
#endif
    }

    private static void Init(ResoniteMod modInstance)
    {
        harmony.PatchCategory(Category.CORE);
        config = modInstance?.GetConfiguration();

        if (config is not null)
        {
            foreach (var p in stageConfigKeys)
            {
                collectStage[p.Key] = config.GetValue(p.Value);
            }
        }

#if DEBUG
        menuActionLabel = $"{MENU_ACTION} ({HotReloader.GetReloadedCountOfModType(modInstance?.GetType())})";
#endif

        DevCreateNewForm.AddAction("/Editor", menuActionLabel, initPanel);
    }
#if DEBUG

    public static void BeforeHotReload()
    {
        try
        {
            Stop();
            harmony.UnpatchCategory(Category.CORE);
            HotReloader.RemoveMenuOption("/Editor", menuActionLabel);
        }
        catch (Exception e)
        {
            Error(e);
        }
    }

    public static void OnHotReload(ResoniteMod modInstance)
    {
        Init(modInstance);
    }
#endif

    public static IEnumerable<string> ParseCommaSeparatedString(string? str)
    {
        return str?.Split(',')?.Select(item => item.Trim()).Where(item => item.Length > 0) ?? Enumerable.Empty<string>();
    }

    public static void initPanel(Slot slot)
    {
        if (Panel is not null)
        {
           Panel.DisableStopButton();
        }

        if (old_slot is not null && isRunning is true)
        {
            Stop();
        }

        Start(slot);
    }

    public static void Start(Slot slot = null)
    {
        if (slot == null)
        {
            //Msg("Assigning field \'old_slot\' to \'slot\' local variable");
            slot = old_slot;
            if (Panel != null)
            {
                //Msg("Disposing Panel");
                Panel.Dispose();
                Panel = null;
            }
        }
        else
        {
            //Msg("Assigning local variable \'slot\' to \'old_slot\' field");
            old_slot = slot;
        }
        isRunning = true;
        Msg("Starting Profiler");
        var blackList = ParseCommaSeparatedString(config?.GetValue(blackListKey));
        Writer = new MetricsCounter(blackList);

        Panel = new MetricsPanel(slot, Writer, config?.GetValue(panelSizeKey) ?? new float2(1200, 1200), config?.GetValue(maxItemsKey) ?? 256);

        foreach (var key in stageConfigKeys)
        {
            if (GetStageConfigValue(key.Key))
            {
                Msg($"Patching to profile {key.Key}");
                harmony.PatchCategory(key.Key.ToString());
            }
        }

        harmony.PatchCategory(Category.PROFILER);

        Msg("Profiler started");
    }
    
    public static void Stop()
    {
        if (!isRunning)
        {
            isRunning = true;
            Start();
            return;
        }
        isRunning = false;
        Msg("Stopping Profiler");
        foreach (var key in stageConfigKeys)
        {
            try
            {
                Msg($"Unpatching {key.Key}");
                harmony.UnpatchCategory(key.Key.ToString());
            }
            catch (Exception e)
            {
                Debug(e);
            }
        }

        harmony.UnpatchCategory(Category.PROFILER);

        if (config?.GetValue(writeToFileKey) == true)
        {
            Writer?.WriteToFile();
        }

        Writer?.Dispose();
        WorldElementHelper.Clear();

        

        Msg("Profiler stopped");
    }
}
