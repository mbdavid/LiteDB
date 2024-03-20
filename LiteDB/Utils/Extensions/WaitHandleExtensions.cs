namespace LiteDB.Utils.Extensions
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class WaitHandleExtensions
    {
        public static async Task<bool> WaitAsync(this WaitHandle handle)
        {
            var tcs = new TaskCompletionSource<bool>();

            using (new ThreadPoolRegistration(handle, tcs))
            {
                return await tcs.Task.ConfigureAwait(false);
            }
        }

        private sealed class ThreadPoolRegistration : IDisposable
        {
            private readonly RegisteredWaitHandle _registeredWaitHandle;

            public ThreadPoolRegistration(WaitHandle handle, TaskCompletionSource<bool> tcs)
            {
                _registeredWaitHandle = ThreadPool.RegisterWaitForSingleObject(handle,
                    (state, timedOut) => ((TaskCompletionSource<bool>)state).TrySetResult(!timedOut), tcs,
                    Timeout.InfiniteTimeSpan, executeOnlyOnce: true);
            }

            void IDisposable.Dispose() => _registeredWaitHandle.Unregister(null);
        }
    }
}