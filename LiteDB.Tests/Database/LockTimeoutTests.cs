using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Tests.Database
{
    [TestClass]
    public class LockTimeoutTests
    {
        [TestMethod]
        public void ThrowsExceptionOnLockTimeout()
        {
            using (var tmp = new TempFile())
            {
                using (var db = new LiteDatabase($"filename={tmp.Filename};timeout=00:00:01"))
                {
                    var transactionStarted = new AutoResetEvent(false);
                    var transactionBlock = new AutoResetEvent(false);
                    var blockTask = Task.Run(() =>
                    {
                        using (db.BeginTrans())
                        {
                            transactionStarted.Set();
                            transactionBlock.WaitOne(TimeSpan.FromSeconds(10));
                        }
                    });

                    transactionStarted.WaitOne(TimeSpan.FromSeconds(10));
                    LiteException lockException = null;
                    try
                    {
                        using (db.BeginTrans())
                        {
                        }
                    }
                    catch (LiteException e)
                    {
                        lockException = e;
                        transactionBlock.Set();
                    }
                    blockTask.Wait();
                    Assert.IsNotNull(lockException);
                    Assert.AreEqual(LiteException.LOCK_TIMEOUT, lockException.ErrorCode);
                }
            }
        }
    }
}