using FrooxEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ResoniteMetricsCounter.Metrics;

internal enum MetricType
{
    Updates,
    PhysicsMoved,
    ProtoFluxContinuousChanges,
    ProtoFluxUpdates,
    Changes,
}


#pragma warning disable CA1812
internal sealed class SlotHierarchyConverter : JsonConverter<Slot>
{
    private readonly Dictionary<Slot, string> cache = new();

    public override Slot ReadJson(JsonReader reader, Type objectType, Slot? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, Slot? value, JsonSerializer serializer)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        if (cache.TryGetValue(value, out var cached))
        {
            writer.WriteValue(cached);
            return;
        }

        var stringBuilder = new StringBuilder();

        for (var s = value; s != null; s = s.Parent)
        {
            stringBuilder.Insert(0, s.Name);
            stringBuilder.Insert(0, "/");
        }

        writer.WriteValue(cache[value] = stringBuilder.ToString());
    }
}
#pragma warning restore CA1812

internal struct Metric
{
    [JsonConverter(typeof(SlotHierarchyConverter))]
    public Slot Slot;

    public string Name;

    [JsonConverter(typeof(StringEnumConverter))]
    public MetricType Type;

    public long Ticks;

    public static long Frequency => Stopwatch.Frequency;

    public static Metric operator +(Metric a, Metric b)
    {
        return new Metric
        {
            Slot = a.Slot,
            Name = a.Name,
            Type = a.Type,
            Ticks = a.Ticks + b.Ticks,
        };
    }

    public readonly string GetName()
    {
        var parent = Slot.Parent;
        var root = parent?.GetObjectRoot();

        if (parent is null)
        {
            return $"{Slot.Name}.{Name}[{Type}]";
        }
        else if (root is null || root == parent)
        {
            return $"{parent.Name}/{Slot.Name}.{Name}[{Type}]";
        }

        return $"{root.Name}/../{parent.Name}/{Slot.Name}.{Name}[{Type}]";
    }

    public override readonly int GetHashCode()
    {
        return Slot.ReferenceID.GetHashCode() ^ Name.GetHashCode() ^ Type.GetHashCode();
    }

    //public bool Equals(Metric other) {
    //	return Slot == other.Slot && Name == other.Name && Type == other.Type;
    //}

    //public int CompareTo(Metric other) {
    //	if (Equals(other)) return 0;
    //	return (int)(other.Ticks - Ticks);
    //}
}
