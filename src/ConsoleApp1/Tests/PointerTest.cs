//using LiteDB;
//using LiteDB.Engine;

//var filename = @$"C:\LiteDB\temp\v6\test-pointer-{DateTime.Now.Ticks}.db";

//var settings = new EngineSettings
//{
//Filename = filename,
//};

//var db = new LiteEngine(settings);

//await db.OpenAsync();

//await db.CreateCollectionAsync("col1");

//var data = Enumerable.Range(10, 100_000)
//    .Select(i => new BsonDocument() { ["_id"] = i, ["long_name"] = "dkhfsdkjhsdkjfhskfshsdkfhsdkjhdskjfhfksjdh" })
//    .ToArray();

//// 64 bytes doc
//await db.InsertAsync("col1", data, BsonAutoId.Int32);

//await db.ShutdownAsync();

//db.DumpState();
//Profiler.PrintResults();

//Console.ReadKey();

//return;

///*
//unsafe
//{
//    var s = sizeof(IndexKey);

//    // 8 bytes
//    var asNull = IndexKey.AllocNewIndexKey(BsonValue.Null);
//    var asMinValue = IndexKey.AllocNewIndexKey(BsonValue.MaxValue);
//    var asMaxValue = IndexKey.AllocNewIndexKey(BsonValue.MinValue);

//    var asBool_0 = IndexKey.AllocNewIndexKey(false);
//    var asBool_1 = IndexKey.AllocNewIndexKey(true);

//    var asInt32 = IndexKey.AllocNewIndexKey(7737);
//    var asInt64 = IndexKey.AllocNewIndexKey(7737L);

//    // 16 bytes
//    var asDouble = IndexKey.AllocNewIndexKey(7737d);
//    var asDecimal = IndexKey.AllocNewIndexKey(7737m);

//    var asDateTime = IndexKey.AllocNewIndexKey(DateTime.Now);

//    // 24 bytes
//    var asGuid = IndexKey.AllocNewIndexKey(Guid.NewGuid());
//    var asObjectId = IndexKey.AllocNewIndexKey(ObjectId.NewObjectId());

//    // 16 bytes
//    var asString_1 = IndexKey.AllocNewIndexKey("Less_8");

//    // 24 bytes
//    var asString_2 = IndexKey.AllocNewIndexKey("Between_8-16");

//    // 40 bytes
//    var asString_3 = IndexKey.AllocNewIndexKey("Larger_than_24_bytes_here");

//    // 64 bytes
//    var asBinary_1 = IndexKey.AllocNewIndexKey(new byte[50]);


//    Console.WriteLine(asString_1->ToString());





//}
//return;
//*/


//// ------------------------------------------------------------------------------------------------------
//// ------------------------------------------------------------------------------------------------------
//// ------------------------------------------------------------------------------------------------------


