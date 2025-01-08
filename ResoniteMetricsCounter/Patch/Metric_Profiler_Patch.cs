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

    private static CodeMatcher InjectProfiler(this CodeMatcher matcher, params CodeMatch[] match)
    {
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

    private static CodeMatcher InjectProfiler(this CodeMatcher matcher, int argumentIndex, params CodeMatch[] match)
    {
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

    private static CodeMatcher InjectProfiler(this CodeMatcher matcher, MetricStage stage, params CodeMatch[] match)
    {
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



    [HarmonyPatchCategory("ProtoFluxUpdates")]
    [HarmonyPatch(typeof(ProtoFluxController), nameof(ProtoFluxController.RunNodeUpdates))]
    private static class RunNodeUpdates_Patch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            ResoniteMod.Debug("Patching method for ProtoFluxUpdates");
            return new CodeMatcher(instructions).InjectProfiler(CodeMatch.Calls(() => default(ProtoFluxNodeGroup)!.RunNodeUpdates())).Instructions();
        }
    }

    [HarmonyPatchCategory("ProtoFluxContinuousChanges")]
    [HarmonyPatch(typeof(ProtoFluxController), nameof(ProtoFluxController.RunContinuousChanges))]
    private static class RunContinuousChanges_Patch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            ResoniteMod.Debug("Patching method for ProtoFluxContinuousChanges");
            return new CodeMatcher(instructions).InjectProfiler(CodeMatch.Calls(() => default(ProtoFluxNodeGroup)!.RunNodeChanges())).Instructions();
        }
    }

    [HarmonyPatchCategory("Updates")]

    [HarmonyPatch(typeof(UpdateManager), nameof(UpdateManager.RunUpdates))]
    private static class RunUpdates_Patch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            ResoniteMod.Debug("Patching method for Updates");
            return new CodeMatcher(instructions).InjectProfiler(CodeMatch.Calls(() => default(IUpdatable)!.InternalRunUpdate())).Instructions();
        }
    }

    [HarmonyPatchCategory(nameof(World.RefreshStage.Changes))]
    [HarmonyPatch(typeof(UpdateManager), "ProcessChange")]
    private static class ProcessChange_Patch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            ResoniteMod.Debug("Patching method for Changes");
            return new CodeMatcher(instructions).InjectProfiler(1, CodeMatch.Calls(() => default(IUpdatable)!.InternalRunApplyChanges(default))).Instructions();
        }
    }

    [HarmonyPatchCategory("Connectors")]
    [HarmonyPatch(typeof(UpdateManager), "ProcessConnectorUpdate")]
    private static class ProcessConnectorUpdate_Patch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            ResoniteMod.Debug("Patching method for Connectors");
            return new CodeMatcher(instructions).InjectProfiler(1, CodeMatch.Calls(() => default(IImplementable)!.InternalUpdateConnector())).Instructions();
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


    [HarmonyPatchCategory(Category.PROFILER)]
    [HarmonyPatch(typeof(DynamicBoneChainManager), nameof(DynamicBoneChainManager.Update))]
    internal static class DynamicBoneChainManager_Update_Patch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            if (ResoniteMetricsCounterMod.GetStageConfigValue(MetricStage.DynamicBoneChainPrepare))
            {
                ResoniteMod.Debug("Patching method for DynamicBoneChainPrepare");
                matcher.InjectProfiler(MetricStage.DynamicBoneChainPrepare, CodeMatch.Calls(AccessTools.Method(typeof(DynamicBoneChain), "Prepare")));
            }

            if (ResoniteMetricsCounterMod.GetStageConfigValue(MetricStage.DynamicBoneChainFinish))
            {
                ResoniteMod.Debug("Patching method for DynamicBoneChainFinish");
                matcher.InjectProfiler(MetricStage.DynamicBoneChainFinish, CodeMatch.Calls(AccessTools.Method(typeof(DynamicBoneChain), "FinishSimulation")));
            }

            return matcher.Instructions();
        }
    }

    [HarmonyPatchCategory("DynamicBoneChainOverlap")]
    [HarmonyPatch]
    internal static class CollisionHandler_Handle_Patch
    {
        internal static MethodBase TargetMethod()
        {
            return typeof(DynamicBoneChainManager).Inner("CollisionHandler").Method("Handle");
        }

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            ResoniteMod.Debug("Patching method for DynamicBoneChainOverlap");
            return new CodeMatcher(instructions)
                .InjectProfiler(MetricStage.DynamicBoneChainOverlaps, CodeMatch.Calls(typeof(DynamicBoneChain).Method("ScheduleCollision")))
                .Instructions();
        }
    }

    [HarmonyPatchCategory("DynamicBoneChainSimulation")]
    [HarmonyPatch(typeof(DynamicBoneChainManager), "SimulateChain")]
    private static class DynamicBoneChainManager_SimulateChain_Patch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            ResoniteMod.Debug("Patching method for DynamicBoneChainSimulation");
            return new CodeMatcher(instructions)
                .InjectProfiler(MetricStage.DynamicBoneChainSimulation, CodeMatch.Calls(AccessTools.Method(typeof(DynamicBoneChain), "RunSimulation")))
                .Instructions();
        }
    }
}
