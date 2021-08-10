---
title: 'Interfaces and Classes'
draft: false
weight: 7
---

## LiteDatabase
###### Constuctors
|Signature|Description|
|----|----|
|`LiteDatabase(string connectionString, BsonMapper mapper = null)`|Creates a new instance of `LiteDatabase` based on the provided connection string and mapper.|
|`LiteDatabase(ConnectionString connectionString, BsonMapper mapper = null)`|Creates a new instance of `LiteDatabase` based on the provided parsed connection string and mapper.|
|`LiteDatabase(Stream stream, BsonMapper mapper = null, Stream logStream = null)`|Creates a new instance of `LiteDatabase` based on the provided data and log streams and mapper.|
|`LiteDatabase(ILiteEngine engine, BsonMapper mapper = null, bool disposeOnClose = true)`|Creates a new instance of `LiteDatabase` based on a pre-existing `ILiteEngine` and a mapper. If `disposeOnClose` is true, `engine` will also be disposed when `this` is disposed.|


###### Fields and Properties
|Signature|Description|
|----|----|
|`BsonMapper Mapper { get; }`|Gets the `BsonMapper` being used by this instance. Read-only (use the `mapper` parameter in the constructors).|
|`ILiteStorage<string> FileStorage { get; }`|Gets an instance of `ILiteStorage<string>` that uses `_chunk` and `_files` collections. For custom names, use `GetStorage` method.|
|`int UserVersion { get; set; }`|Gets or sets the `UserVersion` pragma for the current datafile.|
|`TimeSpan Timeout { get; set; }`|Gets or sets the `Timeout` pragma for the current datafile.|
|`bool UtcDate { get; set; }`|Gets or sets the `UtcDate` pragma for the current datafile.|
|`long LimitSize { get; set; }`|Gets or sets the `LimitSize` pragma for the current datafile.|
|`int CheckpointSize { get; set; }`|Gets or sets the `Checkpoint` pragma for the current datafile.|
|`Collation Collation { get; }`|Gets the `Timeout` pragma for the current datafile (can only be changed with a rebuild).|


###### Methods


