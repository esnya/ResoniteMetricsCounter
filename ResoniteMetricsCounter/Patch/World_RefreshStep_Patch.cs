using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;
using System;

namespace ResoniteMetricsCounter.Patch;

[HarmonyPatch(typeof(World), "RefreshStep")]
[HarmonyPatchCategory(Category.PROFILER)]
internal static class World_RefreshStep_Patch
{
    public static void Postfix(World __instance)
    {
        try
        {
            // Connectors stage has been removed in THE SPLITTENING.
            // Update the panel right before the Finished stage (end of in-process work).
            if (__instance.Focus != World.WorldFocus.Focused || __instance.Stage != World.RefreshStage.Finished - 1) return;
            ResoniteMetricsCounterMod.Panel?.Update();
        }
        catch (Exception e)
        {
            ResoniteMod.Error("Failed to update Resonite Profiler panel.");
            ResoniteMod.Error(e);
        }
    }
}
