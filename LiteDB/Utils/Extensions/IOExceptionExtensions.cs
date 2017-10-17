using System.IO;
using System.Runtime.InteropServices;

namespace LiteDB
{
    internal static class IOExceptionExtensions
    {
        private const int ERROR_SHARING_VIOLATION = 32;
        private const int ERROR_LOCK_VIOLATION = 33;

        /// <summary>
        /// Detect if exception is an Locked exception
        /// </summary>
        public static bool IsLocked(this IOException ex)
        {
            var errorCode = Marshal.GetHRForException(ex) & ((1 << 16) - 1);

            return errorCode == ERROR_SHARING_VIOLATION ||
                errorCode == ERROR_LOCK_VIOLATION;
        }

        public static void WaitIfLocked(this IOException ex, int timer)
        {
            if (ex.IsLocked())
            {
                if (timer > 0)
                {
                    WaitFor(timer);
                }
            }
            else
            {
                throw ex;
            }
        }

        /// <summary>
        /// WaitFor function used in all platforms
        /// </summary>
        public static void WaitFor(int ms)
        {
            // http://stackoverflow.com/questions/12641223/thread-sleep-replacement-in-net-for-windows-store
#if HAVE_TASK_DELAY
            System.Threading.Tasks.Task.Delay(ms).Wait();
#else
            System.Threading.Thread.Sleep(ms);
#endif
        }
    }
}