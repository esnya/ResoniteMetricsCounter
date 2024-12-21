using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using HarmonyLib;
using ResoniteModLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace ResoniteMetricsCounter.Patch;

[HarmonyPatch]
[HarmonyPatchCategory(Category.PROFILER)]
internal static class Metric_Profiler_Patch
{
    internal sealed class CounterContext : IPoolable
    {
        private readonly Stopwatch stopwatch = new();

        public int Ticks => (int)stopwatch.ElapsedTicks;

        public void Start()
        {
            stopwatch.Start();
        }

        public void Clean()
        {
            stopwatch.Stop();
            stopwatch.Reset();
        }
    }

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

    static void Prefix(out CounterContext __state)
    {
        __state = Pool.Borrow<CounterContext>();
        __state.Start();
    }

    static void Postfix(object __instance, ref CounterContext __state)
    {
        try
        {
            var ticks = __state.Ticks;
            Pool.Return(ref __state);

            ResoniteMetricsCounterMod.Writer?.AddForCurrentStage(
                __instance,
                ticks
            );
        }
        catch (Exception e)
        {
            ResoniteMod.Error("Failed to add metric");
            ResoniteMod.Error(e);
        }
    }
}
