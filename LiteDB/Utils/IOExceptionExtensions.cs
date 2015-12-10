using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace LiteDB
{
    internal static class IOExceptionExtensions
    {
        private const int ERROR_SHARING_VIOLATION = 32;
        private const int ERROR_LOCK_VIOLATION = 33;

        public static void WaitIfLocked(this IOException ex, int timer)
        {
            var errorCode = Marshal.GetHRForException(ex) & ((1 << 16) - 1);
            if (errorCode == ERROR_SHARING_VIOLATION || errorCode == ERROR_LOCK_VIOLATION)
            {
                if (timer > 0)
                {
                    Thread.Sleep(timer);
                }
            }
            else
            {
                throw ex;
            }
        }
    }
}