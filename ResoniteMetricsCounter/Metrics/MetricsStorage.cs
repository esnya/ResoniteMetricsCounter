using Elements.Core;
using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ResoniteMetricsCounter.Metrics;


public interface IMetricStorage<TElement, TMetric> where TElement : IWorldElement where TMetric : Metric<TElement>
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
    IEnumerable<TMetric> Metrics { get; }

    /// <summary>
    /// Add a metric for a target.
    /// </summary>
    /// <param name="target">Target element to add the metric for.</param>
    /// <param name="ticks">Ticks to add to the metric.</param>
    void Add(TElement target, long ticks);

    /// <summary>
    /// Remove all metrics for a target.
    /// </summary>
    /// <param name="target">Target element to remove metrics for.</param>
    /// <returns>Cound of metrics removed.</returns>
    int Remove(TElement target);

    /// <summary>
    /// Remove all metrics that match a predicate.
    /// </summary>
    /// <param name="predicate">Predicate to match metrics to remove.</param>
    int RemoveWhere(Func<TMetric, bool> predicate);
}


public abstract class MetricsStorageBase<TElement, TMetric> : IMetricStorage<TElement, TMetric> where TElement : IWorldElement where TMetric : Metric<TElement>
{
    private readonly Dictionary<RefID, TMetric> metrics = new();

    public long Total { get; private set; }

    public long Max { get; private set; }

    public int Count { get => metrics.Count; }

    public IEnumerable<TMetric> Metrics => metrics.Values;

    protected abstract TMetric CreateMetric(in TElement target, long ticks);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(TElement target, long ticks)
    {
        var refID = target.ReferenceID;

        if (metrics.TryGetValue(refID, out var metric))
        {
            metric.Add(ticks);
            if (metric.Ticks > Max)
            {
                Max = metric.Ticks;
            }
        }
        else
        {
            metrics[refID] = CreateMetric(target, ticks);
            if (ticks > Max)
            {
                Max = ticks;
            }
        }

        Total += ticks;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Remove(TElement target)
    {
        return metrics.Remove(target.ReferenceID) ? 1 : 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int RemoveWhere(Func<TMetric, bool> predicate)
    {
        var query = from metric in Metrics
                    where predicate(metric)
                    select Remove(metric.Target) into n
                    select n;

        return query.Sum();
    }
}

public sealed class MetricsStorage<T> : MetricsStorageBase<T, Metric<T>> where T : IWorldElement
{
    protected override Metric<T> CreateMetric(in T target, long ticks)
    {
        return new(target, ticks);
    }
}


public sealed class MetricsByStageStorage<T> : IMetricStorage<T, StageMetric<T>> where T : IWorldElement
{
    internal sealed class StorageImpl : MetricsStorageBase<T, StageMetric<T>>
    {
        protected override StageMetric<T> CreateMetric(in T target, long ticks)
        {
            return new(target.World.Stage, target, ticks);
        }
    }

    private readonly List<StorageImpl> storageByStage;
    public MetricsByStageStorage()
    {
        var stageCount = Enum.GetValues(typeof(World.RefreshStage)).AsQueryable().Cast<int>().Max() + 1;
        storageByStage = new(Enumerable.Range(0, stageCount).Select(_ => new StorageImpl()));
    }

    public long Total { get; private set; }
    public long Max { get; private set; }
    public int Count { get => storageByStage.Sum(s => s.Count); }

    public IEnumerable<StageMetric<T>> Metrics => storageByStage.SelectMany(s => s.Metrics);

    private void UpdateStats()
    {
        Total = storageByStage.Sum(s => s.Total);
        Max = storageByStage.Max(s => s.Max);
    }

    public void Add(T target, long ticks)
    {
        var stage = target.World.Stage;

        storageByStage[(int)stage].Add(target, ticks);

        UpdateStats();
    }

    public int Remove(T target)
    {
        return storageByStage.Sum(s => s.Remove(target));
    }

    public int RemoveWhere(Func<StageMetric<T>, bool> predicate)
    {
        var query = from storage in storageByStage
                    select storage.RemoveWhere(predicate) into n
                    select n;
        var result = query.Sum();

        UpdateStats();

        return result;
    }
}
