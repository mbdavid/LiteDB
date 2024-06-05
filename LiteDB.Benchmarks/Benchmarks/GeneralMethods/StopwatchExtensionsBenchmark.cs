using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using LiteDB.Utils.Extensions;
using System;
using System.Diagnostics;

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


		//[Benchmark]
		//public IDisposable ReturningasIDisposableAction()
		//{
		//	return stopwatch.StartDisposable();
		//}

		//[Benchmark]
		//public async Task<int> ConsumingTheDisposable()
		//{
		//	using var test = stopwatch.StartDisposable();
		//	await Task.Yield();

		//	return 42;
		//}
	}
}
