---
title: 'Connection String'
draft: false
weight: 7
---

LiteDatabase can be initialized using a string connection, with `key1=value1; key2=value2; ...` syntax. If there is no `=` in your connection string, LiteDB assume that your connection string contains only the `Filename`. Keys are case insensitive. Values can be quoted (`"` or `'`) if contains special chars (like `;` or `=`).

### Options

|Key|Type|Description|Default value|
|--------|----|-----------------|-------------|
|Filename|string|Full or relative path to the datafile. Supports `:memory:` for memory database or `:temp:` for in disk temporary database (file will deleted when database is closed) **[required]**|- |
|Connection|string|Connection type ("direct" or "shared")|"direct"|
|Password|string|Encrypt (using AES) your datafile with a password|null (no encryption)|
|InitialSize|string or long|Initial size for the datafile (string suppoorts "KB", "MB" and "GB")|0|
|ReadOnly|bool|Open datafile in read-only mode|false|
|Upgrade|bool|Check if datafile is of an older version and upgrade it before opening|false|

#### Connection Type

LiteDB offers 2 types of connections: `Direct` and `Shared`. This affect how engine will open data file.

- `Direct`: Engine will open the datafoçe in exclusive mode and will keep it open until `Dispose()`. The datafile cannot be opened by another process. This is the recommended mode because it's faster and cachable.
- `Shared`: Engine will be close the datafile after each operation. Locks are made using `Mutex`. This is more expensive but you can open same file from multiple processes.


### Example

_App.config_
```XML
<connectionStrings>
    <add name="LiteDB" connectionString="Filename=C:\database.db;Password=1234" />
</connectionStrings>
```

_C#_
```C#
System.Configuration.ConfigurationManager.ConnectionStrings["LiteDB"].ConnectionString
```