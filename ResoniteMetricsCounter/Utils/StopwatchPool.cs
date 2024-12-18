using System.Collections.Generic;
using System.Diagnostics;

namespace ResoniteMetricsCounter.Utils;

internal sealed class StopwatchPool
{
    readonly LinkedList<Stopwatch> pool = new();
    internal Stopwatch GetAndStart()
    {
        if (pool.Count == 0)
        {
            return Stopwatch.StartNew();
        }
        var stopwatch = pool.Last.Value;
        pool.RemoveLast();
        stopwatch.Restart();
        return stopwatch;
    }

    internal long Release(Stopwatch stopwatch)
    {
        stopwatch.Stop();
        var result = stopwatch.ElapsedTicks;
        pool.AddLast(stopwatch);
        return result;
    }

}
