using Elements.Core;
using FrooxEngine;
using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace ResoniteMetricsCounter.Utils;

internal abstract class CachedValueBase<T, K, V>
{
    private readonly ConcurrentDictionary<K, V> cache = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract K GetKey(in T source);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected abstract V GetValue(in T source);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public V GetOrCache(in T source)
    {
        var key = GetKey(source);
        if (cache.TryGetValue(key, out V res))
        {
            return res;
        }
        return cache[key] = GetValue(source);
    }
    public void Clear()
    {
        cache.Clear();
    }
}

internal abstract class FactoryCachedValueBase<T, K, V> : CachedValueBase<T, K, V>
{
    private readonly Func<T, V> valueFactory;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override V GetValue(in T source) => valueFactory(source);

    public FactoryCachedValueBase(Func<T, V> valueFactory)
    {
        this.valueFactory = valueFactory;
    }
}


//internal sealed class SimpleCachedValue<K, V> : FactoryCachedValueBase<K, K, V>
//{
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    protected override K GetKey(in K source) => source;

//    public SimpleCachedValue(Func<K, V> valueFactory) : base(valueFactory)
//    {
//    }
//}

//internal sealed class CachedValue<T, K, V> : FactoryCachedValueBase<T, K, V>
//{
//    private readonly Func<T, K> keySelector;
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]
//    protected override K GetKey(in T source) => keySelector(source);
//    [MethodImpl(MethodImplOptions.AggressiveInlining)]

//    public CachedValue(Func<T, K> keySelector, Func<T, V> valueFactory) : base(valueFactory)
//    {
//        this.keySelector = keySelector;
//    }
//}

internal sealed class CachedElementValue<T, U> : FactoryCachedValueBase<T, RefID, U> where T : IWorldElement
{

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override RefID GetKey(in T source) => source.ReferenceID;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]

    public CachedElementValue(Func<T, U> valueFactory) : base(valueFactory)
    {
    }
}

internal abstract class CachedElementValueBase<T, U> : CachedValueBase<T, RefID, U> where T : IWorldElement
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override RefID GetKey(in T source) => source.ReferenceID;
}
