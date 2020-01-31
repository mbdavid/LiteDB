---
title: 'Connection String'
draft: false
weight: 7
---

LiteDatabase can be initialized using a string connection, with `key1=value1; key2=value2; ...` syntax. If there is no `=` in your connection string, LiteDB assume that your connection string contains only the `Filename`. Keys are case insensitive.

### Options

|Key|Type|Description|Default value|
|--------|----|-----------------|-------------|
|Filename|string|Full or relative path to the datafile|-|
|Connection|string|Connection type ("direct" or "shared")|"direct"|
|Password|string|Encrypt (using AES) your datafile with a password|null (no encryption)|
|InitialSize|string or long|Initial size for the datafile (string suppoorts "KB", "MB" and "GB")|0|
|ReadOnly|bool|Open datafile in read-only mode|false|
|Upgrade|bool|Check if datafile is of an older version and upgrade it before opening|false|

### Example

_App.config_
```XML
    <connectionStrings>
        <add name="LiteDB" connectionString="Filename=C:\database.db;Password='1234'" />
    </connectionStrings>
```

_C#_
```C#
    System.Configuration.ConfigurationManager.ConnectionStrings["LiteDB"].ConnectionString
```