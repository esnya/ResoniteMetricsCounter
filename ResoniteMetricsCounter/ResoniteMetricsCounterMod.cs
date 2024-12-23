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
    private static readonly ModConfigurationKey<string> blackListKey = new("BlackList", "Ignore those components. Commas separated.", computeDefault: () => "UserPoseController,TipTouchSource,LocomotionController,HandPoser,InteractionLaser");

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<float2> panelSizeKey = new("PanelSize", "Size of the panel.", computeDefault: () => new float2(1200, 1200));

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<int> maxItemsKey = new("MaxItems", "Max items to show in the panel.", computeDefault: () => 256);

    private static readonly Harmony harmony = new($"com.nekometer.esnya.{ModAssembly.GetName()}");
    public static MetricsPanel? Panel { get; private set; }
    public static MetricsCounter? Writer { get; private set; }
    private static string menuActionLabel = MENU_ACTION;

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

        Engine.Current.WorldManager.WorldFocused += OnWorldFocused;

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
            Engine.Current.WorldManager.WorldFocused -= OnWorldFocused;
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

    private static void OnWorldFocused(World world)
    {
        WorldElementHelper.Clear();
    }

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
        Panel = null;
    }
}
