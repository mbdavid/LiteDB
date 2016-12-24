using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB.Tests
{
    public class DataRecord
    {
        public Guid Id { get; set; }
        public string Value { get; set; }
    }

    [TestClass]
    public class MapperExceptionTest
    {
        [TestMethod]
        public void MapperException_Test()
        {
            var dataRecord = new DataRecord();
            var serialized = BsonMapper.Global.ToDocument(dataRecord);
            serialized.Add("_type", dataRecord.GetType().FullName);

            try
            {
                BsonMapper.Global.ToObject<DataRecord>(serialized);

                Assert.Fail("Throw error");
            }
            catch(LiteException ex)
            {
                if(ex.ErrorCode != 207)
                {
                    Assert.Fail("Error must be 207");
                }
            }
        }
    }
}