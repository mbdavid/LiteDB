# Data Modeling

LiteDB is a document database store. Documents are schemaless data strucutre storage in BSON data format (BSON are Binary JSON).

Documents are storage in collections. Each collection can handle any document structure. However, it's a good pratice that all documents in same collection has a similar data structure.

See below a simple comparation sheet between document database store and SQL database.

|LiteDB Document Store |SQL Database|
|----------------------|------------|
|Database              |Database    |
|Collection            |Table       |
|Document              |Row         |
|Document field        |Table Field |
|`_id` field           |Primary Key |
|Index                 |Table Index |


## Document Structure

Documents in LiteDB are multiples key-value pairs. See below a JSON document

```JS
{
    _id: 1,
    name: { first: "John", last: "Doe" },
    age: 37,
    salary: 3000.0,
    createdDate: { $date: "2014-10-30T00:00:00.00Z" },
    phones: ["8000-0000", "9000-0000"]
}
```

The above fields have the following data types:

- `_id` contains document primary key - an unique value in collection
- `name` containts an embedded document with `first` and `last` fields
- `age` contains a `Int32` value
- `salary` contains a `Double` value
- `createDate` contains a `DateTime` value
- `phones` contains an array of `String`

Document key string must be:

- Starts with a letter, a number or `_`
- Contains only letters, numbers, `_` and `-`
- Keys are case-sensitive
- Duplicate keys are not allowed

Document limits:

- Documents are limited in 1Mb size after BSON serialization. This size includes all extra bytes that are used on BSON
- When a field will be indexed, the value must be less then 512 bytes after BSON serialize.

Document fields:

- LiteDB keeps original field order, including POCO classes. The only exception is for `_id` field that will be always first field. If `_id` field are not in first order, LiteDB will force to first place. 
- `_id` field can be any BSON data type, except: `Null`, `MinValue` or `MaxValue`
- `_id` is unique indexed field, so value must be less then 512 bytes
 
## BsonDocument

`BsonDocument` class are LiteDB implementation for documents.

```C#
var customer = new BsonDocument();
customer["_id"] = ObjectId.NewObjectId();
customer["Name"] = "John Doe";
customer["CreateDate"] = DateTime.Now;
customer["Phones"] = new BsonArray { "8000-0000", "9000-000" };
customer["IsActive"] = true;
customer["IsAdmin"] = new BsonValue(true);
```

- `BsonValue` class are base for all BSON values and hold all BSON supported data types
- `BsonValue` has implicit constructor to all supported .NET data types
- `BsonDocument` class are used to hold only documents
- `BsonArray` class are used to hold only arrays. Array items can be any BSON value
- `BsonValue` has `RawValue` property that returns internal .NET object instance.

```C#
// Testing BSON value data type
if(customer["Name"].IsString) { ... }

// Helper to get .NET type
string str = customer["Name"].AsString;

// Casting RawValue to .NET type
string str = (string)customer["Name"].RawValue;
```

LiteDB use a subset of BSON data types. See all supported LiteDB BSON data types and .NET equivalent.

|BSON Type |.NET type                                                   |
|----------|------------------------------------------------------------|
|MinValue  |-                                                           |
|Null      |Any .NET object with `null` value                           |
|Int32     |`System.Int32`                                              |
|Int64     |`System.Int64`                                              |
|Double    |`System.Double`                                             |
|String    |`System.String`                                             |
|Document  |`System.Collection.Generic.Dictionary<string, BsonValue>`   |
|Array     |`System.Collection.Generic.List<BsonValue>`                 |
|Binary    |`System.Byte[]`                                             |
|ObjectId  |`LiteDB.ObjectId`                                           |
|Guid      |`System.Guid`                                               |
|Boolean   |`System.Boolean`                                            |
|DateTime  |`System.DateTime`                                           |
|MaxValue  |-                                                           |

To use .NET data types you need `BsonMapper` class.

## Using POCO classes

LiteDB supports POCO class as strong types documents. When you get `LiteCollection` instance from `LiteDatabase.GetCollection<T>` you can set `<T>` type. This type will be your document type. If this `<T>` was difernt thant `BsonDocument`, LiteDB internal maps you class to `BsonDocument`. To do this, LiteDB uses **`BsonMapper`**:

```C#
// Simple strong type document
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

`BsonMapper` convert each property class in a document field folling this rules:

- Classes must be pulic with a public one non-paramter constructor
- Properties must be public
- Properties can be read-only or read/write
- Class must have an `Id` property, `<ClassName>Id` property or any property with `[BsonId]` attribute
- A property can be decorated with `[BsonIgnore]` to not be mapped to document
- A property can be decorared with `[BsonField]` to have a custom name field
- No circular references allowed
- Max of 20 deep inner classes
- Class fields are not converted to document
- `BsonMapper` use a global instance that cache mapping information for a better performance.

Basic .NET data type mapper:

|.NET Data Type            |BSON Data Type|
|--------------------------|--------------|
|`Int16`, `UInt16`, `Byte` |Int32         |
|`UInt32` , `UInt64`       |Int64         |
|`Single`, `Decimal`       |Double        |
|`Char`, `Enum`            |String        |
|`IList<T>`                |Array         |
|`IDictionary<K,T>`        |Document      |

- `Nullable<T>` values are accepted. If value is `null`, BSON data type is Null, otherwise, BSON value will be .NET then underlying type.
- For `IDictionary<K, T>`, `K` key must be `String` or simple data type (convertable using `Convert.ToString(..)`). 

`BsonMapper` support some options

|Name                   |Default |Description                                                |
|-----------------------|--------|-----------------------------------------------------------|
|`SerializeNullValues`  |false   |If class property is `null` do not map in document result  |
|`TrimWhitespace`       |true    |Trim string properties before map to document              |
|`EmptyStringToNull`    |true    |Empty string conver to `null`                              |
|`ResolvePropertyName`  |(s) => s|A function to map property name to document field name     |

`BsonMapper` offers 2 predefined functions to resolve property name: `UseCamelCase()` and `UseLowerCaseDelimiter("_")`.

```C#
BsonMapper.Global.UseLowerCaseDelimiter("_");

public class Customer
{
    public string FirstName { get; set; }
}

var doc = BsonMapper.Global.ToDocument(new Customer { FirstName = "John" });

var john = doc["first_name"];
```    
    
- You can register your own map function, using `RegisterType<T>` instance method.

```C#
BsonMapper.Global.RegisterType<Uri>
(
    serialize: (uri) => uri.AbsoluteUri,
    deserialize: (bson) => new Uri(bson.AsString)
);
```

### AutoId

`BsonMapper` supports auto setting `_id` value on `Insert` method in `LiteCollection`. To use this, you must define your id property with `[BsonId(true)]`. LiteDB has a predefined functions for some datatypes:

|.NET data type  |New Value                   |
|----------------|----------------------------|
|`ObjectId`      |`ObjectId.NewObjectId()`    |
|`Guid`          |`Guid.NewGuid()`            |
|`Int32`         |Sequence value stating in 1 |

To register a new type auto-id, use `RegisterAutoId<T>`

```C#
BsonMapper.Global.RegisterAutoId<Int64>
(
    isEmpty: (v) => v == 0,
    newId: (c) => DateTime.UtcNow.Ticks
);

public class Calendar
{
    [BsonId(true)]
    public long CalendarId { get; set; }
}
```

### Index definition
    
`BsonMapper` support index definition direct on property using `[BsonIndex]` attribute. You can define index options like, `Unique` or `IgnoreCase`. This is useful to avoid always run `col.EnsureIndex("field")` before run a query.

```C#
public class Customer
{
    public int Id { get; set; }
    
    [BsonIndex]
    public string Name { get; set; }
    
    [BsonIndex(new IndexOptions { Unique = true, IgnoreCase = true })]
    public string Email { get; set; }
}
```

`IndexOptions` class has this settings:

|Name                 |Default|Description                                       |
|---------------------|-------|--------------------------------------------------|
|`Unique`             |false  |Do not allow duplicates values in index           |
|`IgnoreCase`         |true   |Store string on index in lowercase                |
|`RemoveAccents`      |true   |Store string on index removing accents            |
|`TrimWhitespace`     |true   |Store string on index removing trimming spaces    |
|`EmptyStringToNull`  |true   |If string is empty, convert to `Null`             |

To get better performance, `[BsonIndex]` checks only if index exists but do not check if you are changing options. To change an index option in a existing index you must run `EnsureIndex` with new index options. This method drop index and create a new one with new options.

===========================================================================================================
Documents (concepts)
- compare store to sql (table/rows/fields)
- concepts about document store
    - document structure
    - References
    - Embedded Data (denormalized )
- concepts about collections
- example json document
- primary key - id field (not-null/max/min) + multi-field PK

BsonDocument (impl)

`BsonDocument` class are the LiteDB document implementation.

`BsonValue` implement any BSON value data type, like Null, String, Document or Array.

Documents are collection of key-value items. Keys are string datatype and has unique name per document (no duplicate are alowed). Values can be:

- Get/Set methods
- BsonTypes <> .net types
- BsonValue ctor
- Is/As methods
- Operators
- ObjectId class

Bson
- link to bsonorg
- talk about subset only
- data type order

Json
- basic concecpt about json
- extended format for bson
- JsonReader/JsonWriter + JsonSerializer

POCO
- how write poco class
- properties - nofield
- id order 
- DbRef
- attributes
    - bsonignore
    - bsonid + autoinc
    - bsonfield
    - bsonindex

BsonMapper
- glogal reference + db ref + static cache
- rules
- options
- register custom type
- register AutoId
- static Create method

Collections
- methods api
- implement T (new())

File Storage
- how works
- files e chunks collections
- id pattern - how store
- api
