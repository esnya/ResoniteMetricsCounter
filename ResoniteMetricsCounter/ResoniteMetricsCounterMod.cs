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

    private static readonly HashSet<World.RefreshStage> SupportedStages = new()
    {
        World.RefreshStage.PhysicsMoved,
        World.RefreshStage.ProtoFluxContinuousChanges,
        World.RefreshStage.ProtoFluxUpdates,
        World.RefreshStage.Updates,
        World.RefreshStage.Changes,
        World.RefreshStage.Connectors,
    };

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

    private static readonly Harmony harmony = new($"com.nekometer.esnya.{ModAssembly.GetName()}");
    public static MetricsPanel? Panel { get; private set; }
    public static MetricsCounter? Writer { get; private set; }
    private static string menuActionLabel = MENU_ACTION;
    private static readonly Dictionary<World.RefreshStage, ModConfigurationKey<bool>> stageConfigKeys = new();
    private static readonly Dictionary<World.RefreshStage, bool> collectStage = new();

    public override void DefineConfiguration(ModConfigurationDefinitionBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        foreach (var stage in SupportedStages)
        {
            var key = new ModConfigurationKey<bool>($"Collect {stage}", $"Collect metrics for {stage}.", computeDefault: () => true);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CollectStage(World.RefreshStage stage)
    {
        return collectStage.TryGetValue(stage, out var value) && value;
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

        DevCreateNewForm.AddAction("/Editor", menuActionLabel, Start);
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
        catch (System.Exception e)
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

    public static void Start(Slot slot)
    {
        Msg("Starting Profiler");
        var blackList = ParseCommaSeparatedString(config?.GetValue(blackListKey));
        Writer = new MetricsCounter(blackList);
        Panel = new MetricsPanel(slot, Writer, config?.GetValue(panelSizeKey) ?? new float2(1200, 1200), config?.GetValue(maxItemsKey) ?? 256);
        harmony.PatchCategory(Category.PROFILER);
    }

    public static void Stop()
    {
        Msg("Stopping Profiler");
        harmony.UnpatchCategory(Category.PROFILER);
        Writer?.Dispose();
        WorldElementHelper.Clear();
        Panel = null;
    }
}
