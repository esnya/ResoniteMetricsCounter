using FrooxEngine;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using ResoniteMetricsCounter.Metrics;
using ResoniteModLoader;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ResoniteMetricsCounter.Patch;

#pragma warning disable CA1859

internal static class Metric_Profiler_Patch
{
    private static readonly ThreadLocal<Stopwatch> stopwatch = new(() => new Stopwatch());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void StartTimer()
    {
        stopwatch.Value.Restart();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Record(object obj)
    {
        stopwatch.Value.Stop();
        ResoniteMetricsCounterMod.Writer?.AddForCurrentStage(obj, stopwatch.Value.ElapsedTicks);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Record(object obj, MetricStage stage)
    {
        stopwatch.Value.Stop();
        if (obj is IWorldElement element)
        {
            ResoniteMetricsCounterMod.Writer?.Add(element, stopwatch.Value.ElapsedTicks, stage);
        }
    }

    private static CodeMatcher InjectProfiler(this CodeMatcher matcher, CodeMatch match)
    {
        ResoniteMod.Msg($"Patching method for {match}");
        return matcher
            .MatchStartForward(match)
            .ThrowIfNotMatchForward("Failed to inject profiler")
            .InsertAndAdvance(
                CodeInstruction.Call(() => StartTimer()),
                new CodeInstruction(OpCodes.Dup)
            )
            .Advance(1)
            .InsertAndAdvance(
                CodeInstruction.Call(() => Record(default!))
            );
    }

    private static IEnumerable<CodeInstruction> InjectProfiler(this IEnumerable<CodeInstruction> instructions, CodeMatch match)
    {
        return new CodeMatcher(instructions).InjectProfiler(match).Instructions();
    }


    private static CodeMatcher InjectProfiler(this CodeMatcher matcher, int argumentIndex, CodeMatch match)
    {
        ResoniteMod.Msg($"Patching method for {match}");
        return matcher
        .MatchStartForward(match)
        .ThrowIfNotMatchForward("Failed to inject profiler")
        .InsertAndAdvance(
            CodeInstruction.Call(() => StartTimer())
        )
        .Advance(1)
        .InsertAndAdvance(
            CodeInstruction.LoadArgument(argumentIndex),
            CodeInstruction.Call(() => Record(default!))
        );
    }
    private static IEnumerable<CodeInstruction> InjectProfiler(this IEnumerable<CodeInstruction> instructions, int argumentIndex, CodeMatch match)
    {
        return new CodeMatcher(instructions).InjectProfiler(argumentIndex, match).Instructions();
    }

    private static CodeMatcher InjectProfiler(this CodeMatcher matcher, MetricStage stage, CodeMatch match)
    {
        ResoniteMod.Msg($"Patching method for {match}");
        return matcher
        .MatchStartForward(match)
        .ThrowIfNotMatchForward("Failed to inject profiler")
        .InsertAndAdvance(
            CodeInstruction.Call(() => StartTimer()),
            new CodeInstruction(OpCodes.Dup)
        )
        .Advance(1)
        .InsertAndAdvance(
            new CodeInstruction(OpCodes.Ldc_I4, (int)stage),
            CodeInstruction.Call(() => Record(default!, stage))
        );
    }
    private static IEnumerable<CodeInstruction> InjectProfiler(this IEnumerable<CodeInstruction> instructions, MetricStage stage, CodeMatch match)
    {
        return new CodeMatcher(instructions).InjectProfiler(stage, match).Instructions();
    }

    [HarmonyPatchCategory("PhysicsMoved"), HarmonyPatch]
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


    [HarmonyPatchCategory("Updates"), HarmonyPatch(typeof(UpdateManager), nameof(UpdateManager.RunUpdates))]
    private static class UpdateManager_RunUpdates_Patch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.InjectProfiler(CodeMatch.Calls(() => default(IUpdatable)!.InternalRunUpdate()));
    }

    [HarmonyPatchCategory("Changes"), HarmonyPatch(typeof(UpdateManager), "ProcessChange")]
    internal static class UpdateManager_ProcessChange_Patch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.InjectProfiler(1, CodeMatch.Calls(() => default(IUpdatable)!.InternalRunApplyChanges(default)));
    }

    [HarmonyPatchCategory("Connectors"), HarmonyPatch(typeof(UpdateManager), "ProcessConnectorUpdate")]
    internal static class UpdateManager_ProcessConnectorUpdate_Patch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.InjectProfiler(1, CodeMatch.Calls(() => default(IImplementable)!.InternalUpdateConnector()));
    }

    [HarmonyPatchCategory("ProtoFluxRebuild"), HarmonyPatch(typeof(ProtoFluxController), nameof(ProtoFluxController.RebuildChangeTracking))]
    private static class ProtoFluxController_RebuildChangeTracking_Patch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.InjectProfiler(CodeMatch.Calls(() => default(ProtoFluxNodeGroup)!.RebuildChangeTracking()));
    }


    [HarmonyPatchCategory("ProtoFluxRebuild"), HarmonyPatch(typeof(ProtoFluxController), nameof(ProtoFluxController.Rebuild))]
    private static class ProtoFluxController_Rebuild_Patch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.InjectProfiler(CodeMatch.Calls(() => default(ProtoFluxNodeGroup)!.Rebuild()));
    }


    [HarmonyPatchCategory("ProtoFluxEvents"), HarmonyPatch(typeof(ProtoFluxController), nameof(ProtoFluxController.RunNodeEvents))]
    private static class ProtoFluxController_RunNodeEvents_Patch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.InjectProfiler(CodeMatch.Calls(() => default(ProtoFluxNodeGroup)!.RunNodeEvents()));
    }


    [HarmonyPatchCategory("ProtoFluxUpdates"), HarmonyPatch(typeof(ProtoFluxController), nameof(ProtoFluxController.RunNodeUpdates))]
    private static class ProtoFluxController_RunNodeUpdates_Patch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.InjectProfiler(CodeMatch.Calls(() => default(ProtoFluxNodeGroup)!.RunNodeUpdates()));
    }


    [HarmonyPatchCategory("ProtoFluxContinuousChanges"), HarmonyPatch(typeof(ProtoFluxController), nameof(ProtoFluxController.RunContinuousChanges))]
    private static class ProtoFluxController_RunContinuousChanges_Patch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.InjectProfiler(CodeMatch.Calls(() => default(ProtoFluxNodeGroup)!.RunNodeChanges()));
    }

    [HarmonyPatchCategory("ProtoFluxDiscreteChangesPre"), HarmonyPatch(typeof(ProtoFluxController), nameof(ProtoFluxController.RunDiscreteChanges))]
    private static class ProtoFluxController_RunDiscreteChanges_Patch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.InjectProfiler(CodeMatch.Calls(() => default(ProtoFluxNodeGroup)!.RunNodeChanges()));
    }

    [HarmonyPatchCategory("DynamicBoneChainPrepare"), HarmonyPatch(typeof(DynamicBoneChainManager), nameof(DynamicBoneChainManager.Update))]
    internal static class DynamicBoneChain_Prepare_Patch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.InjectProfiler(CodeMatch.Calls(typeof(DynamicBoneChain).Method("Prepare")));
    }

    [HarmonyPatchCategory("DynamicBoneChainOverlap")]
    internal static class CollisionHandler_Handle_Patch
    {
        internal static MethodBase TargetMethod() => typeof(DynamicBoneChainManager).Inner("CollisionHandler").Method("Handle");
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.InjectProfiler(MetricStage.DynamicBoneChainOverlaps, CodeMatch.Calls(typeof(DynamicBoneChain).Method("ScheduleCollision")));
    }

    [HarmonyPatchCategory("DynamicBoneChainSimulation"), HarmonyPatch(typeof(DynamicBoneChainManager), "SimulateChain")]
    private static class DynamicBoneChainManager_SimulateChain_Patch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.InjectProfiler(MetricStage.DynamicBoneChainSimulation, CodeMatch.Calls(typeof(DynamicBoneChain).Method("RunSimulation")));
    }

    [HarmonyPatchCategory("DynamicBoneChainFinish"), HarmonyPatch(typeof(DynamicBoneChainManager), "Update")]
    internal static class DynamicBoneChain_FinishSimulation_Patch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.InjectProfiler(CodeMatch.Calls(AccessTools.Method(typeof(DynamicBoneChain), "FinishSimulation")));
    }
}
