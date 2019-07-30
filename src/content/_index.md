---
title: 'LiteDB - A .NET NoSQL Document Store in a single data file'
date: 2018-11-28T15:14:39+10:00
---

> LiteDB is serverless database delivered in a single DLL, fully written in .NET C# managed code and compatible with .NET Full 4.x, NETStandard 2.0, .NET Core 2 and Xamarin.

### Some features

- Serverless NoSQL Document Store
- Simple API similar to MongoDB
- 100% C# code for .NET 4.5 / NETStandard 2.0 in a single DLL (less than 300kb)
- Thread safe
- ACID with full transaction support
- Data recovery after write failure (WAL log file)
- Datafile encryption using DES (AES) cryptography
- Map your POCO classes to `BsonDocument` using attributes or fluent mapper API
- Store files and stream data (like GridFS in MongoDB)
- Single data file storage (like SQLite)
- Index document fields for fast search (up to 32 indexes per collection)
- LINQ support for queries
- SQL-Like commands to access/transform data
- LiteDB Studio - Nice UI for data access
- Pretty fast - [compare results with SQLite here](https://github.com/mbdavid/LiteDB-Perf)
- Open source and free for everyone - including commercial use
- Install from NuGet: `Install-Package LiteDB`