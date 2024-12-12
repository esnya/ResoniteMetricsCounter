using Elements.Core;
using FrooxEngine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ResoniteMetricsCounter.Metrics;

internal sealed class MetricsCounter : IDisposable
{
    internal Dictionary<int, Metric> Metrics = new();
    private readonly string filename;
    private HashSet<string> blackList;
    private Slot? ignoredHierarchy;
    internal long TotalTicks
    {
        get;
        private set;
    }
    internal long MaxTicks
    {
        get;
        private set;
    }

    public MetricsCounter(IEnumerable<string> blackList)
    {
        filename = UniLog.GenerateLogName(Engine.VersionNumber, "-trace").Replace(".log", ".json");
        this.blackList = blackList.ToHashSet();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(string name, Slot slot, long ticks, MetricType type)
    {
        Add(new Metric()
        {
            Slot = slot,
            Name = name,
            Ticks = ticks,
            Type = type
        });
    }

    public void Add(in Metric metric)
    {
        if (metric.Ticks == 0 || blackList.Contains(metric.Name) || ignoredHierarchy is not null && metric.Slot.IsChildOf(ignoredHierarchy, includeSelf: true)) return;

        TotalTicks += metric.Ticks;

        var id = metric.GetHashCode();
        if (Metrics.TryGetValue(id, out var prevValue))
        {
            Metrics[id] = prevValue + metric;
        }
        else
        {
            Metrics[id] = metric;
        }

        var ticks = Metrics[id].Ticks;
        if (ticks > MaxTicks)
        {
            MaxTicks = ticks;
        }
    }

    public void Flush()
    {
        var serializer = new JsonSerializer();
        var streamWriter = new StreamWriter(filename, append: true);
        serializer.Serialize(streamWriter, Metrics);
        streamWriter.Close();
    }

    public void Dispose()
    {
        Flush();
    }

    internal void UpdateBlacklist(IEnumerable<string> blackList)
    {
        this.blackList = blackList.ToHashSet();
        foreach (var metric in Metrics.Values)
        {
            if (this.blackList.Contains(metric.Name))
            {
                Metrics.Remove(metric.GetHashCode());
            }
        }
    }

    internal void Remove(in Metric metric)
    {
        Metrics.Remove(metric.GetHashCode());
    }

    internal void IgnoreHierarchy(Slot slot)
    {
        ignoredHierarchy = slot;
    }
}
