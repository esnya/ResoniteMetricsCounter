using FrooxEngine;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ResoniteMetricsCounter.Metrics;


internal class Metric<T> where T : IWorldElement
{
    /// <summary>
    /// Target element of the metric.
    /// </summary>
    [JsonInclude] public readonly T Target;

    /// <summary>
    /// Ticks of the metric.
    /// </summary>
    [JsonInclude] public long Ticks { get; private set; }


    public Metric(T target, long ticks = 0)
    {
        Target = target;
        Ticks = ticks;
    }


    /// <summary>
    /// Add ticks to the metric.
    /// </summary>
    /// <param name="ticks">Ticks to add.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(long ticks)
    {
        Ticks += ticks;
    }
}

internal sealed class StageMetric<T> : Metric<T> where T : IWorldElement
{
    /// <summary>
    /// World reflesh stage of the metric.
    /// </summary>
    [JsonInclude] public readonly World.RefreshStage Stage;

    public StageMetric(World.RefreshStage stage, T target, long ticks = 0) : base(target, ticks)
    {
        Stage = stage;
    }
}
