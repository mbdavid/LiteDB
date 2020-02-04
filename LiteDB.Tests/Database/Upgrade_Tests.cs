using System;
using System.IO;
using System.Linq;
using LiteDB;
using FluentAssertions;
using Xunit;

namespace LiteDB.Tests.Database
{
    public class Upgrade_Tests
    {
        [Fact]
        public void Migrage_From_V4()
        {
            // v5 upgrades only from v4!

            var original = "../../../Utils/Legacy/v4.db";
            var copy = original.Replace(".db", "-copy.db");
                
            File.Copy(original, copy, true);

            try
            {

                using(var db = new LiteDatabase($"filename={copy};upgrade=true"))
                {
                    // convert and open database
                    var col1 = db.GetCollection("col1");

                    col1.Count().Should().Be(3);
                }

                using (var db = new LiteDatabase($"filename={copy};upgrade=true"))
                {
                    // database already converted
                    var col1 = db.GetCollection("col1");

                    col1.Count().Should().Be(3);
                }
            }
            finally
            {
                File.Delete(copy);
                
                foreach(var backups in Directory.GetFiles(Path.GetDirectoryName(copy), "*-backup*.db"))
                {
                    File.Delete(backups);
                }
            }
        }

        [Fact]
        public void Migrage_From_V4_No_FileExtension()
        {
            // v5 upgrades only from v4!

            var original = "../../../Utils/Legacy/v4.db";
            var copy = original.Replace(".db", "-copy");

            File.Copy(original, copy, true);

            try
            {

                using (var db = new LiteDatabase($"filename={copy};upgrade=true"))
                {
                    // convert and open database
                    var col1 = db.GetCollection("col1");

                    col1.Count().Should().Be(3);
                }

                using (var db = new LiteDatabase($"filename={copy};upgrade=true"))
                {
                    // database already converted
                    var col1 = db.GetCollection("col1");

                    col1.Count().Should().Be(3);
                }
            }
            finally
            {
                File.Delete(copy);

                foreach (var backups in Directory.GetFiles(Path.GetDirectoryName(copy), "*-backup*"))
                {
                    File.Delete(backups);
                }
            }
        }
    }
}