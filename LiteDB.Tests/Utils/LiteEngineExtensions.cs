using LiteDB.Engine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Tests
{
    public static class LiteEngineExtensions
    {
        public static int Insert(this LiteEngine engine, string collection, BsonDocument doc, BsonAutoId autoId = BsonAutoId.ObjectId)
        {
            return engine.Insert(collection, new BsonDocument[] { doc }, autoId);
        }
    }
}