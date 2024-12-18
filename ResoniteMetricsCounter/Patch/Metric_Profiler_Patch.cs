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
        { "Slot_PhysicsWorldTransformChanged", MetricType.PhysicsMoved },
        { nameof(Component.InternalRunUpdate), MetricType.Updates},
        { nameof(ProtoFluxNodeGroup.RunNodeChanges), MetricType.ProtoFluxContinuousChanges },
        { nameof(ProtoFluxNodeGroup.RunNodeUpdates), MetricType.ProtoFluxUpdates },
        { nameof(Component.InternalRunApplyChanges), MetricType.Changes },
        { nameof(IImplementable.InternalUpdateConnector), MetricType.Connectors }
    };

    static IEnumerable<MethodBase> TargetMethods()
    {
        ResoniteMod.Debug("Patching method for PhysicsMoved");
        yield return AccessTools.Method(typeof(Collider), "Slot_PhysicsWorldTransformChanged");
        ResoniteMod.Debug("Patching method for ProtoFluxContinuousChanges");
        yield return AccessTools.Method(typeof(ProtoFluxNodeGroup), nameof(ProtoFluxNodeGroup.RunNodeChanges));
        ResoniteMod.Debug("Patching method for ProtoFluxUpdates");
        yield return AccessTools.Method(typeof(ProtoFluxNodeGroup), nameof(ProtoFluxNodeGroup.RunNodeUpdates));
        ResoniteMod.Debug("Patching method for Updates");
        yield return AccessTools.Method(typeof(ComponentBase<Component>), nameof(ComponentBase<Component>.InternalRunUpdate));
        ResoniteMod.Debug("Patching method for Changes");
        yield return AccessTools.Method(typeof(ComponentBase<Component>), nameof(ComponentBase<Component>.InternalRunApplyChanges));
        ResoniteMod.Debug("Patching method for Connectors");
        yield return AccessTools.Method(typeof(Slot), nameof(Slot.InternalUpdateConnector));
        yield return AccessTools.Method(typeof(ImplementableComponent<IConnector>), nameof(ImplementableComponent<IConnector>.InternalUpdateConnector));
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

            if (__instance is Slot slot)
            {
                if (slot.IsLocalElement || slot.IsDisposed || slot.World.Focus != World.WorldFocus.Focused)
                {
                    return;
                }

                ResoniteMetricsCounterMod.Writer?.Add(
                    slot.Name,
                    slot,
                    ticks,
                    MetricTypeByMethod[__originalMethod.Name]
                );
            }
            if (__instance is Worker worker)
            {
                if (worker.World.Focus != World.WorldFocus.Focused) return;

                if (worker.Parent is not Slot workerSlot || workerSlot.IsLocalElement)
                {
                    return;
                }

                ResoniteMetricsCounterMod.Writer?.Add(
                    worker.Name,
                    workerSlot,
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

                var nodeSlot = node.Slot;

                ResoniteMetricsCounterMod.Writer?.Add(
                    group.Name,
                    nodeSlot,
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
