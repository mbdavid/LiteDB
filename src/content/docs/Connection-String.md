---
title: 'Connection String'
draft: false
weight: 7
---

LiteDatabase can be initialized using a string connection, with `key1=value1; key2=value2; ...` syntax. If there is no `=` in your connection string, LiteDB assume that your connection string contains only the `Filename`. Keys are case insensitive. Values can be quoted (`"` or `'`) if contains special chars (like `;` or `=`).

### Options

|Key|Type|Description|Default value|
|--------|----|-----------------|-------------|
|Filename|string|Full or relative path to the datafile. Supports `:memory:` for memory database or `:temp:` for in disk temporary database (file will deleted when database was closed) **[required]**|- |
|Connection|string|Connection type ("direct" or "shared")|"direct"|
|Password|string|Encrypt (using AES) your datafile with a password|null (no encryption)|
|InitialSize|string or long|Initial size for the datafile (string suppoorts "KB", "MB" and "GB")|0|
|ReadOnly|bool|Open datafile in read-only mode|false|
|Upgrade|bool|Check if datafile is of an older version and upgrade it before opening|false|

#### Connection Type

LiteDB offers 2 types of connections: `Direct` and `Shared`. This affect how engine will open data file.

- `Direct`: Engine will open files in exclusive mode and will keep files open until `Dispose()`. There will is no second instance of this database. This is the recommended mode because it's faster and cachable.
- `Shared`: In this case, files will be open/close on each operation. Locks are made using `Mutex`. This is more expansive but you can open same file from many different processes.

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