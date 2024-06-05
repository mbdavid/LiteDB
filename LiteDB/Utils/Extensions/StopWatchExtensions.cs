using System;
using System.Diagnostics;

namespace LiteDB.Utils.Extensions
{
	public static class StopWatchExtensions
	{
		/// <summary>
		/// Start the stopwatch and returns an IDisposable that will stop the stopwatch when disposed
		/// </summary>
		/// <param name="stopwatch"><see cref="Stopwatch"/> instance that will be used to measure time.</param>
		/// <returns></returns>
		public static DisposableAction StartDisposable(this Stopwatch stopwatch)
		{
			stopwatch.Start();
			return new DisposableAction(stopwatch.Stop);
		}

		/// <summary>
		/// This struct isn't mean to be instantiated by users, so its ctor is internal.
		/// If you want to use an instance of it call <see cref="StartDisposable(Stopwatch)"/> method.
		/// </summary>
		public readonly struct DisposableAction : IDisposable
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