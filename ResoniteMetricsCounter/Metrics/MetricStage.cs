using System.Collections.Generic;
using FrooxEngine;

namespace ResoniteMetricsCounter.Metrics;

/// <summary>
/// Represents the stage of a metric.
/// </summary>
public enum MetricStage
{
    Unknown = 0,

    PhysicsSync = World.RefreshStage.PhysicsSync,
    PhysicsMoved = World.RefreshStage.PhysicsMoved,
    PhysicsUpdate = World.RefreshStage.PhysicsUpdate,
    Updates = World.RefreshStage.Updates,
    ProtoFluxRebuild = World.RefreshStage.ProtoFluxRebuild,
    ProtoFluxEvents = World.RefreshStage.ProtoFluxEvents,
    ProtoFluxUpdates = World.RefreshStage.ProtoFluxUpdates,
    ProtoFluxContinuousChanges = World.RefreshStage.ProtoFluxContinuousChanges,
    ProtoFluxDiscreteChangesPre = World.RefreshStage.ProtoFluxDiscreteChangesPre,
    Changes = World.RefreshStage.Changes,
    ProtoFluxDiscreteChangesPost = World.RefreshStage.ProtoFluxDiscreteChangesPost,
    ParticleSystems = World.RefreshStage.ParticleSystems,
    Connectors = World.RefreshStage.Connectors,

    Finished = World.RefreshStage.Finished,

    DynamicBoneChainPrepare,
    DynamicBoneChainOverlaps,
    DynamicBoneChainSimulation,
    DynamicBoneChainFinish,
}

public static class MetricStageUtils
{
    public static readonly HashSet<MetricStage> Collectables =
        new()
        {
            MetricStage.PhysicsMoved,
            MetricStage.Updates,
            MetricStage.ProtoFluxRebuild,
            MetricStage.ProtoFluxEvents,
            MetricStage.ProtoFluxUpdates,
            MetricStage.ProtoFluxContinuousChanges,
            MetricStage.ProtoFluxDiscreteChangesPre,
            MetricStage.Changes,
            MetricStage.ProtoFluxDiscreteChangesPost,
            MetricStage.ParticleSystems,
            MetricStage.Connectors,
            MetricStage.DynamicBoneChainPrepare,
            MetricStage.DynamicBoneChainOverlaps,
            MetricStage.DynamicBoneChainSimulation,
            MetricStage.DynamicBoneChainFinish,
        };

    public static readonly HashSet<MetricStage> Defaults =
        new()
        {
            MetricStage.PhysicsMoved,
            MetricStage.Updates,
            MetricStage.ProtoFluxUpdates,
            MetricStage.ProtoFluxContinuousChanges,
            MetricStage.Changes,
            MetricStage.DynamicBoneChainSimulation,
            MetricStage.DynamicBoneChainFinish,
        };
}
