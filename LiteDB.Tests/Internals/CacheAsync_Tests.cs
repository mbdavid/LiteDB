using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using LiteDB.Engine;
using System.Threading;
using System.Diagnostics;

namespace LiteDB.Internals
{
    [TestClass]
    public class CacheAsync_Tests
    {
        [TestMethod]
        public void CacheAsync_Thread_ShareCounter()
        {
            // Set()   - Seta true - Se estiver bloqueado, vai liberar
            // Reset() - Seta false - Quando chegar no proximo Wait() vai aguardar
            // Wait()  - Trava a thread SE estiver false (Reset) - Passa reto se estiver true (Set)
            var wa = new ManualResetEventSlim(true); 
            var wb = new ManualResetEventSlim(false);

            // serialize 2 threads
            void serialize(ManualResetEventSlim toBlock, ManualResetEventSlim toFree) 
            {
                toBlock?.Reset();
                toFree.Set();
                toBlock?.Wait();
            };

            var disk = new DiskService(new EngineSettings { DataStream = new MemoryStream(), MemorySegmentSize = 10 });

            var ta = new Task(() =>
            {
                var r = disk.GetReader();
                wa.Wait();

                // test starts here!!!
                var p0 = new HeaderPage(r.NewPage(), 0);

                p0.UserVersion = 25;

                disk.WriteAsync(new PageBuffer[] { p0.UpdateBuffer() });

                // (1 ->) jump to thread B
                serialize(wa, wb);
                // (2 <-) continue from thread B

                disk.Queue.Wait();

                // (3 ->) jump to thread B
                serialize(wa, wb);

            });

            var tb = new Task(() =>
            {
                var r = disk.GetReader();
                wb.Wait();

                // (1 <-) continue from thread A
                var p0 = r.ReadPage(0, false, FileOrigin.Log);

                // share counter can be 2 or 3
                // - if 2, page was not persisted yet on disk (async)
                // - if 1, page already persisted on disk
                var share = p0.ShareCounter;

                Assert.IsTrue(share >= 1 && share <= 2);

                // (2 ->) jump to thread A
                serialize(wb, wa);
                // (3 <-) continue from thread B

                // but now, I'm sure this page was saved and thread A release
                Assert.AreEqual(1, p0.ShareCounter);

                // let's release my page
                p0.Release();

                Assert.AreEqual(0, p0.ShareCounter);

                // release thread A
                serialize(null, wa);

            });

            ta.Start();
            tb.Start();

            Task.WaitAll(ta, tb);
        }
    }
}