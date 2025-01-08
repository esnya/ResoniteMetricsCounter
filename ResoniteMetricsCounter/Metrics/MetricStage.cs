using FrooxEngine;

namespace ResoniteMetricsCounter.Metrics;

/// <summary>
/// Represents the stage of a metric.
/// </summary>
public enum MetricStage
{
    Unknown = 0,

    PhysicsMoved = World.RefreshStage.PhysicsMoved,
    Updates = World.RefreshStage.Updates,
    ProtoFluxUpdates = World.RefreshStage.ProtoFluxUpdates,
    ProtoFluxContinuousChanges = World.RefreshStage.ProtoFluxContinuousChanges,
    Changes = World.RefreshStage.Changes,
    Connectors = World.RefreshStage.Connectors,

    Finished = World.RefreshStage.Finished,

    DynamicBoneChainPrepare,
    DynamicBoneChainOverlaps,
    DynamicBoneChainSimulation,
    DynamicBoneChainFinish,
}
