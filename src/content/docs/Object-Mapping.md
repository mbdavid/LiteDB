---
title: 'Object Mapping'
date: 2019-02-11T19:30:08+10:00
draft: false
weight: 1
---

LiteDB supports POCO classes to strongly type documents. When you get a `LiteCollection` instance from `LiteDatabase.GetCollection<T>`, `<T>` will be your document type. If `<T>` is not a `BsonDocument`, LiteDB internally maps your class to `BsonDocument`. To do this, LiteDB uses the `BsonMapper` class:

```C#
// Simple strongly-typed document
public class Customer
{
    public ObjectId CustomerId { get; set; }
    public string Name { get; set; }
    public DateTime CreateDate { get; set; }
    public List<Phone> Phones { get; set; }
    public bool IsActive { get; set; }
}

var typedCustomerCollection = db.GetCollection<Customer>("customer");

var schemelessCollection = db.GetCollection("customer"); // <T> is BsonDocument
```

### Mapper conventions

`BsonMapper.ToDocument()` auto converts each property of a class to a document field following these conventions:

- Classes must be _**public with a public parameterless constructor**_
- Properties must be public
- Properties can be read-only or read/write
- The class must have an `Id` property, `<ClassName>Id` property or any property with `[BsonId]` attribute or mapped by fluent api.
- A property can be decorated with `[BsonIgnore]` to not be mapped to a document field
- A property can be decorated with `[BsonField]` to customize the name of the document field
- No circular references are allowed
- Max depth of 20 inner classes
- Class fields are not converted to document
- You can use `BsonMapper` global instance (`BsonMapper.Global`) or a custom instance and pass to `LiteDatabase` in constructor. Keep this instance in a single place to avoid re-creating all mapping each time you use database.

In addition to basic BSON types, `BsonMapper` maps others .NET types to BSON data type:

|.NET type                          |BSON type     |
|-----------------------------------|--------------|
|`Int16`, `UInt16`, `Byte`, `SByte` |Int32         |
|`UInt32` , `UInt64`                |Int64         |
|`Single`                           |Double        |
|`Char`, `Enum`                     |String        |
|`IList<T>`                         |Array         |
|`T[]`                              |Array         |
|`NameValueCollection`              |Document      |
|`IDictionary<K,T>`                 |Document      |
|Any other .NET type                |Document      |

- `Nullable<T>` are accepted. If value is `null` the BSON type is Null, otherwise the mapper will use `T?`.
- For `IDictionary<K, T>`, `K` key must be `String` or simple type (convertible using `Convert.ToString(..)`). 

#### Register a custom type

You can register your own map function, using the `RegisterType<T>` instance method. To register, you need to provide both serialize and deserialize functions.

```C#
BsonMapper.Global.RegisterType<Uri>
(
    serialize: (uri) => uri.AbsoluteUri,
    deserialize: (bson) => new Uri(bson.AsString)
);
```

- `serialize` functions pass a `<T>` object instance as the input parameter and expect return a `BsonValue`
- `deserialize` function pass a `BsonValue` object as the input parameter and expect return a `<T>` value
- `RegisterType` supports complex objects via `BsonDocument` or `BsonArray` 

#### Mapping options

`BsonMapper` class settings:

|Name                   |Default |Description                                                |
|-----------------------|--------|-----------------------------------------------------------|
|`SerializeNullValues`  |false   |Serialize field if value is `null`                         |
|`TrimWhitespace`       |true    |Trim strings properties before mapping to document         |
|`EmptyStringToNull`    |true    |Empty strings convert to `null`                            |
|`ResolvePropertyName`  |(s) => s|A function to map property name to document field name     |

`BsonMapper` offers 2 predefined functions to resolve property name: `UseCamelCase()` and `UseLowerCaseDelimiter('_')`.

```C#
BsonMapper.Global.UseLowerCaseDelimiter('_');

public class Customer
{
    public int CustomerId { get; set; }

    public string FirstName { get; set; }

    [BsonField("customerLastName")]
    public string LastName { get; set; }
}

var doc = BsonMapper.Global.ToDocument(new Customer { FirstName = "John", LastName = "Doe" });

var id = doc["_id"].AsInt;
var john = doc["first_name"].AsString;
var doe = doc["customerLastName"].AsString;
```    

### AutoId

In v4, AutoId is moved to engine level (BsonDocument). There are 4 built-in auto-id implemented:

- `ObjectId`: `ObjectId.NewObjectId()`
- `Guid`: `Guid.NewGuid()` method
- `Int32/Int64`: New collection sequence
- `DateTime`: `DateTime.Now`

Auto Id are used only when there `_id` is missing when inserting. In strong-typed document, `BsonMapper` remove `_id` field from "empty" values (like `0` in `Int` or `Guid.Empty` in `Guid`)

### Fluent Mapping

LiteDB offers a complete fluent API to create custom mapping without using attributes, keeping you domain classes without external references.

Fluent API use `EntityBuilder` to add custom mappings to your classes.

```C#
var mapper = BsonMapper.Global;

mapper.Entity<MyEntity>()
    .Id(x => x.MyCustomKey) // set your document ID
    .Ignore(x => x.DoNotSerializeThis) // ignore this property (do not store)
    .Field(x => x.CustomerName, "cust_name"); // rename document field
```