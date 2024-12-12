using FrooxEngine;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using ResoniteMetricsCounter.Metrics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;


namespace ResoniteMetricsCounter.Patch;

[HarmonyPatch]
[HarmonyPatchCategory(Category.PROFILER)]
internal static class Metric_Profiler_Patch
{
    private static readonly Dictionary<string, MetricType> MetricTypeByMethod = new() {
        { nameof(Component.InternalRunApplyChanges), MetricType.Changes },
        { nameof(Component.InternalRunUpdate), MetricType.Updates},
        { "Slot_PhysicsWorldTransformChanged", MetricType.PhysicsMoved },
        { nameof(ProtoFluxNodeGroup.RunNodeChanges), MetricType.ProtoFluxContinuousChanges },
        { nameof(ProtoFluxNodeGroup.RunNodeUpdates), MetricType.ProtoFluxUpdates },
    };

    static readonly LinkedList<Stopwatch> stopwatchPool = new();
    static Stopwatch GetAndStartStopwatch()
    {
        if (stopwatchPool.Count == 0)
        {
            return Stopwatch.StartNew();
        }
        var stopwatch = stopwatchPool.Last.Value;
        stopwatchPool.RemoveLast();
        stopwatch.Restart();
        return stopwatch;
    }

    static long ReleaseStopwatch(Stopwatch stopwatch)
    {
        stopwatch.Stop();
        var result = stopwatch.ElapsedTicks;
        stopwatchPool.AddLast(stopwatch);
        return result;
    }

    static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(ComponentBase<Component>), nameof(ComponentBase<Component>.InternalRunApplyChanges));
        yield return AccessTools.Method(typeof(ComponentBase<Component>), nameof(ComponentBase<Component>.InternalRunUpdate));
        yield return AccessTools.Method(typeof(Collider), "Slot_PhysicsWorldTransformChanged");
        yield return AccessTools.Method(typeof(ProtoFluxNodeGroup), nameof(ProtoFluxNodeGroup.RunNodeChanges));
        yield return AccessTools.Method(typeof(ProtoFluxNodeGroup), nameof(ProtoFluxNodeGroup.RunNodeUpdates));
    }

    static void Prefix(out Stopwatch __state)
    {
        __state = GetAndStartStopwatch();
    }

    static void Postfix(object __instance, MethodBase __originalMethod, Stopwatch __state)
    {
        try
        {
            var ticks = ReleaseStopwatch(__state);

            if (__instance is Worker worker)
            {
                if (worker.World.Focus != World.WorldFocus.Focused) return;

                if (worker.Parent is not Slot slot || slot.IsLocalElement)
                {
                    return;
                }

                ResoniteMetricsCounterMod.Writer?.Add(
                    worker.Name,
                    slot,
                    ticks,
                    MetricTypeByMethod[__originalMethod.Name]
                );
            }
            else if (__instance is ProtoFluxNodeGroup group)
            {
                if (group.World.Focus != World.WorldFocus.Focused) return;

                var node = group.Nodes.FirstOrDefault();
                if (node is null)
                {
                    return;
                }

                var slot = node.Slot;

                ResoniteMetricsCounterMod.Writer?.Add(
                    group.Name,
                    slot,
                    ticks,
                    MetricTypeByMethod[__originalMethod.Name]
                );
            }
            else
            {
                ResoniteMetricsCounterMod.Debug($"Unknown instance type: {__instance}");
            }
        }
        catch (Exception e)
        {
            ResoniteMetricsCounterMod.Error("Failed to add metric");
            ResoniteMetricsCounterMod.Error(e);
        }
    }
}
