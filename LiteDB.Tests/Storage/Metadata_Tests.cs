using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LiteDB.Tests.Storage
{
    [TestClass]
    public class Metadata_Tests
    {
        [TestMethod]
        public void Insert_Update_Storage_Metadata()
        {
            var source = new byte[25000];
            source[0] = 255;
            source[24999] = 127;
            source[10000] = 65;

            var ms = new MemoryStream(source);

            // testing issue #495
            using (var file = new TempFile())
            using (var db = new LiteDatabase(file.Filename))
            {
                var dict = new Dictionary<string, BsonValue>();
                dict.Add("extension", ".jpg");

                // upload file
                // FileId is = "1020d6eb-e5fb-4c94-8a21-c0ea2f3a4b59"
                db.FileStorage.Upload("1020d6eb-e5fb-4c94-8a21-c0ea2f3a4b59", "$/gallery/1020d6eb-e5fb-4c94-8a21-c0ea2f3a4b59", ms);

                db.FileStorage.SetMetadata("1020d6eb-e5fb-4c94-8a21-c0ea2f3a4b59", new BsonDocument(dict));

                // getting file inside _files collection
                var d = db.FileStorage.FindById("1020d6eb-e5fb-4c94-8a21-c0ea2f3a4b59");

                Assert.IsNotNull(d);
                Assert.AreEqual(d.Metadata["extension"].AsString, ".jpg");

                // now lets download file
                var output = new MemoryStream();
                db.FileStorage.Download("1020d6eb-e5fb-4c94-8a21-c0ea2f3a4b59", output);
                var dest = output.ToArray();

                Assert.AreEqual(source.Length, dest.Length);
                Assert.AreEqual(source[0], dest[0]);
                Assert.AreEqual(source[10000], dest[10000]);
                Assert.AreEqual(source[24999], dest[24999]);
            }
        }
    }
}