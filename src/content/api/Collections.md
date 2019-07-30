---
title: 'Collections'
date: 2019-02-11T19:30:08+10:00
draft: false
weight: 1
---

## `BsonArray`

```csharp
public class LiteDB.BsonArray
    : BsonValue, IComparable<BsonValue>, IEquatable<BsonValue>, IList<BsonValue>, ICollection<BsonValue>, IEnumerable<BsonValue>, IEnumerable

```

Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `Int32` | Count |  | 
| `Boolean` | IsReadOnly |  | 
| `BsonValue` | Item |  | 
| `IList<BsonValue>` | RawValue |  | 


Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `void` | Add(`BsonValue` item) |  | 
| `void` | AddRange(`IEnumerable<BsonValue>` items) |  | 
| `void` | Clear() |  | 
| `Int32` | CompareTo(`BsonValue` other) |  | 
| `Boolean` | Contains(`BsonValue` item) |  | 
| `void` | CopyTo(`BsonValue[]` array, `Int32` arrayIndex) |  | 
| `Int32` | GetBytesCount(`Boolean` recalc) |  | 
| `IEnumerator<BsonValue>` | GetEnumerator() |  | 
| `Int32` | IndexOf(`BsonValue` item) |  | 
| `void` | Insert(`Int32` index, `BsonValue` item) |  | 
| `Boolean` | Remove(`BsonValue` item) |  | 
| `void` | RemoveAt(`Int32` index) |  | 
| `String` | ToString() |  | 


## `BsonAutoId`

All supported BsonTypes supported in AutoId insert operation
```csharp
public enum LiteDB.BsonAutoId
    : Enum, IComparable, IFormattable, IConvertible

```

Enum

| Value | Name | Summary | 
| --- | --- | --- | 
| `2` | Int32 |  | 
| `3` | Int64 |  | 
| `10` | ObjectId |  | 
| `11` | Guid |  | 


## `BsonDataReader`

Class to read void, one or a collection of BsonValues. Used in SQL execution commands and query returns. Use local data source (IEnumerable[BsonDocument])
```csharp
public class LiteDB.BsonDataReader
    : IBsonDataReader, IDisposable

```

Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `String` | Collection | Return collection name | 
| `BsonValue` | Current | Return current value | 
| `Boolean` | HasValues | Return if has any value in result | 
| `BsonValue` | Item |  | 


Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `void` | Dispose(`Boolean` disposing) |  | 
| `void` | Dispose() |  | 
| `void` | Finalize() |  | 
| `Boolean` | Read() | Move cursor to next result. Returns true if read was possible | 


## `BsonDocument`

```csharp
public class LiteDB.BsonDocument
    : BsonValue, IComparable<BsonValue>, IEquatable<BsonValue>, IDictionary<String, BsonValue>, ICollection<KeyValuePair<String, BsonValue>>, IEnumerable<KeyValuePair<String, BsonValue>>, IEnumerable

```

Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `Int32` | Count |  | 
| `Boolean` | IsReadOnly |  | 
| `BsonValue` | Item | Get/Set a field for document. Fields are case sensitive | 
| `ICollection<String>` | Keys |  | 
| `PageAddress` | RawId | Get/Set position of this document inside database. It's filled when used in Find operation. | 
| `Dictionary<String, BsonValue>` | RawValue |  | 
| `ICollection<BsonValue>` | Values |  | 


Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `void` | Add(`String` key, `BsonValue` value) |  | 
| `void` | Add(`KeyValuePair<String, BsonValue>` item) |  | 
| `void` | Clear() |  | 
| `Int32` | CompareTo(`BsonValue` other) |  | 
| `Boolean` | Contains(`KeyValuePair<String, BsonValue>` item) |  | 
| `Boolean` | ContainsKey(`String` key) |  | 
| `void` | CopyTo(`KeyValuePair`2[]` array, `Int32` arrayIndex) |  | 
| `void` | CopyTo(`BsonDocument` other) |  | 
| `Int32` | GetBytesCount(`Boolean` recalc) |  | 
| `IEnumerable<KeyValuePair<String, BsonValue>>` | GetElements() | Get all document elements - Return "_id" as first of all (if exists) | 
| `IEnumerator<KeyValuePair<String, BsonValue>>` | GetEnumerator() |  | 
| `Boolean` | Remove(`String` key) |  | 
| `Boolean` | Remove(`KeyValuePair<String, BsonValue>` item) |  | 
| `String` | ToString() |  | 
| `Boolean` | TryGetValue(`String` key, `BsonValue&` value) |  | 


## `BsonExpression`

Compile and execute string expressions using BsonDocuments. Used in all document manipulation (transform, filter, indexes, updates). See https://github.com/mbdavid/LiteDB/wiki/Expressions
```csharp
public class LiteDB.BsonExpression

```

Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `Expression` | Expression | Get transformed LINQ expression | 
| `HashSet<String>` | Fields | Fill this hashset with all fields used in root level of document (be used to partial deserialize) - "$" means all fields | 
| `Boolean` | IsAllOperator | Set if this expression are an ALL operator (used to not remove in Filter) | 
| `Boolean` | IsImmutable | If true, this expression do not change if same document/paramter are passed (only few methods change - like NOW() - or parameters) | 
| `Boolean` | IsIndexable | This expression can be indexed? To index some expression must contains fields (at least 1) and  must use only immutable methods and no parameters | 
| `Boolean` | IsPredicate | Indicate that expression evaluate to TRUE or FALSE (=, &gt;, ...). OR and AND are not considered Predicate expressions  Predicate expressions must have Left/Right expressions | 
| `Boolean` | IsScalar | Indicate if this expressions returns a single value or IEnumerable value | 
| `Boolean` | IsValue | This expression has no dependency of BsonDocument so can be used as user value (when select index) | 
| `BsonExpression` | Left | In predicate expressions, indicate Left side | 
| `BsonDocument` | Parameters | Get/Set parameter values that will be used on expression execution | 
| `BsonExpression` | Right | In predicate expressions, indicate Rigth side | 
| `String` | Source | Get formatted expression | 
| `BsonExpressionType` | Type | Indicate expression type | 
| `Boolean` | UseSource | Get/Set this expression (or any inner expression) use global Source (*) | 


Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `String` | DefaultFieldName() | Get default field name when need convert simple BsonValue into BsonDocument | 
| `IEnumerable<BsonValue>` | Execute() | Execute expression with an empty document (used only for resolve math/functions). | 
| `IEnumerable<BsonValue>` | Execute(`BsonDocument` root) | Execute expression with an empty document (used only for resolve math/functions). | 
| `IEnumerable<BsonValue>` | Execute(`IEnumerable<BsonDocument>` source) | Execute expression with an empty document (used only for resolve math/functions). | 
| `IEnumerable<BsonValue>` | Execute(`IEnumerable<BsonDocument>` source, `BsonDocument` root, `BsonValue` current) | Execute expression with an empty document (used only for resolve math/functions). | 
| `BsonValue` | ExecuteScalar() | Execute scalar expression with an empty document (used only for resolve math/functions). | 
| `BsonValue` | ExecuteScalar(`BsonDocument` root) | Execute scalar expression with an empty document (used only for resolve math/functions). | 
| `BsonValue` | ExecuteScalar(`IEnumerable<BsonDocument>` source, `BsonDocument` root, `BsonValue` current) | Execute scalar expression with an empty document (used only for resolve math/functions). | 
| `String` | ToString() |  | 


Static Fields

| Type | Name | Summary | 
| --- | --- | --- | 
| `BsonExpression` | Root | Get root document $ expression | 


Static Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `IEnumerable<MethodInfo>` | Methods | Get all registered methods for BsonExpressions | 


Static Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `BsonExpression` | Create(`String` expression) | Parse string and create new instance of BsonExpression - can be cached | 
| `BsonExpression` | Create(`String` expression, `BsonValue[]` args) | Parse string and create new instance of BsonExpression - can be cached | 
| `BsonExpression` | Create(`String` expression, `BsonDocument` parameters) | Parse string and create new instance of BsonExpression - can be cached | 
| `BsonExpression` | Create(`Tokenizer` tokenizer, `BsonDocument` parameters, `BsonExpressionParserMode` mode) | Parse string and create new instance of BsonExpression - can be cached | 
| `MethodInfo` | GetMethod(`String` name, `Int32` parameterCount) | Get expression method with same name and same parameter - return null if not found | 
| `BsonExpression` | Parse(`Tokenizer` tokenizer, `BsonExpressionParserMode` mode, `Boolean` isRoot) | Parse and compile string expression and return BsonExpression | 


## `BsonExpressionType`

```csharp
public enum LiteDB.BsonExpressionType
    : Enum, IComparable, IFormattable, IConvertible

```

Enum

| Value | Name | Summary | 
| --- | --- | --- | 
| `1` | Double |  | 
| `2` | Int |  | 
| `3` | String |  | 
| `4` | Boolean |  | 
| `5` | Null |  | 
| `6` | Array |  | 
| `7` | Document |  | 
| `8` | Parameter |  | 
| `9` | Call |  | 
| `10` | Path |  | 
| `11` | Modulo |  | 
| `12` | Add |  | 
| `13` | Subtract |  | 
| `14` | Multiply |  | 
| `15` | Divide |  | 
| `16` | Equal |  | 
| `17` | Like |  | 
| `18` | Between |  | 
| `19` | GreaterThan |  | 
| `20` | GreaterThanOrEqual |  | 
| `21` | LessThan |  | 
| `22` | LessThanOrEqual |  | 
| `23` | NotEqual |  | 
| `24` | In |  | 
| `25` | Or |  | 
| `26` | And |  | 
| `27` | Map |  | 
| `28` | Source |  | 


## `BsonFieldAttribute`

Set a name to this property in BsonDocument
```csharp
public class LiteDB.BsonFieldAttribute
    : Attribute, _Attribute

```

Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `String` | Name |  | 


## `BsonIdAttribute`

Indicate that property will be used as BsonDocument Id
```csharp
public class LiteDB.BsonIdAttribute
    : Attribute, _Attribute

```

Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `Boolean` | AutoId |  | 


## `BsonIgnoreAttribute`

Indicate that property will not be persist in Bson serialization
```csharp
public class LiteDB.BsonIgnoreAttribute
    : Attribute, _Attribute

```

## `BsonMapper`

Class that converts your entity class to/from BsonDocument  If you prefer use a new instance of BsonMapper (not Global), be sure cache this instance for better performance  Serialization rules:  - Classes must be "public" with a public constructor (without parameters)  - Properties must have public getter (can be read-only)  - Entity class must have Id property, [ClassName]Id property or [BsonId] attribute  - No circular references  - Fields are not valid  - IList, Array supports  - IDictionary supports (Key must be a simple datatype - converted by ChangeType)
```csharp
public class LiteDB.BsonMapper

```

Fields

| Type | Name | Summary | 
| --- | --- | --- | 
| `Func<Type, String>` | ResolveCollectionName | Custom resolve name collection based on Type | 
| `Func<String, String>` | ResolveFieldName | A resolver name for field | 
| `Action<Type, MemberInfo, MemberMapper>` | ResolveMember | A custom callback to change MemberInfo behavior when converting to MemberMapper.  Use mapper.ResolveMember(Type entity, MemberInfo property, MemberMapper documentMappedField)  Set FieldName to null if you want remove from mapped document | 


Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `Boolean` | EmptyStringToNull | Convert EmptyString to Null (default true) | 
| `Boolean` | IncludeFields | Get/Set that mapper must include fields (default: false) | 
| `Boolean` | IncludeNonPublic | Get/Set that mapper must include non public (private, protected and internal) (default: false) | 
| `Boolean` | SerializeNullValues | Indicate that mapper do not serialize null values (default false) | 
| `Boolean` | TrimWhitespace | Apply .Trim() in strings when serialize (default true) | 


Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `EntityMapper` | BuildEntityMapper(`Type` type) | Use this method to override how your class can be, by default, mapped from entity to Bson document.  Returns an EntityMapper from each requested Type | 
| `T` | Deserialize(`BsonValue` value) | Deserialize a BsonValue to .NET object typed in T | 
| `Object` | Deserialize(`Type` type, `BsonValue` value) | Deserialize a BsonValue to .NET object typed in T | 
| `EntityBuilder<T>` | Entity() | Map your entity class to BsonDocument using fluent API | 
| `EntityMapper` | GetEntityMapper(`Type` type) | Get property mapper between typed .NET class and BsonDocument - Cache results | 
| `BsonExpression` | GetExpression(`Expression<Func<T, K>>` predicate) | Resolve LINQ expression into BsonExpression | 
| `BsonExpression` | GetExtendExpression(`Expression<Func<T, K>>` predicate) | Resolve LINQ expression into BsonExpression | 
| `MemberInfo` | GetIdMember(`IEnumerable<MemberInfo>` members) | Gets MemberInfo that refers to Id from a document object. | 
| `IEnumerable<MemberInfo>` | GetTypeMembers(`Type` type) | Returns all member that will be have mapper between POCO class to document | 
| `void` | RegisterType(`Func<T, BsonValue>` serialize, `Func<BsonValue, T>` deserialize) | Register a custom type serializer/deserialize function | 
| `void` | RegisterType(`Type` type, `Func<Object, BsonValue>` serialize, `Func<BsonValue, Object>` deserialize) | Register a custom type serializer/deserialize function | 
| `BsonValue` | Serialize(`T` obj) | Serialize to BsonValue any .NET object based on T type (using mapping rules) | 
| `BsonValue` | Serialize(`Type` type, `Object` obj) | Serialize to BsonValue any .NET object based on T type (using mapping rules) | 
| `BsonValue` | Serialize(`Type` type, `Object` obj, `Int32` depth) | Serialize to BsonValue any .NET object based on T type (using mapping rules) | 
| `BsonDocument` | ToDocument(`Type` type, `Object` entity) | Serialize a entity class to BsonDocument | 
| `BsonDocument` | ToDocument(`T` entity) | Serialize a entity class to BsonDocument | 
| `Object` | ToObject(`Type` type, `BsonDocument` doc) | Deserialize a BsonDocument to entity class | 
| `T` | ToObject(`BsonDocument` doc) | Deserialize a BsonDocument to entity class | 
| `BsonMapper` | UseCamelCase() | Use lower camel case resolution for convert property names to field names | 
| `BsonMapper` | UseLowerCaseDelimiter(`Char` delimiter = _) | Uses lower camel case with delimiter to convert property names to field names | 


Static Fields

| Type | Name | Summary | 
| --- | --- | --- | 
| `BsonMapper` | Global | Global instance used when no BsonMapper are passed in LiteDatabase ctor | 


Static Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `void` | RegisterDbRef(`BsonMapper` mapper, `MemberMapper` member, `String` collection) | Register a property mapper as DbRef to serialize/deserialize only document reference _id | 


## `BsonRefAttribute`

Indicate that field are not persisted inside this document but it's a reference for another document (DbRef)
```csharp
public class LiteDB.BsonRefAttribute
    : Attribute, _Attribute

```

Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `String` | Collection |  | 


## `BsonSerializer`

Class to call method for convert BsonDocument to/from byte[] - based on http://bsonspec.org/spec.html  In v5 this class use new BufferRead/Writer to work with byte[] segments. This class are just a shortchut
```csharp
public class LiteDB.BsonSerializer

```

Static Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `BsonDocument` | Deserialize(`Byte[]` buffer, `Boolean` utcDate = False, `HashSet<String>` fields = null) | Deserialize binary data into BsonDocument | 
| `Byte[]` | Serialize(`BsonDocument` doc) | Serialize BsonDocument into a binary array | 


## `BsonType`

All supported BsonTypes in sort order
```csharp
public enum LiteDB.BsonType
    : Enum, IComparable, IFormattable, IConvertible

```

Enum

| Value | Name | Summary | 
| --- | --- | --- | 
| `0` | MinValue |  | 
| `1` | Null |  | 
| `2` | Int32 |  | 
| `3` | Int64 |  | 
| `4` | Double |  | 
| `5` | Decimal |  | 
| `6` | String |  | 
| `7` | Document |  | 
| `8` | Array |  | 
| `9` | Binary |  | 
| `10` | ObjectId |  | 
| `11` | Guid |  | 
| `12` | Boolean |  | 
| `13` | DateTime |  | 
| `14` | MaxValue |  | 


## `BsonValue`

Represent a Bson Value used in BsonDocument
```csharp
public class LiteDB.BsonValue
    : IComparable<BsonValue>, IEquatable<BsonValue>

```

Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `BsonArray` | AsArray |  | 
| `Byte[]` | AsBinary |  | 
| `Boolean` | AsBoolean |  | 
| `DateTime` | AsDateTime |  | 
| `Decimal` | AsDecimal |  | 
| `BsonDocument` | AsDocument |  | 
| `Double` | AsDouble |  | 
| `Guid` | AsGuid |  | 
| `Int32` | AsInt32 |  | 
| `Int64` | AsInt64 |  | 
| `ObjectId` | AsObjectId |  | 
| `String` | AsString |  | 
| `Boolean` | IsArray |  | 
| `Boolean` | IsBinary |  | 
| `Boolean` | IsBoolean |  | 
| `Boolean` | IsDateTime |  | 
| `Boolean` | IsDecimal |  | 
| `Boolean` | IsDocument |  | 
| `Boolean` | IsDouble |  | 
| `Boolean` | IsGuid |  | 
| `Boolean` | IsInt32 |  | 
| `Boolean` | IsInt64 |  | 
| `Boolean` | IsMaxValue |  | 
| `Boolean` | IsMinValue |  | 
| `Boolean` | IsNull |  | 
| `Boolean` | IsNumber |  | 
| `Boolean` | IsObjectId |  | 
| `Boolean` | IsString |  | 
| `BsonValue` | Item | Get/Set a field for document. Fields are case sensitive - Works only when value are document | 
| `BsonValue` | Item | Get/Set a field for document. Fields are case sensitive - Works only when value are document | 
| `Object` | RawValue | Get internal .NET value object | 
| `BsonType` | Type | Indicate BsonType of this BsonValue | 


Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `Int32` | CompareTo(`BsonValue` other) |  | 
| `Boolean` | Equals(`BsonValue` other) |  | 
| `Boolean` | Equals(`Object` obj) |  | 
| `Int32` | GetBytesCount(`Boolean` recalc) | Returns how many bytes this BsonValue will consume when converted into binary BSON  If recalc = false, use cached length value (from Array/Document only) | 
| `Int32` | GetBytesCountElement(`String` key, `BsonValue` value) | Get how many bytes one single element will used in BSON format | 
| `Int32` | GetHashCode() |  | 
| `String` | ToString() |  | 


Static Fields

| Type | Name | Summary | 
| --- | --- | --- | 
| `BsonValue` | MaxValue | Represent a MaxValue bson type | 
| `BsonValue` | MinValue | Represent a MinValue bson type | 
| `BsonValue` | Null | Represent a Null bson type | 
| `DateTime` | UnixEpoch |  | 


Static Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `BsonDocument` | DbRef(`BsonValue` id, `String` collection) | Create a new document used in DbRef =&gt; { $id: id, $ref: collection } | 


## `ConnectionMode`

```csharp
public enum LiteDB.ConnectionMode
    : Enum, IComparable, IFormattable, IConvertible

```

Enum

| Value | Name | Summary | 
| --- | --- | --- | 
| `0` | Exclusive |  | 
| `1` | Shared |  | 


## `ConnectionString`

Manage ConnectionString to connect and create databases. Connection string are NameValue using Name1=Value1; Name2=Value2
```csharp
public class LiteDB.ConnectionString

```

Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `String` | Filename | "filename": Full path or relative path from DLL directory  Supported in [Local, Shared] connection type | 
| `Int64` | InitialSize | "initial size": If database is new, initialize with allocated space - support KB, MB, GB (default: 0 bytes)  Supported in [Local, Shared] connection type | 
| `String` | Item | Get value from parsed connection string. Returns null if not found | 
| `Int64` | LimitSize | "limit size": Max limit of datafile - support KB, MB, GB (default: long.MaxValue - no limit)  Supported in [Local, Shared] connection type | 
| `ConnectionMode` | Mode | "type": Return how engine will be open (default: Local) | 
| `String` | Password | "password": Database password used to encrypt/decypted data pages | 
| `Boolean` | ReadOnly | "readonly": Open datafile in readonly mode (default: false)  Supported in [Local, Shared] connection type | 
| `TimeSpan` | Timeout | "timeout": Timeout for waiting unlock operations (default: 1 minute)  Supported in [Local, Shared] connection type | 
| `Boolean` | UtcDate | "utc": Returns date in UTC timezone from BSON deserialization (default: false - LocalTime)  Supported in [Local, Shared] connection type | 


Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `ILiteEngine` | CreateEngine() | Create ILiteEngine instance according string connection parameters. For now, only Local/Shared are supported | 


## `EntityBuilder<T>`

Helper class to modify your entity mapping to document. Can be used instead attribute decorates
```csharp
public class LiteDB.EntityBuilder<T>

```

Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `EntityBuilder<T>` | DbRef(`Expression<Func<T, K>>` property, `String` collection = null) | Define a subdocument (or a list of) as a reference | 
| `EntityBuilder<T>` | Field(`Expression<Func<T, K>>` property, `String` field) | Define a custom name for a property when mapping to document | 
| `EntityBuilder<T>` | Id(`Expression<Func<T, K>>` property, `Boolean` autoId = True) | Define which property is your document id (primary key). Define if this property supports auto-id | 
| `EntityBuilder<T>` | Ignore(`Expression<Func<T, K>>` property) | Define which property will not be mapped to document | 


## `EntityMapper`

Class to map entity class to BsonDocument
```csharp
public class LiteDB.EntityMapper

```

Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `Type` | ForType | Indicate which Type this entity mapper is | 
| `MemberMapper` | Id | Indicate which member is _id | 
| `List<MemberMapper>` | Members | List all type members that will be mapped to/from BsonDocument | 


Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `MemberMapper` | GetMember(`Expression` expr) | Resolve expression to get member mapped | 


## `IBsonDataReader`

```csharp
public interface LiteDB.IBsonDataReader
    : IDisposable

```

Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `String` | Collection |  | 
| `BsonValue` | Current |  | 
| `Boolean` | HasValues |  | 
| `BsonValue` | Item |  | 


Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `Boolean` | Read() |  | 


## `ILiteQueryable<T>`

```csharp
public interface LiteDB.ILiteQueryable<T>
    : ILiteQueryableResult<T>

```

Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `ILiteQueryable<T>` | ForUpdate() |  | 
| `ILiteQueryable<T>` | GroupBy(`BsonExpression` keySelector) |  | 
| `ILiteQueryable<T>` | GroupBy(`Expression<Func<T, K>>` keySelector) |  | 
| `ILiteQueryable<T>` | Having(`BsonExpression` predicate) |  | 
| `ILiteQueryable<T>` | Having(`Expression<Func<IEnumerable<T>, Boolean>>` keySelector) |  | 
| `ILiteQueryable<T>` | Include(`BsonExpression` path) |  | 
| `ILiteQueryable<T>` | Include(`List<BsonExpression>` paths) |  | 
| `ILiteQueryable<T>` | Include(`Expression<Func<T, K>>` path) |  | 
| `ILiteQueryable<T>` | Limit(`Int32` limit) |  | 
| `ILiteQueryable<T>` | Offset(`Int32` offset) |  | 
| `ILiteQueryable<T>` | OrderBy(`BsonExpression` keySelector, `Int32` order = 1) |  | 
| `ILiteQueryable<T>` | OrderBy(`Expression<Func<T, K>>` keySelector, `Int32` order = 1) |  | 
| `ILiteQueryable<T>` | OrderByDescending(`BsonExpression` keySelector) |  | 
| `ILiteQueryable<T>` | OrderByDescending(`Expression<Func<T, K>>` keySelector) |  | 
| `ILiteQueryableResult<BsonDocument>` | Select(`BsonExpression` selector) |  | 
| `ILiteQueryableResult<K>` | Select(`Expression<Func<T, K>>` selector) |  | 
| `ILiteQueryableResult<K>` | SelectAll(`Expression<Func<IEnumerable<T>, K>>` selector) |  | 
| `ILiteQueryable<T>` | Skip(`Int32` offset) |  | 
| `ILiteQueryable<T>` | Where(`BsonExpression` predicate) |  | 
| `ILiteQueryable<T>` | Where(`String` predicate, `BsonDocument` parameters) |  | 
| `ILiteQueryable<T>` | Where(`String` predicate, `BsonValue[]` args) |  | 
| `ILiteQueryable<T>` | Where(`Expression<Func<T, Boolean>>` predicate) |  | 


## `ILiteQueryableResult<T>`

```csharp
public interface LiteDB.ILiteQueryableResult<T>

```

Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `Int32` | Count() |  | 
| `IBsonDataReader` | ExecuteReader() |  | 
| `Boolean` | Exists() |  | 
| `T` | First() |  | 
| `T` | FirstOrDefault() |  | 
| `BsonDocument` | GetPlan() |  | 
| `Int32` | Into(`String` newCollection, `BsonAutoId` autoId = ObjectId) |  | 
| `Int64` | LongCount() |  | 
| `T` | Single() |  | 
| `T` | SingleOrDefault() |  | 
| `T[]` | ToArray() |  | 
| `IEnumerable<BsonDocument>` | ToDocuments() |  | 
| `IEnumerable<T>` | ToEnumerable() |  | 
| `List<T>` | ToList() |  | 


## `JsonReader`

A class that read a json string using a tokenizer (without regex)
```csharp
public class LiteDB.JsonReader

```

Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `Int64` | Position |  | 


Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `BsonValue` | Deserialize() |  | 
| `IEnumerable<BsonValue>` | DeserializeArray() |  | 
| `BsonValue` | ReadValue(`Token` token) |  | 


## `JsonSerializer`

Static class for serialize/deserialize BsonDocuments into json extended format
```csharp
public class LiteDB.JsonSerializer

```

Static Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `BsonValue` | Deserialize(`String` json) | Deserialize a Json string into a BsonValue | 
| `BsonValue` | Deserialize(`TextReader` reader) | Deserialize a Json string into a BsonValue | 
| `IEnumerable<BsonValue>` | DeserializeArray(`String` json) | Deserialize a json array as an IEnumerable of BsonValue | 
| `IEnumerable<BsonValue>` | DeserializeArray(`TextReader` reader) | Deserialize a json array as an IEnumerable of BsonValue | 
| `String` | Serialize(`BsonValue` value) | Json serialize a BsonValue into a String | 
| `void` | Serialize(`BsonValue` value, `TextWriter` writer) | Json serialize a BsonValue into a String | 
| `void` | Serialize(`BsonValue` value, `StringBuilder` sb) | Json serialize a BsonValue into a String | 


## `JsonWriter`

```csharp
public class LiteDB.JsonWriter

```

Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `Int32` | Indent | Get/Set indent size | 
| `Boolean` | Pretty | Get/Set if writer must print pretty (with new line/indent) | 


Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `void` | Serialize(`BsonValue` value) | Serialize value into text writer | 


## `LiteCollection<T>`

```csharp
public class LiteDB.LiteCollection<T>

```

Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `String` | Name | Get collection name | 


Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `Int32` | Count() | Get document count using property on collection. | 
| `Int32` | Count(`BsonExpression` predicate) | Get document count using property on collection. | 
| `Int32` | Count(`String` predicate, `BsonDocument` parameters) | Get document count using property on collection. | 
| `Int32` | Count(`String` predicate, `BsonValue[]` args) | Get document count using property on collection. | 
| `Int32` | Count(`Expression<Func<T, Boolean>>` predicate) | Get document count using property on collection. | 
| `Boolean` | Delete(`BsonValue` id) | Delete a single document on collection based on _id index. Returns true if document was deleted | 
| `Int32` | DeleteMany(`BsonExpression` predicate) | Delete all documents based on predicate expression. Returns how many documents was deleted | 
| `Int32` | DeleteMany(`String` predicate, `BsonDocument` parameters) | Delete all documents based on predicate expression. Returns how many documents was deleted | 
| `Int32` | DeleteMany(`String` predicate, `BsonValue[]` args) | Delete all documents based on predicate expression. Returns how many documents was deleted | 
| `Int32` | DeleteMany(`Expression<Func<T, Boolean>>` predicate) | Delete all documents based on predicate expression. Returns how many documents was deleted | 
| `Boolean` | DropIndex(`String` name) | Drop index and release slot for another index | 
| `Boolean` | EnsureIndex(`String` name, `BsonExpression` expression, `Boolean` unique = False) | Create a new permanent index in all documents inside this collections if index not exists already. Returns true if index was created or false if already exits | 
| `Boolean` | EnsureIndex(`BsonExpression` expression, `Boolean` unique = False) | Create a new permanent index in all documents inside this collections if index not exists already. Returns true if index was created or false if already exits | 
| `Boolean` | EnsureIndex(`Expression<Func<T, K>>` keySelector, `Boolean` unique = False) | Create a new permanent index in all documents inside this collections if index not exists already. Returns true if index was created or false if already exits | 
| `Boolean` | EnsureIndex(`String` name, `Expression<Func<T, K>>` keySelector, `Boolean` unique = False) | Create a new permanent index in all documents inside this collections if index not exists already. Returns true if index was created or false if already exits | 
| `Boolean` | Exists(`BsonExpression` predicate) | Returns true if query returns any document. This method does not deserialize any document. Needs indexes on query expression | 
| `Boolean` | Exists(`String` predicate, `BsonDocument` parameters) | Returns true if query returns any document. This method does not deserialize any document. Needs indexes on query expression | 
| `Boolean` | Exists(`String` predicate, `BsonValue[]` args) | Returns true if query returns any document. This method does not deserialize any document. Needs indexes on query expression | 
| `Boolean` | Exists(`Expression<Func<T, Boolean>>` predicate) | Returns true if query returns any document. This method does not deserialize any document. Needs indexes on query expression | 
| `IEnumerable<T>` | Find(`BsonExpression` predicate, `Int32` skip = 0, `Int32` limit = 2147483647) | Find documents inside a collection using predicate expression. | 
| `IEnumerable<T>` | Find(`Expression<Func<T, Boolean>>` predicate, `Int32` skip = 0, `Int32` limit = 2147483647) | Find documents inside a collection using predicate expression. | 
| `IEnumerable<T>` | FindAll() | Returns all documents inside collection order by _id index. | 
| `T` | FindById(`BsonValue` id) | Find a document using Document Id. Returns null if not found. | 
| `T` | FindOne(`BsonExpression` predicate) | Find the first document using predicate expression. Returns null if not found | 
| `T` | FindOne(`String` predicate, `BsonDocument` parameters) | Find the first document using predicate expression. Returns null if not found | 
| `T` | FindOne(`BsonExpression` predicate, `BsonValue[]` args) | Find the first document using predicate expression. Returns null if not found | 
| `T` | FindOne(`Expression<Func<T, Boolean>>` predicate) | Find the first document using predicate expression. Returns null if not found | 
| `LiteCollection<T>` | Include(`Expression<Func<T, K>>` keySelector) | Run an include action in each document returned by Find(), FindById(), FindOne() and All() methods to load DbRef documents  Returns a new Collection with this action included | 
| `LiteCollection<T>` | Include(`BsonExpression` keySelector) | Run an include action in each document returned by Find(), FindById(), FindOne() and All() methods to load DbRef documents  Returns a new Collection with this action included | 
| `BsonValue` | Insert(`T` document) | Insert a new entity to this collection. Document Id must be a new value in collection - Returns document Id | 
| `void` | Insert(`BsonValue` id, `T` document) | Insert a new entity to this collection. Document Id must be a new value in collection - Returns document Id | 
| `Int32` | Insert(`IEnumerable<T>` docs) | Insert a new entity to this collection. Document Id must be a new value in collection - Returns document Id | 
| `Int64` | LongCount() | Get document count using property on collection. | 
| `Int64` | LongCount(`BsonExpression` predicate) | Get document count using property on collection. | 
| `Int64` | LongCount(`String` predicate, `BsonDocument` parameters) | Get document count using property on collection. | 
| `Int64` | LongCount(`String` predicate, `BsonValue[]` args) | Get document count using property on collection. | 
| `Int64` | LongCount(`Expression<Func<T, Boolean>>` predicate) | Get document count using property on collection. | 
| `BsonValue` | Max(`BsonExpression` keySelector) | Returns the max value from specified key value in collection | 
| `BsonValue` | Max() | Returns the max value from specified key value in collection | 
| `K` | Max(`Expression<Func<T, K>>` keySelector) | Returns the max value from specified key value in collection | 
| `BsonValue` | Min(`BsonExpression` keySelector) | Returns the min value from specified key value in collection | 
| `BsonValue` | Min() | Returns the min value from specified key value in collection | 
| `K` | Min(`Expression<Func<T, K>>` keySelector) | Returns the min value from specified key value in collection | 
| `ILiteQueryable<T>` | Query() | Return a new LiteQueryable to build more complex queries | 
| `Boolean` | Update(`T` document) | Update a document in this collection. Returns false if not found document in collection | 
| `Boolean` | Update(`BsonValue` id, `T` document) | Update a document in this collection. Returns false if not found document in collection | 
| `Int32` | Update(`IEnumerable<T>` documents) | Update a document in this collection. Returns false if not found document in collection | 
| `Int32` | UpdateMany(`BsonExpression` transform, `BsonExpression` predicate) | Update many documents based on transform expression. This expression must return a new document that will be replaced over current document (according with predicate).  Eg: col.UpdateMany("{ Name: UPPER($.Name), Age }", "_id &gt; 0") | 
| `Int32` | UpdateMany(`Expression<Func<T, T>>` extend, `Expression<Func<T, Boolean>>` predicate) | Update many documents based on transform expression. This expression must return a new document that will be replaced over current document (according with predicate).  Eg: col.UpdateMany("{ Name: UPPER($.Name), Age }", "_id &gt; 0") | 
| `Boolean` | Upsert(`T` document) | Insert or Update a document in this collection. | 
| `Int32` | Upsert(`IEnumerable<T>` documents) | Insert or Update a document in this collection. | 
| `Boolean` | Upsert(`BsonValue` id, `T` document) | Insert or Update a document in this collection. | 


## `LiteDatabase`

The LiteDB database. Used for create a LiteDB instance and use all storage resources. It's the database connection
```csharp
public class LiteDB.LiteDatabase
    : IDisposable

```

Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `LiteStorage<String>` | FileStorage | Returns a special collection for storage files/stream inside datafile. Use _files and _chunks collection names. FileId is implemented as string. Use "GetStorage" for custom options | 
| `BsonMapper` | Mapper | Get current instance of BsonMapper used in this database instance (can be BsonMapper.Global) | 
| `Int32` | UserVersion | Get/Set database user version - use this version number to control database change model | 


Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `Int32` | Analyze(`String[]` collections) | Analyze indexes in collections to better index choose decision | 
| `Boolean` | BeginTrans() | Initialize a new transaction. Transaction are created "per-thread". There is only one single transaction per thread.  Return true if transaction was created or false if current thread already in a transaction. | 
| `void` | Checkpoint() | Do database checkpoint. Copy all commited transaction from log file into datafile. | 
| `Boolean` | CollectionExists(`String` name) | Checks if a collection exists on database. Collection name is case insensitive | 
| `Boolean` | Commit() | Commit current transaction | 
| `void` | Dispose() |  | 
| `Boolean` | DropCollection(`String` name) | Drop a collection and all data + indexes | 
| `IBsonDataReader` | Execute(`TextReader` commandReader, `BsonDocument` parameters = null) | Execute SQL commands and return as data reader. | 
| `IBsonDataReader` | Execute(`String` command, `BsonDocument` parameters = null) | Execute SQL commands and return as data reader. | 
| `IBsonDataReader` | Execute(`String` command, `BsonValue[]` args) | Execute SQL commands and return as data reader. | 
| `LiteCollection<T>` | GetCollection(`String` name) | Get a collection using a entity class as strong typed document. If collection does not exits, create a new one. | 
| `LiteCollection<T>` | GetCollection() | Get a collection using a entity class as strong typed document. If collection does not exits, create a new one. | 
| `LiteCollection<T>` | GetCollection(`BsonAutoId` autoId) | Get a collection using a entity class as strong typed document. If collection does not exits, create a new one. | 
| `LiteCollection<BsonDocument>` | GetCollection(`String` name, `BsonAutoId` autoId = ObjectId) | Get a collection using a entity class as strong typed document. If collection does not exits, create a new one. | 
| `IEnumerable<String>` | GetCollectionNames() | Get all collections name inside this database. | 
| `LiteStorage<TFileId>` | GetStorage(`String` filesCollection = _files, `String` chunksCollection = _chunks) | Get new instance of Storage using custom FileId type, custom "_files" collection name and custom "_chunks" collection. LiteDB support multiples file storages (using different files/chunks collection names) | 
| `Boolean` | RenameCollection(`String` oldName, `String` newName) | Rename a collection. Returns false if oldName does not exists or newName already exists | 
| `Boolean` | Rollback() | Rollback current transaction | 
| `Int32` | Vaccum() | Analyze all database to find-and-fix non linked empty pages | 


## `LiteException`

The main exception for LiteDB
```csharp
public class LiteDB.LiteException
    : Exception, ISerializable, _Exception

```

Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `Int32` | ErrorCode |  | 
| `Int64` | Position |  | 


Static Fields

| Type | Name | Summary | 
| --- | --- | --- | 
| `Int32` | ALREADY_EXISTS_COLLECTION_NAME |  | 
| `Int32` | ALREADY_OPEN_DATAFILE |  | 
| `Int32` | COLLECTION_ALREADY_EXIST |  | 
| `Int32` | COLLECTION_LIMIT_EXCEEDED |  | 
| `Int32` | COLLECTION_NOT_FOUND |  | 
| `Int32` | DATABASE_SHUTDOWN |  | 
| `Int32` | DOCUMENT_MAX_DEPTH |  | 
| `Int32` | FILE_NOT_FOUND |  | 
| `Int32` | FILE_SIZE_EXCEEDED |  | 
| `Int32` | INDEX_ALREADY_EXIST |  | 
| `Int32` | INDEX_DROP_ID |  | 
| `Int32` | INDEX_DUPLICATE_KEY |  | 
| `Int32` | INDEX_NAME_LIMIT_EXCEEDED |  | 
| `Int32` | INDEX_NOT_FOUND |  | 
| `Int32` | INVALID_COLLECTION_NAME |  | 
| `Int32` | INVALID_COMMAND |  | 
| `Int32` | INVALID_CTOR |  | 
| `Int32` | INVALID_DATA_TYPE |  | 
| `Int32` | INVALID_DATABASE |  | 
| `Int32` | INVALID_DBREF |  | 
| `Int32` | INVALID_EXPRESSION_TYPE |  | 
| `Int32` | INVALID_FORMAT |  | 
| `Int32` | INVALID_INDEX_KEY |  | 
| `Int32` | INVALID_INDEX_NAME |  | 
| `Int32` | INVALID_TRANSACTION_STATE |  | 
| `Int32` | INVALID_TYPED_NAME |  | 
| `Int32` | INVALID_UPDATE_FIELD |  | 
| `Int32` | LOCK_TIMEOUT |  | 
| `Int32` | PROPERTY_NOT_MAPPED |  | 
| `Int32` | PROPERTY_READ_WRITE |  | 
| `Int32` | TEMP_ENGINE_ALREADY_DEFINED |  | 
| `Int32` | UNEXPECTED_TOKEN |  | 


Static Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `LiteException` | AlreadyExistsCollectionName(`String` newName) |  | 
| `LiteException` | AlreadyExistsTransaction() |  | 
| `LiteException` | AlreadyOpenDatafile(`String` filename) |  | 
| `LiteException` | CollectionAlreadyExist(`String` key) |  | 
| `LiteException` | CollectionLimitExceeded(`Int32` limit) |  | 
| `LiteException` | CollectionLockerNotFound(`String` collection) |  | 
| `LiteException` | CollectionNotFound(`String` key) |  | 
| `LiteException` | DatabaseShutdown() |  | 
| `LiteException` | DocumentMaxDepth(`Int32` depth, `Type` type) |  | 
| `LiteException` | FileNotFound(`Object` fileId) |  | 
| `LiteException` | FileSizeExceeded(`Int64` limit) |  | 
| `LiteException` | IndexAlreadyExist(`String` name) |  | 
| `LiteException` | IndexDropId() |  | 
| `LiteException` | IndexDuplicateKey(`String` field, `BsonValue` key) |  | 
| `LiteException` | IndexNameLimitExceeded(`Int32` limit) |  | 
| `LiteException` | IndexNotFound(`String` name) |  | 
| `LiteException` | InvalidCollectionName(`String` name, `String` reason) |  | 
| `LiteException` | InvalidCommand(`String` command) |  | 
| `LiteException` | InvalidCtor(`Type` type, `Exception` inner) |  | 
| `LiteException` | InvalidDatabase() |  | 
| `LiteException` | InvalidDataType(`String` field, `BsonValue` value) |  | 
| `LiteException` | InvalidDbRef(`String` path) |  | 
| `LiteException` | InvalidExpressionType(`BsonExpression` expr, `BsonExpressionType` type) |  | 
| `LiteException` | InvalidExpressionTypePredicate(`BsonExpression` expr) |  | 
| `LiteException` | InvalidFormat(`String` field) |  | 
| `LiteException` | InvalidIndexKey(`String` text) |  | 
| `LiteException` | InvalidIndexName(`String` name, `String` collection, `String` reason) |  | 
| `LiteException` | InvalidTypedName(`String` type) |  | 
| `LiteException` | InvalidUpdateField(`String` field) |  | 
| `LiteException` | LockTimeout(`String` mode, `TimeSpan` ts) |  | 
| `LiteException` | LockTimeout(`String` mode, `String` collection, `TimeSpan` ts) |  | 
| `LiteException` | PropertyNotMapped(`String` name) |  | 
| `LiteException` | PropertyReadWrite(`PropertyInfo` prop) |  | 
| `LiteException` | TempEngineAlreadyDefined() |  | 
| `LiteException` | UnexpectedToken(`Token` token, `String` expected = null) |  | 
| `LiteException` | UnexpectedToken(`String` message, `Token` token) |  | 


## `LiteFileInfo<TFileId>`

Represents a file inside storage collection
```csharp
public class LiteDB.LiteFileInfo<TFileId>

```

Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `Int32` | Chunks |  | 
| `String` | Filename |  | 
| `TFileId` | Id |  | 
| `Int64` | Length |  | 
| `BsonDocument` | Metadata |  | 
| `String` | MimeType |  | 
| `DateTime` | UploadDate |  | 


Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `void` | CopyTo(`Stream` stream) | Copy file content to another stream | 
| `LiteFileStream<TFileId>` | OpenRead() | Open file stream to read from database | 
| `LiteFileStream<TFileId>` | OpenWrite() | Open file stream to write to database | 
| `void` | SaveAs(`String` filename, `Boolean` overwritten = True) | Save file content to a external file | 
| `void` | SetReference(`BsonValue` fileId, `LiteCollection<LiteFileInfo<TFileId>>` files, `LiteCollection<BsonDocument>` chunks) |  | 


## `LiteFileStream<TFileId>`

```csharp
public class LiteDB.LiteFileStream<TFileId>
    : Stream, IDisposable

```

Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `Boolean` | CanRead |  | 
| `Boolean` | CanSeek |  | 
| `Boolean` | CanWrite |  | 
| `LiteFileInfo<TFileId>` | FileInfo | Get file information | 
| `Int64` | Length |  | 
| `Int64` | Position |  | 


Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `void` | Dispose(`Boolean` disposing) |  | 
| `void` | Finalize() |  | 
| `void` | Flush() |  | 
| `Int32` | Read(`Byte[]` buffer, `Int32` offset, `Int32` count) |  | 
| `Int64` | Seek(`Int64` offset, `SeekOrigin` origin) |  | 
| `void` | SetLength(`Int64` value) |  | 
| `void` | Write(`Byte[]` buffer, `Int32` offset, `Int32` count) |  | 


Static Fields

| Type | Name | Summary | 
| --- | --- | --- | 
| `Int32` | MAX_CHUNK_SIZE | Number of bytes on each chunk document to store | 


## `LiteQueryable<T>`

An IQueryable-like class to write fluent query in documents in collection.
```csharp
public class LiteDB.LiteQueryable<T>
    : ILiteQueryable<T>, ILiteQueryableResult<T>

```

Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `Int32` | Count() | Execute Count methos in filter query | 
| `IBsonDataReader` | ExecuteReader() | Execute query and returns resultset as generic BsonDataReader | 
| `Boolean` | Exists() | Returns true/false if query returns any result | 
| `T` | First() | Returns first document of resultset | 
| `T` | FirstOrDefault() | Returns first document of resultset or null if resultset are empty | 
| `ILiteQueryable<T>` | ForUpdate() | Execute query locking collection in write mode. This is avoid any other thread change results after read document and before transaction ends | 
| `BsonDocument` | GetPlan() | Get execution plan over current query definition to see how engine will execute query | 
| `ILiteQueryable<T>` | GroupBy(`BsonExpression` keySelector) | Groups the documents of resultset according to a specified key selector expression (support only one GroupBy) | 
| `ILiteQueryable<T>` | GroupBy(`Expression<Func<T, K>>` keySelector) | Groups the documents of resultset according to a specified key selector expression (support only one GroupBy) | 
| `ILiteQueryable<T>` | Having(`BsonExpression` predicate) | Filter documents after group by pipe according to predicate expression (requires GroupBy and support only one Having) | 
| `ILiteQueryable<T>` | Having(`Expression<Func<IEnumerable<T>, Boolean>>` predicate) | Filter documents after group by pipe according to predicate expression (requires GroupBy and support only one Having) | 
| `ILiteQueryable<T>` | Include(`Expression<Func<T, K>>` path) | Load cross reference documents from path expression (DbRef reference) | 
| `ILiteQueryable<T>` | Include(`BsonExpression` path) | Load cross reference documents from path expression (DbRef reference) | 
| `ILiteQueryable<T>` | Include(`List<BsonExpression>` paths) | Load cross reference documents from path expression (DbRef reference) | 
| `Int32` | Into(`String` newCollection, `BsonAutoId` autoId = ObjectId) |  | 
| `ILiteQueryable<T>` | Limit(`Int32` limit) | Return a specified number of contiguous documents from start of resultset | 
| `Int64` | LongCount() | Execute Count methos in filter query | 
| `ILiteQueryable<T>` | Offset(`Int32` offset) | Bypasses a specified number of documents in resultset and retun the remaining documents (same as Skip) | 
| `ILiteQueryable<T>` | OrderBy(`BsonExpression` keySelector, `Int32` order = 1) | Sort the documents of resultset in ascending (or descending) order according to a key (support only one OrderBy) | 
| `ILiteQueryable<T>` | OrderBy(`Expression<Func<T, K>>` keySelector, `Int32` order = 1) | Sort the documents of resultset in ascending (or descending) order according to a key (support only one OrderBy) | 
| `ILiteQueryable<T>` | OrderByDescending(`BsonExpression` keySelector) | Sort the documents of resultset in descending order according to a key (support only one OrderBy) | 
| `ILiteQueryable<T>` | OrderByDescending(`Expression<Func<T, K>>` keySelector) | Sort the documents of resultset in descending order according to a key (support only one OrderBy) | 
| `ILiteQueryableResult<BsonDocument>` | Select(`BsonExpression` selector) | Transform input document into a new output document. Can be used with each document, group by or all source | 
| `ILiteQueryableResult<K>` | Select(`Expression<Func<T, K>>` selector) | Transform input document into a new output document. Can be used with each document, group by or all source | 
| `ILiteQueryableResult<K>` | SelectAll(`Expression<Func<IEnumerable<T>, K>>` selector) | Project all documents inside a single expression. Output will be a single document or one document per group (used in GroupBy) | 
| `T` | Single() | Returns the only document of resultset, and throw an exception if there not exactly one document in the sequence | 
| `T` | SingleOrDefault() | Returns the only document of resultset, or null if resultset are empty; this method throw an exception if there not exactly one document in the sequence | 
| `ILiteQueryable<T>` | Skip(`Int32` offset) | Bypasses a specified number of documents in resultset and retun the remaining documents (same as Offset) | 
| `T[]` | ToArray() | Execute query and return results as an Array | 
| `IEnumerable<BsonDocument>` | ToDocuments() | Execute query and return resultset as IEnumerable of documents | 
| `IEnumerable<T>` | ToEnumerable() | Execute query and return resultset as IEnumerable of T. If T is a ValueType or String, return values only (not documents) | 
| `List<T>` | ToList() | Execute query and return results as a List | 
| `ILiteQueryable<T>` | Where(`BsonExpression` predicate) | Filters a sequence of documents based on a predicate expression | 
| `ILiteQueryable<T>` | Where(`String` predicate, `BsonDocument` parameters) | Filters a sequence of documents based on a predicate expression | 
| `ILiteQueryable<T>` | Where(`String` predicate, `BsonValue[]` args) | Filters a sequence of documents based on a predicate expression | 
| `ILiteQueryable<T>` | Where(`Expression<Func<T, Boolean>>` predicate) | Filters a sequence of documents based on a predicate expression | 


## `LiteRepository`

The LiteDB repository pattern. A simple way to access your documents in a single class with fluent query api
```csharp
public class LiteDB.LiteRepository
    : IDisposable

```

Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `LiteDatabase` | Database | Get database instance | 


Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `Boolean` | Delete(`BsonValue` id, `String` collectionName = null) | Delete entity based on _id key | 
| `Int32` | DeleteMany(`BsonExpression` predicate, `String` collectionName = null) | Delete entity based on Query | 
| `Int32` | DeleteMany(`Expression<Func<T, Boolean>>` predicate, `String` collectionName = null) | Delete entity based on Query | 
| `void` | Dispose() |  | 
| `List<T>` | Fetch(`BsonExpression` predicate, `String` collectionName = null) | Execute Query[T].Where(predicate).ToList(); | 
| `List<T>` | Fetch(`Expression<Func<T, Boolean>>` predicate, `String` collectionName = null) | Execute Query[T].Where(predicate).ToList(); | 
| `T` | First(`BsonExpression` predicate, `String` collectionName = null) | Execute Query[T].Where(predicate).First(); | 
| `T` | First(`Expression<Func<T, Boolean>>` predicate, `String` collectionName = null) | Execute Query[T].Where(predicate).First(); | 
| `T` | FirstOrDefault(`BsonExpression` predicate, `String` collectionName = null) | Execute Query[T].Where(predicate).FirstOrDefault(); | 
| `T` | FirstOrDefault(`Expression<Func<T, Boolean>>` predicate, `String` collectionName = null) | Execute Query[T].Where(predicate).FirstOrDefault(); | 
| `void` | Insert(`T` entity, `String` collectionName = null) | Insert a new document into collection. Document Id must be a new value in collection - Returns document Id | 
| `Int32` | Insert(`IEnumerable<T>` entities, `String` collectionName = null) | Insert a new document into collection. Document Id must be a new value in collection - Returns document Id | 
| `ILiteQueryable<T>` | Query(`String` collectionName = null) | Returns new instance of LiteQueryable that provides all method to query any entity inside collection. Use fluent API to apply filter/includes an than run any execute command, like ToList() or First() | 
| `T` | Single(`BsonExpression` predicate, `String` collectionName = null) | Execute Query[T].Where(predicate).Single(); | 
| `T` | Single(`Expression<Func<T, Boolean>>` predicate, `String` collectionName = null) | Execute Query[T].Where(predicate).Single(); | 
| `T` | SingleById(`BsonValue` id, `String` collectionName = null) | Search for a single instance of T by Id. Shortcut from Query.SingleById | 
| `T` | SingleOrDefault(`BsonExpression` predicate, `String` collectionName = null) | Execute Query[T].Where(predicate).SingleOrDefault(); | 
| `T` | SingleOrDefault(`Expression<Func<T, Boolean>>` predicate, `String` collectionName = null) | Execute Query[T].Where(predicate).SingleOrDefault(); | 
| `Boolean` | Update(`T` entity, `String` collectionName = null) | Update a document into collection. Returns false if not found document in collection | 
| `Int32` | Update(`IEnumerable<T>` entities, `String` collectionName = null) | Update a document into collection. Returns false if not found document in collection | 
| `Boolean` | Upsert(`T` entity, `String` collectionName = null) | Insert or Update a document based on _id key. Returns true if insert entity or false if update entity | 
| `Int32` | Upsert(`IEnumerable<T>` entities, `String` collectionName = null) | Insert or Update a document based on _id key. Returns true if insert entity or false if update entity | 


## `LiteStorage<TFileId>`

Storage is a special collection to store files and streams.
```csharp
public class LiteDB.LiteStorage<TFileId>

```

Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `Boolean` | Delete(`TFileId` id) | Delete a file inside datafile and all metadata related | 
| `LiteFileInfo<TFileId>` | Download(`TFileId` id, `Stream` stream) | Copy all file content to a steam | 
| `LiteFileInfo<TFileId>` | Download(`TFileId` id, `String` filename, `Boolean` overwritten) | Copy all file content to a steam | 
| `Boolean` | Exists(`TFileId` id) | Returns if a file exisits in database | 
| `IEnumerable<LiteFileInfo<TFileId>>` | Find(`BsonExpression` predicate) | Find all files that match with predicate expression. | 
| `IEnumerable<LiteFileInfo<TFileId>>` | Find(`String` predicate, `BsonDocument` parameters) | Find all files that match with predicate expression. | 
| `IEnumerable<LiteFileInfo<TFileId>>` | Find(`String` predicate, `BsonValue[]` args) | Find all files that match with predicate expression. | 
| `IEnumerable<LiteFileInfo<TFileId>>` | Find(`Expression<Func<LiteFileInfo<TFileId>, Boolean>>` predicate) | Find all files that match with predicate expression. | 
| `IEnumerable<LiteFileInfo<TFileId>>` | FindAll() | Find all files inside file collections | 
| `LiteFileInfo<TFileId>` | FindById(`TFileId` id) | Find a file inside datafile and returns LiteFileInfo instance. Returns null if not found | 
| `LiteFileStream<TFileId>` | OpenRead(`TFileId` id) | Load data inside storage and returns as Stream | 
| `LiteFileStream<TFileId>` | OpenWrite(`TFileId` id, `String` filename, `BsonDocument` metadata = null) | Open/Create new file storage and returns linked Stream to write operations. | 
| `Boolean` | SetMetadata(`TFileId` id, `BsonDocument` metadata) | Update metadata on a file. File must exist. | 
| `LiteFileInfo<TFileId>` | Upload(`TFileId` id, `String` filename, `Stream` stream, `BsonDocument` metadata = null) | Upload a file based on stream data | 
| `LiteFileInfo<TFileId>` | Upload(`TFileId` id, `String` filename) | Upload a file based on stream data | 


## `MemberMapper`

Internal representation for a .NET member mapped to BsonDocument
```csharp
public class LiteDB.MemberMapper

```

Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `Boolean` | AutoId | If member is Id, indicate that are AutoId | 
| `Type` | DataType | Member returns data type | 
| `Func<BsonValue, BsonMapper, Object>` | Deserialize | When used, can define a deserialization function from bson value | 
| `String` | FieldName | Converted document field name | 
| `GenericGetter` | Getter | Delegate method to get value from entity instance | 
| `Boolean` | IsDbRef | Is this property an DbRef? Must implement Serialize/Deserialize delegates | 
| `Boolean` | IsList | Indicate that this property contains an list of elements (IEnumerable) | 
| `String` | MemberName | Member name | 
| `Func<Object, BsonMapper, BsonValue>` | Serialize | When used, can be define a serialization function from entity class to bson value | 
| `GenericSetter` | Setter | Delegate method to set value to entity instance | 
| `Type` | UnderlyingType | When property is an array of items, gets underlying type (otherwise is same type of PropertyType) | 


## `ObjectId`

Represent a 12-bytes BSON type used in document Id
```csharp
public class LiteDB.ObjectId
    : IComparable<ObjectId>, IEquatable<ObjectId>

```

Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `DateTime` | CreationTime | Get creation time | 
| `Int32` | Increment | Get increment | 
| `Int32` | Machine | Get machine number | 
| `Int16` | Pid | Get pid number | 
| `Int32` | Timestamp | Get timestamp | 


Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `Int32` | CompareTo(`ObjectId` other) | Compares two instances of ObjectId | 
| `Boolean` | Equals(`ObjectId` other) | Checks if this ObjectId is equal to the given object. Returns true  if the given object is equal to the value of this instance.  Returns false otherwise. | 
| `Boolean` | Equals(`Object` other) | Checks if this ObjectId is equal to the given object. Returns true  if the given object is equal to the value of this instance.  Returns false otherwise. | 
| `Int32` | GetHashCode() | Returns a hash code for this instance. | 
| `void` | ToByteArray(`Byte[]` bytes, `Int32` startIndex) | Represent ObjectId as 12 bytes array | 
| `Byte[]` | ToByteArray() | Represent ObjectId as 12 bytes array | 
| `String` | ToString() |  | 


Static Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `ObjectId` | Empty | A zero 12-bytes ObjectId | 


Static Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `ObjectId` | NewObjectId() | Creates a new ObjectId. | 


## `Query`

Represent full query options
```csharp
public class LiteDB.Query

```

Properties

| Type | Name | Summary | 
| --- | --- | --- | 
| `Boolean` | ExplainPlan |  | 
| `Boolean` | ForUpdate |  | 
| `BsonExpression` | GroupBy |  | 
| `BsonExpression` | Having |  | 
| `List<BsonExpression>` | Includes |  | 
| `String` | Into |  | 
| `BsonAutoId` | IntoAutoId |  | 
| `Int32` | Limit |  | 
| `Int32` | Offset |  | 
| `Int32` | Order |  | 
| `BsonExpression` | OrderBy |  | 
| `BsonExpression` | Select |  | 
| `List<BsonExpression>` | Where |  | 


Static Fields

| Type | Name | Summary | 
| --- | --- | --- | 
| `Int32` | Ascending | Indicate when a query must execute in ascending order | 
| `Int32` | Descending | Indicate when a query must execute in descending order | 


Static Methods

| Type | Name | Summary | 
| --- | --- | --- | 
| `BsonExpression` | And(`BsonExpression` left, `BsonExpression` right) | Returns document that exists in BOTH queries results. If both queries has indexes, left query has index preference (other side will be run in full scan) | 
| `BsonExpression` | And(`BsonExpression[]` queries) | Returns document that exists in BOTH queries results. If both queries has indexes, left query has index preference (other side will be run in full scan) | 
| `BsonExpression` | Between(`String` field, `BsonValue` start, `BsonValue` end) | Returns all document that values are between "start" and "end" values (BETWEEN) | 
| `BsonExpression` | Contains(`String` field, `String` value) | Returns all documents that contains value (CONTAINS) | 
| `BsonExpression` | EQ(`String` field, `BsonValue` value) | Returns all documents that value are equals to value (=) | 
| `BsonExpression` | GT(`String` field, `BsonValue` value) | Returns all document that value are greater than value (&gt;) | 
| `BsonExpression` | GTE(`String` field, `BsonValue` value) | Returns all documents that value are greater than or equals value (&gt;=) | 
| `BsonExpression` | In(`String` field, `BsonArray` value) | Returns all documents that has value in values list (IN) | 
| `BsonExpression` | In(`String` field, `BsonValue[]` values) | Returns all documents that has value in values list (IN) | 
| `BsonExpression` | In(`String` field, `IEnumerable<BsonValue>` values) | Returns all documents that has value in values list (IN) | 
| `BsonExpression` | LT(`String` field, `BsonValue` value) | Returns all documents that value are less than value (&lt;) | 
| `BsonExpression` | LTE(`String` field, `BsonValue` value) | Returns all documents that value are less than or equals value (&lt;=) | 
| `BsonExpression` | Not(`String` field, `BsonValue` value) | Returns all documents that are not equals to value (not equals) | 
| `BsonExpression` | Or(`BsonExpression` left, `BsonExpression` right) | Returns documents that exists in ANY queries results (Union). | 
| `BsonExpression` | Or(`BsonExpression[]` queries) | Returns documents that exists in ANY queries results (Union). | 
| `BsonExpression` | StartsWith(`String` field, `String` value) | Returns all documents that starts with value (LIKE) | 


