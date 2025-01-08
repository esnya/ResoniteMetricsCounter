using FrooxEngine;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ResoniteMetricsCounter.Metrics;

/// <summary>
/// Represents a metric that is associated with a specific target element.
/// </summary>
/// <typeparam name="T">The type of the target element, which must implement <see cref="IWorldElement"/>.</typeparam>
public class Metric<T> where T : IWorldElement
{
    /// <summary>
    /// Target element of the metric.
    /// </summary>
    [JsonInclude] public T Target { get; private set; }

    /// <summary>
    /// Stage of the metric.
    /// </summary>  
    [JsonInclude] public MetricStage Stage { get; private set; }

    /// <summary>
    /// Ticks of the metric.
    /// </summary>
    [JsonInclude] public long Ticks { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Metric{T}"/> class.
    /// </summary>
    /// <param name="target">The target element of the metric.</param>
    /// <param name="stage">The stage of the metric. Default is <see cref="MetricStage.Unknown"/>.</param>
    /// <param name="ticks">The initial number of ticks. Default is 0.</param>
    public Metric(T target, long ticks, MetricStage stage)
    {
        Target = target;
        Ticks = ticks;
        Stage = stage;
    }

    /// <summary>
    /// Adds the specified number of ticks to the metric.
    /// </summary>
    /// <param name="ticks">The number of ticks to add.</param>
    /// <throws><see cref="OverflowException"/> if the result is greater than <see cref="long.MaxValue"/>.</throws>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(long ticks)
    {
        Ticks += ticks;
    }
}
