---
title: 'Indexes'
date: 2019-02-11T19:30:08+10:00
draft: false
weight: 1
---

LiteDB improves search performance by using indexes on document fields. Each index stores the value of a specific field ordered by the value (and type) of the field. Without an index, LiteDB must execute a query using a full document scan. Full document scans are inefficient because LiteDB must deserialize all documents to test each one by one.

### Index Implementation

LiteDB uses a simple index solution: **Skip Lists**. Skip lists are double linked sorted list with up to 32 levels. Skip lists are super easy to implement (only 15 lines of code) and statistically balanced. The results are great: insert and find results has an average of O(ln n) = 1 million of documents = 13 steps. If you want to know more about skip lists, see [this great video](https://www.youtube.com/watch?v=kBwUoWpeH_Q). 

Documents are schema-less, even if they are in the same collection. So, you can create an index on a field that can be one type in one document and another type in another document. When you have a field with different types, LiteDB compares only the types. Each type has an order:

|BSON Type                     |Order|
|------------------------------|-----|
|MinValue                      |1    |
|Null                          |2    |
|Int32, Int64, Double, Decimal |3    |
|String                        |4    |
|Document                      |5    |
|Array                         |6    |
|Binary                        |7    |
|ObjectId                      |8    |
|Guid                          |9    |
|Boolean                       |10   |
|DateTime                      |11   |
|MaxValue                      |12   |

- Numbers (Int32, Int64, Double or Decimal) have the same order. If you mix these number types in the same document field, LiteDB will convert them to `Decimal` when comparing.

### Primary key (= auto id) 

Primary key is also one of the indexes. By default primary key `_id` will be created and inserted automatically when you call method `col.Insert()` . 
However it won't be created if Custom types are used in Id property.

### EnsureIndex()

Indexes are created via `EnsureIndex`. This instance method ensures an index: create the index if it does not exist or do nothing if already exists. In v4 there is no more re-create index in change definition. If you want re-create an index you must drop before and runs `EnsureIndex` again.

Indexes are identified by document field name. LiteDB only supports 1 field per index, but this field can be any BSON type, even an embedded document.

```JS
{
    _id: 1,
    Address:
    {
        Street: "Av. Protasio Alves, 1331",
        City: "Porto Alegre",
        Country: "Brazil"
    }
}
```

- You can use `EnsureIndex("Address")` to create an index to all `Address` embedded document
- Or `EnsureIndex("Address.Street")` to create an index on `Street` using dotted notation
- Indexes are executed as `BsonDocument` fields. If you are using a custom `ResolvePropertyName` or `[BsonField]` attribute, you must use your document field name and not the property name. See [Object Mapping](Object-Mapping).
- You can use a lambda expression to define an index field in a strongly typed collection: `EnsureIndex(x => x.Name)`

### MultiKey Index

When you create an index in a array type field, all values are included on index keys and you can search for any value.

```C#
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string[] Phones { get; set; }
}

var customers = db.GetCollection<Customer>("customers");

customers.Insert(new Customer { Name = "John", Phones = new string[] { "1", "2", "5" });
customers.Insert(new Customer { Name = "Doe", Phones = new string[] { "1", "8" });

customers.EnsureIndex(x => x.Phones, "$.Phones[*]");

var result = customers.Query(x => x.Phones.Contains("1")); // returns both documents
```

### Expressions

In v4 it's possible to create an index based on a expression execution with multikey values support. With this, you can index any king information that are not direct you field value, like:

- `db.EnsureIndex("customer", "Name", false, "LOWER($.Name)")`
- `db.EnsureIndex("customer", "Total", false, "SUM($.Items[*].Price)")`
- `db.EnsureIndex("customer", "CheapBooks", false, "LOWER($.Books[@.Price < 20].Title)")`

See [Expressions](Expressions) for more details about expressions.

### Changes in v4

- There is no more auto index creation. You always run `EnsureIndex` in your database initialization.
- If you try query without an index, query will be runned using full search
- If you are using a LINQ expression with no resolution to `Query` object, query engine will run query after map to object
- When your query has an `And` operation, engine will run only 1 side with index (if exist) and another side will use full scan. This optimize results avoiding multi index queries. Try always use left

```C#
col.EnsureIndex(x => x.Name);
col.EnsureIndex(x= > x.Age);

var r = col.Find(x => x.Name == "John" && x.Age > 20 && x.Phones.Length > 1);
```

In this example, LiteDB will use `Name` index to get first results. For `Age > 20` full scan will be used over all documents which `Name == 'John'`. And them, over result of this, query `Phones.Length > 1`

###  Limitations

- Index values must have less than 512 bytes (after BSON serialization)
- Max of 16 indexes per collections - including the `_id` primary key