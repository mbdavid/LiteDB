---
title: 'BsonDocument'
draft: false
weight: 5
---

The `BsonDocument` class is LiteDB's implementation of documents. Internally, a `BsonDocument` stores key-value pairs in a `Dictionary<string, BsonValue>`.

```C#
var customer = new BsonDocument();
customer["_id"] = ObjectId.NewObjectId();
customer["Name"] = "John Doe";
customer["CreateDate"] = DateTime.Now;
customer["Phones"] = new BsonArray { "8000-0000", "9000-000" };
customer["IsActive"] = true;
customer["IsAdmin"] = new BsonValue(true);
customer.Set("Address.Street", "Av. Protasio Alves, 1331");
```

About document field **keys**:

- Keys must contains only letters, numbers or `_` and `-`
- Keys are case-sensitive
- Duplicate keys are not allowed
- LiteDB keeps the original key order, including mapped classes. The only exception is for `_id` field that will always be the first field. 

About document field **values**:

- Values can be any BSON value data type: Null, Int32, Int64, Double, String, Embedded Document, Array, Binary, ObjectId, Guid, Boolean, DateTime, MinValue, MaxValue
- When a field is indexed, the value must be less than 512 bytes after BSON serialization.
- Non-indexed values themselves have no size limit, but the whole document is limited to 1Mb after BSON serialization. This size includes all extra bytes that are used by BSON.
- `_id` field cannot be: `Null`, `MinValue` or `MaxValue`
- `_id` is unique indexed field, so value must be less than 512 bytes

About .NET classes

- `BsonValue` 
    - This class can hold any BSON data type, including null, array or document.
    - Has implicit constructor to all supported .NET data types
    - Value never changes
    - `RawValue` property that returns internal .NET object instance
- `BsonArray` 
    - Supports `IEnumerable<BsonValue>`
    - Each array item can have different BSON type objects
- `BsonDocument`
    - Missing fields always return `BsonValue.Null` value
    - `Set` and `Get` methods can be used with dotted notation to access/create inner documents

```C#
// Testing BSON value data type
if(customer["Name"].IsString) { ... }

// Helper to get .NET type
string str = customer["Name"].AsString;
```

To use other .NET data types you need a custom `BsonMapper` class.