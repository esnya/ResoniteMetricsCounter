using FrooxEngine;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using ResoniteModLoader;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace ResoniteMetricsCounter.Patch;

[HarmonyPatch]
[HarmonyPatchCategory(Category.PROFILER)]
internal static class Metric_Profiler_Patch
{
    private static readonly Stopwatch stopwatch = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void StartTimer()
    {
        stopwatch.Restart();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Record(IWorldElement element)
    {
        stopwatch.Stop();
        ResoniteMetricsCounterMod.Writer?.AddForCurrentStage(element, stopwatch.ElapsedTicks);
    }

    [HarmonyPatch(typeof(ProtoFluxController), nameof(ProtoFluxController.RunNodeUpdates))]
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> RunNodeUpdatesTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        ResoniteMod.Debug("Patching method for ProtoFluxUpdates");
        var matcher = new CodeMatcher(instructions)
            .MatchStartForward(CodeMatch.Calls(() => default(ProtoFluxNodeGroup)!.RunNodeUpdates()))
            .ThrowIfNotMatchForward("Failed to match RunNodeUpdates call")
            .InsertAndAdvance(
                CodeInstruction.Call(() => StartTimer()),
                new CodeInstruction(OpCodes.Dup)
            )
            .Advance(1)
            .InsertAndAdvance(
                CodeInstruction.Call(() => Record(default!))
            );
        return matcher.Instructions();
    }

    [HarmonyPatch(typeof(ProtoFluxController), nameof(ProtoFluxController.RunContinuousChanges))]
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> RunContinuousChangesTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        ResoniteMod.Debug("Patching method for ProtoFluxContinuousChanges");
        var matcher = new CodeMatcher(instructions)
            .MatchStartForward(CodeMatch.Calls(() => default(ProtoFluxNodeGroup)!.RunNodeChanges()))
            .ThrowIfNotMatchForward("Failed to match RunContinuousChanges call")
            .InsertAndAdvance(
                CodeInstruction.Call(() => StartTimer()),
                new CodeInstruction(OpCodes.Dup)
            )
            .Advance(1)
            .InsertAndAdvance(
                CodeInstruction.Call(() => Record(default!))
            );

        return matcher.Instructions();
    }

    [HarmonyPatch(typeof(UpdateManager), nameof(UpdateManager.RunUpdates))]
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        ResoniteMod.Debug("Patching method for Updates");
        var matcher = new CodeMatcher(instructions)
            .MatchStartForward(CodeMatch.Calls(() => default(IUpdatable)!.InternalRunUpdate()))
            .ThrowIfNotMatchForward("Failed to match InternalRunUpdate call")
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Dup),
                CodeInstruction.Call(() => StartTimer())
            )
            .Advance(1)
            .InsertAndAdvance(
                CodeInstruction.Call(() => Record(default!))
            );

        return matcher.Instructions();
    }

    [HarmonyPatch(typeof(UpdateManager), "ProcessChange")]
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> ProcessChangeTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        ResoniteMod.Debug("Patching method for Changes");
        var matcher = new CodeMatcher(instructions)
            .MatchStartForward(CodeMatch.Calls(() => default(IUpdatable)!.InternalRunApplyChanges(default)))
            .ThrowIfNotMatchForward("Failed to match InternalRunApplyChanges call")
            .InsertAndAdvance(CodeInstruction.Call(() => StartTimer()))
            .Advance(1)
            .InsertAndAdvance(
                CodeInstruction.LoadArgument(1),
                CodeInstruction.Call(() => Record(default!))
            );
        return matcher.Instructions();
    }

    [HarmonyPatch(typeof(UpdateManager), "ProcessConnectorUpdate")]
    [HarmonyTranspiler]
    internal static IEnumerable<CodeInstruction> ProcessConnectorUpdateTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        ResoniteMod.Debug("Patching method for Connectors");
        var matcher = new CodeMatcher(instructions)
            .MatchStartForward(CodeMatch.Calls(() => default(IImplementable)!.InternalUpdateConnector()))
            .ThrowIfNotMatchForward("Failed to match InternalUpdateConnector call")
            .InsertAndAdvance(CodeInstruction.Call(() => StartTimer()))
            .Advance(1)
            .InsertAndAdvance(
                CodeInstruction.LoadArgument(1),
                CodeInstruction.Call(() => Record(default!)
                )
            );
        return matcher.Instructions();
    }

    [HarmonyPatchCategory(Category.PROFILER)]
    [HarmonyPatch]
    internal static class PhysicsMovedHierarchyEventManager_RunMovedEvent_Patch
    {
        internal static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Collider), "Slot_PhysicsWorldTransformChanged");
        }

        internal static void Prefix()
        {
            StartTimer();
        }

        internal static void Postfix(Collider __instance)
        {
            Record(__instance);
        }
    }
}
