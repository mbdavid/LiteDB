---
title: 'Expressions'
date: 2019-02-11T19:30:08+10:00
draft: false
weight: 1
---

Expressions are path or formulas to access and modify your document data. Based on JSON path article (http://goessner.net/articles/JsonPath/), LiteDB support a near syntax to navigate in a single document. Path always returns an `IEnumerable<BsonValue>` in any case.

`BsonExpression` are the class that parse a string expression (or path) and compile into a LINQ Expression to be fast evaluate by LiteDB. Parser uses

- Path starts with `$`: `$.Address.Street`
- Int values starts with `[0-9]*`: `123`
- Double values starts with `[0-9].[0-9]`: `123.45`
- Strings are represented with a single quote `'`: `'Hello World'`
- Null are just `null`
- Bool are represented using `true` or `false` keyword.
- Document starts with `{ key1: <value|expression>, key2: ... }`
- Arrays are represented with `[values, expressions, paths, ...]`
- Functions are represented with `FUNCTION_NAME(par1, par2, ...)`: `LOWER($.Name)`

Examples:
- `$.Price`
- `$.Price + 100`
- `SUM($.Items[*].Price)`

```C#
var expr = new BsonExpression("SUM($.Items[*].Unity * $.Items[*].Price)");
var total = expr.Execute(doc, true).First().AsDecimal;
```

Expressions also can be used in:

- Create an index based on an expression:
    - `collection.EnsureIndex("Name", true, "LOWER($.Name)")`
    - `collection.EnsureIndex(x => x.Name, true, "LOWER($.Name)")`
- Query documents inside a collection based on expression (full scan search)
    - `Query.EQ("SUBSTRING($.Name, 0, 1)", "T")`
- Update shell command
    - `db.customers.update $.Name = LOWER($.Name) where _id = 1`
- Create new document result in SELECT shell command
    - `db.customers.select { upper_titles: ARRAY(UPPER($.Books[*].Title)) } where $.Name startswith "John"`

# Path 

- `$` - Root
- `$.Name` - Name field
- `$.Name.First` - First field from a Name subdocument  
- `$.Books` - Returns book array value 
- `$.Books[0]` - Return first book inside books array
- `$.Books[*]` - Return all books inside books array
- `$.Books[*].Title` Return titles from all books
- `$.Books[-1]` - Return last book inside books array

Path also support expression to filter child node

- `$.Books[@.Title = 'John Doe']` - Return all books where title is John Doe
- `$.Books[@.Price > 100].Title` - Return all titles where book price are greater than 100

Inside array, `@` represent current sub document. It's possible use functions inside expressions too:

- `$.Books[SUBSTRING(LOWER(@.Title), 0, 1) = 'j']` - Return all books with title starts with T or t.

# Functions

Functions are always works in `IEnumerable<BsonValue>` as input/output parameters.

- `LOWER($.Name)` - Returns `IEnumerable` with a single element
- `LOWER($.Books[*].Title)` - Returns `IEnumerable` with all values in lower case

## Operators

Operators are function to implement same math syntax. Support

- `ADD(<left>, <right>)`: `<left> + <right>` (If any side are string, concat values and return as string.)
- `MINUS(<left>, <right>)`: `<left> - <right>`
- `MULTIPLY(<left>, <right>)`: `<left> * <right>`
- `DIVIDE(<left>, <right>)`: `<left> / <right>`
- `MOD(<left>, <right>)`: `<left> % <right>`
- `EQ(<left>, <right>)`: `<left> = <right>`
- `NEQ(<left>, <right>)`: `<left> != <right>`
- `GT(<left>, <right>)`: `<left> > <right>`
- `GTE(<left>, <right>)`: `<left> >= <right>`
- `LT(<left>, <right>)`: `<left> < <right>`
- `LTE(<left>, <right>)`: `<left> <= <right>`
- `AND(<left>, <right>)`: `<left> && <right>` (Left and right must be a boolean)
- `OR(<left>, <right>)`: `<left> || <right>`: Left and right must be a boolean)

### Examples

- `db.dummy.select ((2 + 11 % 7)-2)/3` => `1.33333`
- `db.dummy.select 'My Id is ' + $._id` => `"My Id is 1"`
- `db.customers.select { info: IIF($._id < 20, 'Less', 'Greater') + ' than twenty' }`
    - => `{ info: "Less than twenty" }`
## String

String function will work only if your `<values>` are string. Any other data type will skip results

| Function | Description
| --- | ---
| `LOWER(<values>)` | Same `ToLower()` from `String`. Returns a string
| `UPPER(<values>)` | Same `ToUpper()` from `String`. Returns a string
| `SUBSTRING(<values>, <index>, <length>)` | Same `Substring()` from `String`. Returns a string
| `LPAD(<values>, <totalWidth>, <paddingChar>)` | Same `PadLeft()` from `String`. Returns a string
| `RPAD(<values>, <totalWidth>, <paddingChar>)` | Same `PadRight()` from `String`. Returns a string
| `FORMAT(<values>, <format>)` | Same `Format()` from `String`. Works if any data type (use `RawValue`). Returns a string

## Aggregates

| Function | Description
| --- | ---
| `SUM(<values>)` | Sum all number values and returns a number
| `COUNT(<values>)` | Count all values and returns a integer
| `MIN(<values>)` | Get min value in value list
| `MAX(<values>)` | Get max value in value list
| `AVG(<values>)` | Calculate average in list of values
| `FIRST(<values>)` | Get first value from list of values
| `LAST(<values>)` | Get last value from list of values
| `ARRAY(<values>)` | Convert all values into a single array

All aggregates functions works in a single document and always with an IEnumerable selector of an array: (`SUM($.Items[*].Price)`)

## DataType

| Function | Description
| --- | ---
| `EXTEND(<src>, <extend>)` | Copy, into source document, all field from extend document `EXTEND({a:1}, {b:2})` = `{a:1, b:2}`
| `JSON(<strings>)` | Deserialize string value into a `BsonDocument` value. `JSON('{a:1}')` = `{a:1}`
| `IS_MINVALUE(<values>)` | Return true for each value that are minvalue
| `IS_NULL(<values>)` | Return true for each value that are null
| `IS_INT32(<values>)` or `IS_INT(<values>)` | Return true for each value that are int
| `IS_INT64(<values>)` or `IS_LONG(<values>)`| Return true for each value that are long
| `IS_DOUBLE(<values>)` | Return true for each value that are double
| `IS_DECIMAL(<values>)` | Return true for each value that are decimal
| `IS_NUMBER(<values>)` | Return true for each value that are number
| `IS_STRING(<values>)` | Return true for each value that are string
| `IS_ARRAY(<values>)` | Return true for each value that are array
| `IS_BINARY(<values>)` | Return true for each value that are binary
| `IS_OBJECTID(<values>)` | Return true for each value that are objectid
| `IS_GUID(<values>)` | Return true for each value that are guid
| `IS_BOOLEAN(<values>)` or `IS_BOOL(<values>)` | Return true for each value that are boolean
| `IS_DATETIME(<values>)` or `IS_DATE(<values>)` | Return true for each value that are datetime
| `IS_MAXVALUE(<values>)` | Return true for each value that are maxvalue
| `MINVALUE()` | Return new min value
| `MAXVALUE()` | Return new max value
| `INT32(<values>)` or `INT(<values>)` | Convert all possible values into int
| `INT64(<values>)` or `LONG(<values>)` | Convert all possible values into long
| `DOUBLE(<values>)` | Convert all possible values into double
| `DECIMAL(<values>)` | Convert all possible values into decimal
| `STRING(<values>)` | Convert each value as string
| `OBJECTID()` | Return new objectid
| `OBJECTID(<values>)` | Convert all possible values (string) into objectid
| `GUID()` | Return new guid
| `GUID(<values>)` | Convert all possible values (string) into guid
| `DATETIME()` or `DATE()`  | Return new date (now)
| `DATETIME(<values>)` or `DATE(<values>)`  | Convert all possible values (string) into datetime
| `DATETIME(<year>, <month>, <day>)` or `DATE(<year>, <month>, <day>)`  | Create new date based on year, month, day (int) passed


## Date

| Function | Description |
| --- | --- |
| `YEAR(<date>)` | Return year (as int) from date
| `MONTH(<date>)` | Return month(as int) from date
| `DAY(<date>)` | Return day (as int) from date
| `HOUR(<date>)` | Return hour (as int) from date
| `MINUTE(<date>)` | Return minute (as int) from date
| `SECOND(<date>)` | Return second (as int) from date
| `DATEADD(<datePart>, <number>, <date>)` | Return new date adding internal from date
| `DATEDIFF(<datePart>, <dateStart>, <dateEnd>)` | Return interval between start and end date

### DateParts

Date parts are strings used in `DATEADD` and `DATEDIFF`

- `y` or `year` = Year
- `M` or `month` = Month
- `d` or `day` = Day
- `h` or `hour` = Hour
- `m` or `minute` = Minute
- `s` or `second` = Second

`db.orders.select { in_days: DATEDIFF('day', $.DueDate, DATE()) }`

## Misc

| Function | Description
| --- | ---
| `KEYS(<docs>)` | Returns all keys from all documents. `KEYS({a:1, b:2})` = `a`, `b`
| `IIF(<condition>, <ifTrue>, <ifFalse>)` | Condition must be a boolean value
| `LENGTH(<values>)`| Returns value length (String.Length or Array.Length or ByteArray.Length or Document.Keys.Length)

All function are static methods from `BsonExpression`.
