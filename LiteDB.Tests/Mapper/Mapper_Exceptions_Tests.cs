using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB.Tests.Mapper
{
    #region Model

    public class DataRecord
    {
        public Guid Id { get; set; }
        public string Value { get; set; }
    }

    #endregion

    [TestClass]
    public class Mapper_Exceptions_Tests
    {
        [TestMethod]
        public void Mapper_Exceptions()
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