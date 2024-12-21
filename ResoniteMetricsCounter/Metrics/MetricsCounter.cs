using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using ResoniteMetricsCounter.Serialization;
using ResoniteMetricsCounter.Utils;
using ResoniteModLoader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ResoniteMetricsCounter.Metrics;


internal sealed class MetricsCounter : IDisposable
{
    [JsonInclude] public Slot? IgnoredHierarchy { get; private set; }
    internal bool IsDisposed { get; private set; }


    [JsonInclude] public readonly string Filename;
    [JsonInclude] public readonly VersionNumber EngineVersion;
    [JsonInclude] public HashSet<string> BlackList;

    [JsonInclude] public readonly MetricsByStageStorage<IWorldElement> ByElement = new();
    [JsonInclude] public readonly MetricsStorage<Slot> ByObjectRoot = new();

    public MetricsCounter(IEnumerable<string> blackList)
    {
        EngineVersion = Engine.Version;
        Filename = UniLog.GenerateLogName(EngineVersion.ToString(), "-trace").Replace(".log", ".json");
        BlackList = blackList.ToHashSet();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddForCurrentStage(object obj, long ticks)
    {
        if (obj is IWorldElement element) AddForCurrentStage(element, ticks);
        else if (obj is ProtoFluxNodeGroup group) AddForCurrentStage(group, ticks);
        else if (ResoniteMod.IsDebugEnabled()) ResoniteMod.Debug($"Unknown object type: {obj.GetType()}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddForCurrentStage(ProtoFluxNodeGroup group, long ticks)
    {
        var world = group.World;
        if (world.Focus != World.WorldFocus.Focused) return;

        var node = group.Nodes.FirstOrDefault();
        if (node is null) return;

        AddForCurrentStage(node, ticks);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddForCurrentStage(IWorldElement element, long ticks)
    {
        var world = element.World;
        if (world.Focus != World.WorldFocus.Focused) return;

        if (element.IsLocalElement || element.IsRemoved || BlackList.Contains(element.GetNameFast())) return;

        var slot = (element is Slot ? element : element.Parent) as Slot;
        if (slot is not null && IgnoredHierarchy is not null && IgnoredHierarchy.IsChildOf(slot, includeSelf: true)) return;

        ByElement.Add(element, ticks);

        if (slot is null) return;
        var objectRoot = slot.GetObjectRoot(true) ?? world.RootSlot;
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
        using (var writer = new FileStream(Filename, FileMode.Create))
        {
            JsonSerializer.Serialize(writer, this, jsonSerializerOptions);
        }
        ResoniteMod.DebugFunc(() => $"Metrics written to {Filename}");
    }

    public void Dispose()
    {
        IsDisposed = true;
        Flush();
    }

    internal void UpdateBlacklist(IEnumerable<string> blackList)
    {
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
