using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace LiteDB.Tests.Engine
{
    public class Transactions_Tests
    {
        [Fact]
        public void Transaction_Write_Lock_Timeout()
        {
            var data1 = DataGen.Person(1, 100).ToArray();
            var data2 = DataGen.Person(101, 200).ToArray();

            using (var db = new LiteDatabase("filename=:memory:;timeout=1"))
            {
                var person = db.GetCollection<Person>();

                // init person collection with 100 document
                person.Insert(data1);

                // task A will open transaction and will insert +100 documents 
                // but will commit only 2s later
                var ta = new Task(() =>
                {
                    db.BeginTrans();

                    person.Insert(data2);

                    Task.Delay(4000).Wait();

                    var count = person.Count();

                    count.Should().Be(data1.Length + data2.Length);

                    db.Commit();
                });

                // task B will try delete all documents but will be locked during 1 second
                var tb = new Task(() =>
                {
                    Task.Delay(250).Wait();

                    db.BeginTrans();
                    person
                        .Invoking(personCol => personCol.DeleteMany("1 = 1"))
                        .Should()
                        .Throw<LiteException>()
                        .Where(ex => ex.ErrorCode == LiteException.LOCK_TIMEOUT);
                });

                ta.Start();
                tb.Start();

                Task.WaitAll(ta, tb);
            }
        }


        [Fact]
        public void Transaction_Avoid_Drity_Read()
        {
            var data1 = DataGen.Person(1, 100).ToArray();
            var data2 = DataGen.Person(101, 200).ToArray();

            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var person = db.GetCollection<Person>();

                // init person collection with 100 document
                person.Insert(data1);

                // task A will open transaction and will insert +100 documents 
                // but will commit only 1s later - this plus +100 document must be visible only inside task A
                var ta = new Task(() =>
                {
                    db.BeginTrans();

                    person.Insert(data2);

                    Task.Delay(1000).Wait();

                    var count = person.Count();

                    count.Should().Be(data1.Length + data2.Length);

                    db.Commit();
                });

                // task B will not open transaction and will wait 250ms before and count collection - 
                // at this time, task A already insert +100 document but here I cann't see (are not commited yet)
                // after task A finish, I can see now all 200 documents
                var tb = new Task(() =>
                {
                    Task.Delay(250).Wait();

                    var count = person.Count();

                    // read 100 documents
                    count.Should().Be(data1.Length);

                    ta.Wait();

                    // read 200 documets
                    count = person.Count();

                    count.Should().Be(data1.Length + data2.Length);
                });

                ta.Start();
                tb.Start();

                Task.WaitAll(ta, tb);
            }
        }

        [Fact]
        public void Transaction_Read_Version()
        {
            var data1 = DataGen.Person(1, 100).ToArray();
            var data2 = DataGen.Person(101, 200).ToArray();

            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var person = db.GetCollection<Person>();

                // init person collection with 100 document
                person.Insert(data1);

                // task A will insert more 100 documents but will commit only 1s later
                var ta = new Task(() =>
                {
                    db.BeginTrans();

                    person.Insert(data2);

                    Task.Delay(1000).Wait();

                    db.Commit();
                });

                // task B will open transaction too and will count 100 original documents only
                // but now, will wait task A finish - but is in transaction and must see only initial version
                var tb = new Task(() =>
                {
                    db.BeginTrans();

                    Task.Delay(250).Wait();

                    var count = person.Count();

                    // read 100 documents
                    count.Should().Be(data1.Length);

                    ta.Wait();

                    // keep reading 100 documets because i'm still in same transaction
                    count = person.Count();

                    count.Should().Be(data1.Length);
                });

                ta.Start();
                tb.Start();

                Task.WaitAll(ta, tb);
            }
        }

        [Fact]
        public void Test_Transaction_States()
        {
            var data0 = DataGen.Person(1, 10).ToArray();
            var data1 = DataGen.Person(11, 20).ToArray();

            using (var db = new LiteDatabase(new MemoryStream()))
            {
                var person = db.GetCollection<Person>();

                // first time transaction will be opened
                db.BeginTrans().Should().BeTrue();

                // but in second type transaction will be same
                db.BeginTrans().Should().BeFalse();

                person.Insert(data0);

                // must commit transaction
                db.Commit().Should().BeTrue();

                // no transaction to commit
                db.Commit().Should().BeFalse();

                // no transaction to rollback;
                db.Rollback().Should().BeFalse();

                db.BeginTrans().Should().BeTrue();

                // no page was changed but ok, let's rollback anyway
                db.Rollback().Should().BeTrue();

                // auto-commit
                person.Insert(data1);

                person.Count().Should().Be(20);
            }
        }
    }
}