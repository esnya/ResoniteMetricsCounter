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




#if DEBUG
using ResoniteHotReloadLib;
#endif

namespace ResoniteMetricsCounter;

public partial class ResoniteMetricsCounterMod : ResoniteMod
{
    private static Assembly ModAssembly => typeof(ResoniteMetricsCounterMod).Assembly;

    public override string Name => ModAssembly.GetCustomAttribute<AssemblyTitleAttribute>().Title;
    public override string Author => ModAssembly.GetCustomAttribute<AssemblyCompanyAttribute>().Company;
    public override string Version => ModAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
    public override string Link => ModAssembly.GetCustomAttributes<AssemblyMetadataAttribute>().First(meta => meta.Key == "RepositoryUrl").Value;

    private static ModConfiguration? config;

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<string> blackListKey = new("BlackList", "Ignore those components. Commas separated.", computeDefault: () => "UserPoseController,TipTouchSource,LocomotionController,HandPoser");

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<float2> panelSizeKey = new("PanelSize", "Size of the panel.", computeDefault: () => new float2(1200, 1200));

    [AutoRegisterConfigKey]
    private static readonly ModConfigurationKey<int> maxItemsKey = new("MaxItems", "Max items to show in the panel.", computeDefault: () => 256);

    private static readonly Harmony harmony = new($"com.nekometer.esnya.{ModAssembly.GetName()}");
    internal static MetricsPanel? panel;
    internal static MetricsCounter? Writer { get; private set; }

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

        DevCreateNewForm.AddAction("/Editor", "Performance Metrics Counter (Mod)", (_) => Start());
    }

    public static void BeforeHotReload()
    {
        try
        {
            Stop();
            harmony.UnpatchCategory(Category.CORE);
        }
        catch (System.Exception e)
        {
            Error(e);
        }
    }

    private static IEnumerable<string> ParseCommaSeparatedString(string? str)
    {
        return str?.Split(',')?.Select(item => item.Trim()) ?? Enumerable.Empty<string>();
    }


    public static void OnHotReload(ResoniteMod modInstance)
    {
        Init(modInstance);
    }

    public static void Start()
    {
        Msg("Starting Profiler");
        var blackList = ParseCommaSeparatedString(config?.GetValue(blackListKey));
        Writer = new MetricsCounter(blackList);
        panel = new MetricsPanel(Writer, config?.GetValue(panelSizeKey) ?? new float2(1200, 1200), config?.GetValue(maxItemsKey) ?? 256);
        harmony.PatchCategory(Category.PROFILER);
    }

    public static void Stop()
    {
        Msg("Stopping Profiler");
        harmony.UnpatchCategory(Category.PROFILER);
        Writer?.Dispose();
        panel = null;
    }
}
