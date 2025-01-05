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

#pragma warning disable CA1859

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

    [HarmonyPatchCategory("ProtoFluxUpdates")]
    [HarmonyPatch(typeof(ProtoFluxController), nameof(ProtoFluxController.RunNodeUpdates))]
    private static class RunNodeUpdates_Patch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
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
    }

    [HarmonyPatchCategory("ProtoFluxContinuousChanges")]
    [HarmonyPatch(typeof(ProtoFluxController), nameof(ProtoFluxController.RunContinuousChanges))]
    private static class RunContinuousChanges_Patch
    {

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
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
    }

    [HarmonyPatchCategory("Updates")]

    [HarmonyPatch(typeof(UpdateManager), nameof(UpdateManager.RunUpdates))]
    private static class RunUpdates_Patch
    {
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
    }

    [HarmonyPatchCategory(nameof(World.RefreshStage.Changes))]
    [HarmonyPatch(typeof(UpdateManager), "ProcessChange")]
    private static class ProcessChange_Patch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
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
    }

    [HarmonyPatchCategory("Connectors")]
    [HarmonyPatch(typeof(UpdateManager), "ProcessConnectorUpdate")]
    private static class ProcessConnectorUpdate_Patch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            ResoniteMod.Debug("Patching method for Connectors");
            var matcher = new CodeMatcher(instructions)
                .MatchStartForward(CodeMatch.Calls(() => default(IImplementable)!.InternalUpdateConnector()))
                .ThrowIfNotMatchForward("Failed to match InternalUpdateConnector call")
                .InsertAndAdvance(CodeInstruction.Call(() => StartTimer()))
                .Advance(1)
                .InsertAndAdvance(
                    CodeInstruction.LoadArgument(1),
                    CodeInstruction.Call(() => Record(default!))
                );
            return matcher.Instructions();
        }
    }

    [HarmonyPatchCategory("PhysicsMoved")]
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
