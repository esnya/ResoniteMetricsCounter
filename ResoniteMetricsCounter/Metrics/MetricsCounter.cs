using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using ResoniteMetricsCounter.Serialization;
using ResoniteMetricsCounter.Utils;
using ResoniteModLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ResoniteMetricsCounter.Metrics;


public sealed class MetricsCounter : IDisposable
{
    private readonly CachedElementValue<IWorldElement, bool> shouldSkip;

    [JsonInclude] public Slot? IgnoredHierarchy { get; private set; }
    internal bool IsDisposed { get; private set; }

    [JsonInclude] public string Filename { get; private set; }
    [JsonInclude] public VersionNumber EngineVersion { get; private set; }
    [JsonInclude] public HashSet<string> BlackList { get; private set; }

    [JsonInclude] public MetricsByStageStorage<IWorldElement> ByElement { get; private set; } = new();
    [JsonInclude] public MetricsStorage<Slot> ByObjectRoot { get; private set; } = new();

    private readonly Stopwatch stopwatch = new();

    [JsonInclude] public long ElapsedMilliseconds => stopwatch.ElapsedMilliseconds;
    public long ElapsedTicks => stopwatch.ElapsedTicks;

    public MetricsCounter(IEnumerable<string> blackList)
    {
        shouldSkip = new(ShouldSkipImpl);

        EngineVersion = Engine.Version;
        Filename = UniLog.GenerateLogName(EngineVersion.ToString(), "-trace").Replace(".log", ".json");
        BlackList = blackList.ToHashSet();

        stopwatch.Start();
    }

    private bool ShouldSkipImpl(IWorldElement element)
    {
        var world = element.World;
        if (world.Focus != World.WorldFocus.Focused || !ResoniteMetricsCounterMod.CollectStage(world.Stage))
        {
            return true;
        }

        if (element.IsLocalElement || element.IsRemoved || BlackList.Contains(element.GetNameFast()))
        {
            return true;
        }

        var slot = element.GetSlotFast();
        if (slot is null || IgnoredHierarchy is null)
        {
            return false;
        }

        return IgnoredHierarchy.IsChildOf(slot, includeSelf: true);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddForCurrentStage(object? obj, long ticks)
    {
        if (obj is IWorldElement element)
        {
            AddForCurrentStage(element, ticks);
        }
        else if (obj is ProtoFluxNodeGroup group)
        {
            AddForCurrentStage(group, ticks);
        }
        else if (ResoniteMod.IsDebugEnabled())
        {
            ResoniteMod.Debug($"Unknown object type: {obj?.GetType()}");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddForCurrentStage(ProtoFluxNodeGroup group, long ticks)
    {
        var world = group.World;
        if (world.Focus != World.WorldFocus.Focused)
        {
            return;
        }

        var node = group.Nodes.FirstOrDefault();
        if (node is null)
        {
            return;
        }

        AddForCurrentStage(node, ticks);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddForCurrentStage(IWorldElement element, long ticks)
    {
        if (shouldSkip.GetOrCache(element))
        {
            return;
        }

        ByElement.Add(element, ticks);

        var objectRoot = element.GetMetricObjectRoot();
        if (objectRoot is null)
        {
            return;
        }

        ByObjectRoot.Add(objectRoot, ticks);
    }

    private static readonly JsonSerializerOptions jsonSerializerOptions = new()
    {
        WriteIndented = true,
        IgnoreReadOnlyFields = false,
        IgnoreReadOnlyProperties = false,
        Converters = { new IWorldElementConverter(), new JsonStringEnumConverter<World.RefreshStage>() },
    };

    public void Flush()
    {
        ResoniteMod.DebugFunc(() => $"Writing metrics to {Filename}");
        using var writer = new FileStream(Filename, FileMode.Create);
        JsonSerializer.Serialize(writer, this, jsonSerializerOptions);
    }

    public void Dispose()
    {
        IsDisposed = true;
        stopwatch.Stop();
        Flush();
    }

    internal void UpdateBlacklist(IEnumerable<string> blackList)
    {
        shouldSkip.Clear();
        BlackList = blackList.ToHashSet();
        ByElement.RemoveWhere(m => BlackList.Contains(m.Target.GetNameFast()));
        ByObjectRoot.RemoveWhere(m => BlackList.Contains(m.Target.GetNameFast()));
    }

    internal void Remove(Slot slot)
    {
        ByObjectRoot.Remove(slot);
    }

    internal void IgnoreHierarchy(Slot slot)
    {
        IgnoredHierarchy = slot;
    }
}
