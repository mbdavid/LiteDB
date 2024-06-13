using System.IO;

namespace LiteDB
{
    using System.Threading.Tasks;

    internal static class StreamExtensions
    {
        /// <summary>
        /// If Stream are FileStream, flush content direct to disk (avoid OS cache)
        /// </summary>
        public static void FlushToDisk(this Stream stream)
        {
            if (stream is FileStream fstream)
            {
                fstream.Flush(true);
            }
            else
            {
                stream.Flush();
            }
        }

        public static async Task FlushToDiskAsync(this Stream stream)
        {
            // We don't need to cancel flush operation here.
            // From FileStream sources:
            // ================================================================================================
            // Unlike Flush(), FlushAsync() always flushes to disk. This is intentional.
            // Legend is that we chose not to flush the OS file buffers in Flush() in fear of
            // perf problems with frequent, long running FlushFileBuffers() calls. But we don't
            // have that problem with FlushAsync() because we will call FlushFileBuffers() in the background.
            // ================================================================================================
            // Hence no casting and bool flag here.
            await stream.FlushAsync().ConfigureAwait(false);
        }
    }
}