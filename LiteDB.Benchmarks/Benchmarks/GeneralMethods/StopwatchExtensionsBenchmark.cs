using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using LiteDB.Utils.Extensions;
using System;
using System.Diagnostics;
using System.Linq;

namespace LiteDB.Benchmarks.Benchmarks.GeneralMethods
{
	[MemoryDiagnoser]
	[SimpleJob(RuntimeMoniker.Net472, baseline: true)]
	[SimpleJob(RuntimeMoniker.Net60)]
	public class StopwatchExtensionsBenchmark
	{
		Stopwatch stopwatch = new Stopwatch();
		Action a = () => { };

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
	}
}
