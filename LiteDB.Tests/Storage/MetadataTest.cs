using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace LiteDB.Tests
{
    [TestClass]
    public class MetadataTest
    {
        [TestMethod]
        public void Metadata_Test()
        {
            var ms = new MemoryStream(new byte[25000]);

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

            }
        }
    }
}