using System;
using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;

namespace ResoniteMetricsCounter.Patch;

[HarmonyPatch(typeof(World), "RefreshStep")]
[HarmonyPatchCategory(Category.PROFILER)]
internal static class World_RefreshStep_Patch
{
    public static void Postfix(World __instance)
    {
        try
        {
            if (
                __instance.Focus != World.WorldFocus.Focused
                || __instance.Stage != World.RefreshStage.Connectors - 1
            )
            {
                return;
            }

            ResoniteMetricsCounterMod.Panel?.Update();
        }
# pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception e)
# pragma warning restore CA1031 // Do not catch general exception types
        {
            // Log the error to the console
            ResoniteMod.Error("Failed to update Resonite Profiler panel.");
            ResoniteMod.Error(e);
        }
    }
}
