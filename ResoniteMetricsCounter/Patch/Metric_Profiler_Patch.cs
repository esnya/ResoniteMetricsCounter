using FrooxEngine;
using FrooxEngine.PhotonDust;
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
        ResoniteMod.DebugFunc(() => $"Patching method for {match}");
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
        ResoniteMod.DebugFunc(() => $"Patching method for {match}");
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
        ResoniteMod.DebugFunc(() => $"Patching method for {match}");
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

    [HarmonyPatchCategory(Category.PROFILER), HarmonyPatch]
    private static class UpdateManager_Patch
    {
        internal static readonly MethodBase updatesTarget = typeof(UpdateManager).Method(nameof(UpdateManager.RunUpdates));
        internal static readonly MethodBase changesTarget = typeof(UpdateManager).Method("ProcessChange");
        internal static readonly MethodBase connectorsTarget = typeof(UpdateManager).Method("ProcessConnectorUpdate");


        internal static IEnumerable<MethodBase> TargetMethods()
        {
            if (ResoniteMetricsCounterMod.GetStageConfigValue(MetricStage.Updates))
            {
                yield return updatesTarget;
            }

            if (ResoniteMetricsCounterMod.GetStageConfigValue(MetricStage.Changes))
            {
                yield return changesTarget;
            }

            if (ResoniteMetricsCounterMod.GetStageConfigValue(MetricStage.Connectors))
            {
                yield return connectorsTarget;
            }
        }

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            if (__originalMethod == updatesTarget)
            {
                ResoniteMod.Msg("Patching a profiler for Updates");
                return instructions.InjectProfiler(CodeMatch.Calls(() => default(IUpdatable)!.InternalRunUpdate()));
            }
            else if (__originalMethod == changesTarget)
            {
                ResoniteMod.Msg("Patching a profiler for Changes");
                return instructions.InjectProfiler(1, CodeMatch.Calls(() => default(IUpdatable)!.InternalRunApplyChanges(default)));
            }
            else if (__originalMethod == connectorsTarget)
            {
                ResoniteMod.Msg("Patching a profiler for Connectors");
                return instructions.InjectProfiler(1, CodeMatch.Calls(() => default(IImplementable)!.InternalUpdateConnector()));
            }

            return instructions;
        }
    }

    [HarmonyPatchCategory(Category.PROFILER), HarmonyPatch]
    internal static class ProtoFluxController_Patch
    {
        internal static readonly MethodBase rebuildChangeTrackingTarget = typeof(ProtoFluxController).Method(nameof(ProtoFluxController.RebuildChangeTracking));
        internal static readonly MethodBase rebuildTarget = typeof(ProtoFluxController).Method(nameof(ProtoFluxController.Rebuild));
        internal static readonly MethodBase eventsTarget = typeof(ProtoFluxController).Method(nameof(ProtoFluxController.RunNodeEvents));
        internal static readonly MethodBase updatesTarget = typeof(ProtoFluxController).Method(nameof(ProtoFluxController.RunNodeUpdates));
        internal static readonly MethodBase continuousChangesTarget = typeof(ProtoFluxController).Method(nameof(ProtoFluxController.RunContinuousChanges));
        internal static readonly MethodBase discreteChangesTarget = typeof(ProtoFluxController).Method(nameof(ProtoFluxController.RunDiscreteChanges));

        internal static IEnumerable<MethodBase> TargetMethods()
        {
            if (ResoniteMetricsCounterMod.GetStageConfigValue(MetricStage.ProtoFluxRebuild))
            {
                yield return rebuildChangeTrackingTarget;
                yield return rebuildTarget;
            }

            if (ResoniteMetricsCounterMod.GetStageConfigValue(MetricStage.ProtoFluxEvents))
            {
                yield return eventsTarget;
            }

            if (ResoniteMetricsCounterMod.GetStageConfigValue(MetricStage.ProtoFluxUpdates))
            {
                yield return updatesTarget;
            }

            if (ResoniteMetricsCounterMod.GetStageConfigValue(MetricStage.ProtoFluxContinuousChanges))
            {
                yield return continuousChangesTarget;
            }

            if (ResoniteMetricsCounterMod.GetStageConfigValue(MetricStage.ProtoFluxDiscreteChangesPre) || ResoniteMetricsCounterMod.GetStageConfigValue(MetricStage.ProtoFluxDiscreteChangesPost))
            {
                yield return discreteChangesTarget;
            }
        }

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            if (__originalMethod == rebuildChangeTrackingTarget)
            {
                ResoniteMod.Msg("Patching a profiler for ProtoFluxRebuild");
                return instructions.InjectProfiler(CodeMatch.Calls(() => default(ProtoFluxNodeGroup)!.RebuildChangeTracking()));
            }

            else if (__originalMethod == rebuildTarget)
            {
                return instructions.InjectProfiler(CodeMatch.Calls(() => default(ProtoFluxNodeGroup)!.Rebuild()));
            }

            else if (__originalMethod == eventsTarget)
            {
                ResoniteMod.Msg("Patching a profiler for ProtoFluxEvents");
                return instructions.InjectProfiler(CodeMatch.Calls(() => default(ProtoFluxNodeGroup)!.RunNodeEvents()));
            }
            else if (__originalMethod == updatesTarget)
            {
                ResoniteMod.Msg("Patching a profiler for ProtoFluxUpdates");
                return instructions.InjectProfiler(CodeMatch.Calls(() => default(ProtoFluxNodeGroup)!.RunNodeUpdates()));
            }
            else if (__originalMethod == continuousChangesTarget)
            {
                ResoniteMod.Msg("Patching a profiler for ProtoFluxContinuousChanges");
                return instructions.InjectProfiler(CodeMatch.Calls(() => default(ProtoFluxNodeGroup)!.RunNodeChanges()));
            }
            else
            {
                if (__originalMethod == discreteChangesTarget)
                {
                    ResoniteMod.Msg("Patching a profiler for ProtoFluxDiscreteChangesPre/Post");
                    return instructions.InjectProfiler(CodeMatch.Calls(() => default(ProtoFluxNodeGroup)!.RunNodeChanges()));
                }
            }

            return instructions;
        }
    }

    [HarmonyPatchCategory("ParticleSystems"), HarmonyPatch(typeof(ParticleSystemManager), nameof(ParticleSystemManager.Update))]
    internal static class ParticleSystemManager_Patch
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.InjectProfiler(CodeMatch.Calls(typeof(ParticleSystem).Method("ScheduleUpdate")));
    }

    [HarmonyPatchCategory(Category.PROFILER), HarmonyPatch]
    internal static class DynamicBoneChainManager_Patch
    {
        internal static readonly MethodBase updateTarget = typeof(DynamicBoneChainManager).Method(nameof(DynamicBoneChainManager.Update));
        internal static readonly MethodBase simulateChainTarget = typeof(DynamicBoneChainManager).Method("SimulateChain");

        internal static IEnumerable<MethodBase> TargetMethods()
        {
            if (ResoniteMetricsCounterMod.GetStageConfigValue(MetricStage.DynamicBoneChainPrepare) || ResoniteMetricsCounterMod.GetStageConfigValue(MetricStage.DynamicBoneChainFinish))
            {
                yield return updateTarget;
            }
            if (ResoniteMetricsCounterMod.GetStageConfigValue(MetricStage.DynamicBoneChainSimulation))
            {
                yield return simulateChainTarget;
            }
        }

        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            if (__originalMethod == updateTarget)
            {
                ResoniteMod.Msg("Patching a profiler for DynamicBoneChainPrepare/Finish");

                if (ResoniteMetricsCounterMod.GetStageConfigValue(MetricStage.DynamicBoneChainPrepare))
                {
                    instructions = instructions.InjectProfiler(MetricStage.DynamicBoneChainPrepare, CodeMatch.Calls(typeof(DynamicBoneChain).Method("Prepare")));
                }

                if (ResoniteMetricsCounterMod.GetStageConfigValue(MetricStage.DynamicBoneChainFinish))
                {
                    instructions = instructions.InjectProfiler(MetricStage.DynamicBoneChainFinish, CodeMatch.Calls(typeof(DynamicBoneChain).Method("FinishSimulation")));
                }

                return instructions;
            }
            else if (__originalMethod == simulateChainTarget)
            {
                ResoniteMod.Msg("Patching a profiler for DynamicBoneChainSimulation");
                return instructions.InjectProfiler(MetricStage.DynamicBoneChainSimulation, CodeMatch.Calls(typeof(DynamicBoneChain).Method("RunSimulation")));
            }

            return instructions;
        }
    }

    [HarmonyPatchCategory("DynamicBoneChainOverlap")]
    internal static class CollisionHandler_Handle_Patch
    {
        internal static MethodBase TargetMethod() => typeof(DynamicBoneChainManager).Inner("CollisionHandler").Method("Handle");
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => instructions.InjectProfiler(MetricStage.DynamicBoneChainOverlaps, CodeMatch.Calls(typeof(DynamicBoneChain).Method("ScheduleCollision")));
    }
}
