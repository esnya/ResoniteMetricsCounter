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
    Connectors = World.RefreshStage.Connectors,

    Finished = World.RefreshStage.Finished,

    DynamicBoneChainPrepare,
    DynamicBoneChainOverlaps,
    DynamicBoneChainSimulation,
    DynamicBoneChainFinish,
}
