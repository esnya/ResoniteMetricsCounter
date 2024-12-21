using Elements.Core;
using FrooxEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ResoniteMetricsCounter.Utils;

internal static class WorldElementHelper
{
    private static readonly ThreadLocal<Dictionary<RefID, string>> nameCache = new(() => new());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetNameFast(this IWorldElement element)
    {
        var refID = element.ReferenceID;
        var cache = nameCache.Value;
        if (cache.TryGetValue(refID, out string name))
        {
            return name;
        }

        return cache[refID] = element.Name;
    }

    public static void Clear()
    {
        foreach (var cache in nameCache.Values)
        {
            cache.Clear();
        }
    }
}
