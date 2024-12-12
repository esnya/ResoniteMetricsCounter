using FrooxEngine;
using HarmonyLib;
using System;

namespace ResoniteMetricsCounter.Patch;

[HarmonyPatch(typeof(World), "RefreshStep")]
[HarmonyPatchCategory(Category.PROFILER)]
internal static class World_RefleshStep_Patch
{
    public static void Postfix(World __instance)
    {
        if (__instance.Focus != World.WorldFocus.Focused || __instance.Stage != World.RefreshStage.Connectors - 1) return;

        try
        {
            ResoniteMetricsCounterMod.panel?.Update();
        }
        catch (Exception e)
        {
            ResoniteMetricsCounterMod.Error("Failed to update Resonite Profiler panel.");
            ResoniteMetricsCounterMod.Error(e);
        }
    }
}
