using ResoniteMetricsCounter.Metrics;
using System.Collections.Generic;

namespace ResoniteMetricsCounter.Utils;

internal struct Constants
{
    public const float ROWHEIGHT = 32;
    public const float HEIGHT = 24;
    public const float FIXEDWIDTH = 32 * 3;
    public const float PADDING = 4;
    public const float SPACING = 8;
    public const float FLEX = 1;

    public static readonly HashSet<MetricStage> CollectableStages = new()
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
        MetricStage.Connectors,
        MetricStage.DynamicBoneChainPrepare,
        MetricStage.DynamicBoneChainOverlaps,
        MetricStage.DynamicBoneChainSimulation,
        MetricStage.DynamicBoneChainFinish,
    };

    public static readonly HashSet<MetricStage> DefaultStageConfig = new()
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
