using FrooxEngine;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using ResoniteMetricsCounter.Metrics;
using ResoniteMetricsCounter.Utils;
using ResoniteModLoader;
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
    private static readonly StopwatchPool stopwatch = new();

    private static readonly Dictionary<string, MetricType> MetricTypeByMethod = new() {
        { nameof(Component.InternalRunApplyChanges), MetricType.Changes },
        { nameof(Component.InternalRunUpdate), MetricType.Updates},
        { "Slot_PhysicsWorldTransformChanged", MetricType.PhysicsMoved },
        { nameof(ProtoFluxNodeGroup.RunNodeChanges), MetricType.ProtoFluxContinuousChanges },
        { nameof(ProtoFluxNodeGroup.RunNodeUpdates), MetricType.ProtoFluxUpdates },
    };

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
        __state = stopwatch.GetAndStart();
    }

    static void Postfix(object __instance, MethodBase __originalMethod, Stopwatch __state)
    {
        try
        {
            var ticks = stopwatch.Release(__state);

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
                ResoniteMod.DebugFunc(() => $"Unknown instance type: {__instance}");
            }
        }
        catch (Exception e)
        {
            ResoniteMod.Error("Failed to add metric");
            ResoniteMod.Error(e);
        }
    }
}
