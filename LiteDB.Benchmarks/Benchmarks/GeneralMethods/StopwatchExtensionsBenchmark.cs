using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

using LiteDB.Utils.Extensions;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LiteDB.Benchmarks.Benchmarks.GeneralMethods;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net472, baseline: true)]
[SimpleJob(RuntimeMoniker.Net60)]
[BenchmarkCategory(Constants.Categories.GENERAL)]
public class StopwatchExtensionsBenchmark
{
    private Stopwatch stopwatch = new Stopwatch();
    private Action a = () => { };

    [Benchmark]
    public StopWatchExtensions.DisposableAction ReturningNewDisposableAction()
    {
        return stopwatch.StartDisposable();
    }

    [Benchmark]
    public int Sum()
    {
        var watchDisposable = stopwatch.StartDisposable();
        int sum = 0;
        try
        {
            foreach (var i in Enumerable.Range(0, 100))
            {
                sum += i;
            }
        }
        finally
        {
            watchDisposable.Dispose();
        }
        return sum;
    }

    [Benchmark]
    public int Struct_SumWithRangeHavingSw()
    {
        int sum = 0;
        foreach (var i in Struct_Range(0, 100))
        {
            sum += i;
        }
        return sum;
    }

    private static IEnumerable<int> Struct_Range(int start, int count)
    {
        var sw = new Stopwatch();

        using var _ = sw.StartDisposable();
        for (int i = 0; i < count; i++)
        {
            sw.Stop();
            yield return start + i;
            sw.Start();
        }
    }

    [Benchmark]
    public int Class_SumWithRangeHavingSw()
    {
        int sum = 0;
        foreach (var i in Class_Range(0, 100))
        {
            sum += i;
        }
        return sum;
    }

    private static IEnumerable<int> Class_Range(int start, int count)
    {
        var sw = new Stopwatch();

        using var _ = StopWatchExtensionsOld.StartDisposableOld(sw);
        for (int i = 0; i < count; i++)
        {
            sw.Stop();
            yield return start + i;
            sw.Start();
        }
    }

    internal static class StopWatchExtensionsOld
    {
        /// <summary>
        /// Start the stopwatch and returns an IDisposable that will stop the stopwatch when disposed
        /// </summary>
        /// <param name="stopwatch"><see cref="Stopwatch"/> instance that will be used to measure time.</param>
        /// <returns></returns>
        public static DisposableAction StartDisposableOld(Stopwatch stopwatch)
        {
            stopwatch.Start();
            return new DisposableAction(stopwatch.Stop);
        }

        /// <summary>
        /// This struct isn't mean to be instantiated by users, so its ctor is internal.
        /// If you want to use an instance of it call <see cref="StartDisposable(Stopwatch)"/> method.
        /// </summary>
        public class DisposableAction : IDisposable
        {
            private readonly Action _action;

            internal DisposableAction(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                _action();
            }
        }
    }
}