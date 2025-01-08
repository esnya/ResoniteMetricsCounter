using Elements.Core;
using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ResoniteMetricsCounter.Metrics;


public interface IMetricStorage<T> where T : IWorldElement
{
    /// <summary>
    /// Total ticks of all metrics.
    /// </summary>
    long Total { get; }

    /// <summary>
    /// Maximum ticks of all metrics.
    /// </summary>
    long Max { get; }

    /// <summary>
    /// Number of metrics.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Enumerate metrics of all stages.
    /// </summary>
    IEnumerable<Metric<T>> Metrics { get; }

    /// <summary>
    /// Add a metric for a target.
    /// </summary>
    /// <param name="target">Target element to add the metric for.</param>
    /// <param name="ticks">Ticks to add to the metric.</param>
    void Add(T target, long ticks, MetricStage stage = MetricStage.Unknown);

    /// <summary>
    /// Remove all metrics for a target.
    /// </summary>
    /// <param name="target">Target element to remove metrics for.</param>
    /// <returns>Cound of metrics removed.</returns>
    int Remove(T target);

    /// <summary>
    /// Remove all metrics that match a predicate.
    /// </summary>
    /// <param name="predicate">Predicate to match metrics to remove.</param>
    int RemoveWhere(Func<Metric<T>, bool> predicate);
}

internal abstract class MetricStorageBase<T> : IMetricStorage<T> where T : IWorldElement
{
    private readonly Dictionary<RefID, Metric<T>> metrics = new();

    public abstract long Total { get; protected set; }

    public long Max { get; private set; }

    public int Count { get => metrics.Count; }

    public IEnumerable<Metric<T>> Metrics => metrics.Values;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void InternalAdd(T target, long ticks, MetricStage stage)
    {
        InternalAdd(target, ticks, stage, metrics);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void InternalAdd(T target, long ticks, MetricStage stage, Dictionary<RefID, Metric<T>> metricsDict)
    {
        var refID = target.ReferenceID;

        if (metricsDict.TryGetValue(refID, out var metric))
        {
            metric.Add(ticks);
            if (metric.Ticks > Max)
            {
                Max = metric.Ticks;
            }
        }
        else
        {
            metricsDict[refID] = new Metric<T>(target, ticks, stage);
            if (ticks > Max)
            {
                Max = ticks;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public abstract void Add(T target, long ticks, MetricStage stage = MetricStage.Unknown);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Remove(T target)
    {
        return metrics.Remove(target.ReferenceID) ? 1 : 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int RemoveWhere(Func<Metric<T>, bool> predicate)
    {
        var query = from metric in Metrics
                    where predicate(metric)
                    select Remove(metric.Target) into n
                    select n;

        return query.Sum();
    }
}


internal sealed class MetricsStorage<T> : MetricStorageBase<T>, IDisposable where T : IWorldElement
{
    private readonly ThreadLocal<Dictionary<RefID, Metric<T>>> parallelMetrics = new(() => new());
    private bool hasParallelMetric;

    public override long Total { get; protected set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Add(T target, long ticks, MetricStage stage = MetricStage.Unknown)
    {
        var refID = target.ReferenceID;

        var isParallel = stage == MetricStage.DynamicBoneChainSimulation;

        if (isParallel)
        {
            hasParallelMetric = true;
            InternalAdd(target, ticks, stage, parallelMetrics.Value);
        }
        if (!isParallel)
        {
            InternalAdd(target, ticks, stage);
            Total += ticks;

            if (hasParallelMetric)
            {
                hasParallelMetric = false;
                var query = from item in parallelMetrics.Values.SelectMany(d => d)
                            group item.Value by item.Key into values
                            select values;

                foreach (var values in query)
                {
                    var maxTicks = values.Max(m => m.Ticks);
                    Total += maxTicks;

                    var metric = values.First();
                    InternalAdd(metric.Target, maxTicks, metric.Stage);
                }
                foreach (var value in parallelMetrics.Values)
                {
                    value.Clear();
                }
            }
        }
    }

    public void Dispose()
    {
        parallelMetrics.Dispose();
    }
}

public sealed class MetricsByStageStorage<T> : IMetricStorage<T> where T : IWorldElement
{
    private sealed class MetricsStorageImpl : MetricStorageBase<T>
    {
        public override long Total { get; protected set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Add(T target, long ticks, MetricStage stage = MetricStage.Unknown)
        {
            InternalAdd(target, ticks, stage);
            Total += ticks;
        }
    }

    private readonly List<MetricsStorageImpl> storageByStage;
    public MetricsByStageStorage()
    {
        var stageCount = Enum.GetValues(typeof(MetricStage)).AsQueryable().Cast<int>().Max() + 1;
        storageByStage = new(Enumerable.Range(0, stageCount).Select(_ => new MetricsStorageImpl()));
    }

    public long Total { get; private set; }
    public long Max { get; private set; }
    public int Count { get => storageByStage.Sum(s => s.Count); }

    public IEnumerable<Metric<T>> Metrics => storageByStage.SelectMany(s => s.Metrics).Where(m => m is not null);

    private void UpdateStats()
    {
        Total = storageByStage.Sum(s => s.Total);
        Max = storageByStage.Max(s => s.Max);
    }

    public void Add(T target, long ticks, MetricStage stage)
    {
        storageByStage[(int)stage].Add(target, ticks, stage);
        UpdateStats();
    }

    public int Remove(T target)
    {
        return storageByStage.Sum(s => s.Remove(target));
    }

    public int RemoveWhere(Func<Metric<T>, bool> predicate)
    {
        var query = from storage in storageByStage
                    select storage.RemoveWhere(predicate) into n
                    select n;
        var result = query.Sum();

        UpdateStats();

        return result;
    }
}
