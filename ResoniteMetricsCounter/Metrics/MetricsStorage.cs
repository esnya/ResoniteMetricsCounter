using Elements.Core;
using FrooxEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

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


public class MetricsStorage<T> : IMetricStorage<T> where T : IWorldElement
{
    private readonly Dictionary<RefID, Metric<T>> metrics = new();

    public long Total { get; private set; }

    public long Max { get; private set; }

    public int Count { get => metrics.Count; }

    public IEnumerable<Metric<T>> Metrics => metrics.Values;


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T target, long ticks, MetricStage stage = MetricStage.Unknown)
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
            metrics[refID] = new Metric<T>(target, ticks, stage);
            if (ticks > Max)
            {
                Max = ticks;
            }
        }

        Total += ticks;
    }

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

public sealed class MetricsByStageStorage<T> : IMetricStorage<T> where T : IWorldElement
{

    private readonly List<MetricsStorage<T>> storageByStage;
    public MetricsByStageStorage()
    {
        var stageCount = Enum.GetValues(typeof(MetricStage)).AsQueryable().Cast<int>().Max() + 1;
        storageByStage = new(Enumerable.Range(0, stageCount).Select(_ => new MetricsStorage<T>()));
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
