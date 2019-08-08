---
title: 'Connection String'
date: 2019-02-11T19:30:08+10:00
draft: false
weight: 2
---

LiteDatabase can be initialize using a string connection, with `key1=value1; key2=value2; ...` syntax. If there is no `;` in your connection string, LiteDB assume that your connection string is Filename key. Keys are case insensitive.

### Options

- **`Filename`** (string): Full path or relative path from DLL directory.
- **`Journal`** (bool): Enabled or disable double write check to ensure durability (default: true)
- **`Password`** (string): Encrypt (using AES) your datafile with a password (default: null - no encryption)
- **`Cache Size`** (int): Max number of pages in cache. After this size, flush data to disk to avoid too memory usage (default: 5000)
- **`Timeout`** (TimeSpan): Timeout for waiting unlock operations (thread lock and locked file)
- **`Mode`** (Exclusive|ReadOnly|Shared): How datafile will be open (default: `Shared` in NET35/NET40/NETStandard2.0 and `Exclusive` in NETStandard1.3)
- **`Initial Size`** (string|long): If database is new, initialize with allocated space - support KB, MB, GB (default: null)
- **`Limit Size`** (string|long): Max limit of datafile - support KB, MB, GB (default: null)
- **`Upgrade`** (bool): If true, try upgrade datafile from old version (v2) (default: null)
- **`Log`** (byte): Debug messages from database - use `LiteDatabase.Log` (default: Logger.NONE)
- **`Async`** (bool): Support "sync over async" file stream creation to use in UWP access any disk folder (only for NetStandard, default: false)
- **`Flush`** (bool): Write data direct to disk avoiding OS cache (not available in NET35, default: false) (v4.1.2)

### Example

_App.config_
```XML
    <connectionStrings>
        <add name="LiteDB" connectionString="Filename=C:\database.db" />
    </connectionStrings>
```

_C#_
```C#
    System.Configuration.ConfigurationManager.ConnectionStrings["LiteDB"].ConnectionString
```