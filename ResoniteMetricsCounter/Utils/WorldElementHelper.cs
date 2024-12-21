using FrooxEngine;
using System.Runtime.CompilerServices;

namespace ResoniteMetricsCounter.Utils;



internal static class WorldElementHelper
{
    private sealed class CachedElementName : CachedElementValueBase<IWorldElement, string>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override string GetValue(in IWorldElement source) => source.Name;
    }

    private sealed class CachedElementSlot : CachedElementValueBase<IWorldElement, Slot?>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override Slot? GetValue(in IWorldElement source)
        {
            if (source is Slot slot) return slot;
            return source.Parent as Slot;
        }
    }

    private sealed class ExactObjectRootOrWorldRoot : CachedElementValueBase<IWorldElement, Slot?>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected override Slot? GetValue(in IWorldElement source)
        {
            if (source is not Slot slot)
            {
                return null;
            }

            return slot.GetObjectRoot(true) ?? slot.World.RootSlot;

        }
    }


    private static readonly CachedElementName nameCache = new();
    private static readonly CachedElementSlot slotCache = new();
    private static readonly ExactObjectRootOrWorldRoot exactObjectRootOrWorldRootCache = new();


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string GetNameFast(this IWorldElement element)
    {
        return nameCache.GetOrCache(element);
    }

    public static Slot? GetSlotFast(this IWorldElement element)
    {
        return slotCache.GetOrCache(element);
    }

    public static Slot? GetExactObjectRootOrWorldRootFast(this IWorldElement element)
    {
        return exactObjectRootOrWorldRootCache.GetOrCache(element);
    }

    public static void Clear()
    {
        nameCache.Clear();
        slotCache.Clear();
        exactObjectRootOrWorldRootCache.Clear();
    }
}
