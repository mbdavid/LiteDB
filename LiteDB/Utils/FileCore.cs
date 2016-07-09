#if !PCL
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
#if NETCORE
using System.Threading.Tasks;
#endif

namespace LiteDB
{
    /// <summary>
    /// Internal class that use File operation compatible with .net core
    /// </summary>
    internal class FileCore
    {
        public static void Delete(string filename)
        {
#if NETFULL
            File.Delete(filename);
#elif NETCORE
            SyncOverAsync(() => File.Delete(filename));
#endif
        }

        public static bool Exists(string filename)
        {
#if NETFULL
            return File.Exists(filename);
#elif NETCORE
            return SyncOverAsync<bool>(() => File.Exists(filename));
#endif
        }

        public static FileStream FileStream(string filename, FileMode mode, FileAccess access, FileShare share)
        {
#if NETFULL
            return new FileStream(filename, mode, access, share, BasePage.PAGE_SIZE);
#elif NETCORE
            return SyncOverAsync<FileStream>(() => new FileStream(filename, mode, access, share, BasePage.PAGE_SIZE));
#endif
        }

#if NETCORE
        // These methods will run the specified Action on a background thread
        // so they will not cause issues with UI
        private static void SyncOverAsync(Action f)
        {
            Task.Run(f).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        private static T SyncOverAsync<T>(Func<T> f)
        {
            return Task.Run<T>(f).ConfigureAwait(false).GetAwaiter().GetResult();
        }
#endif
    }
}
#endif