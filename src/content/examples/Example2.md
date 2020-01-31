---
title: 'Batch insert'
date: 2019-02-11T19:30:08+10:00
draft: false
weight: 2
---

Documents are stored and organized in collections. `LiteCollection` is a generic class to manage collections in LiteDB. Each collection must have a unique name:

- Contains only letters, numbers and `_`
- Collection names are **case insensitive**
- Collection names starting with `_` are reserved for internal use

The total size of all the collections names in a database is limited to 3000 bytes. If you plan to have many collections in your database, make sure to use short names for your collections. For example, if collection names are about 10 bytes in length, you can have ~300 collection in the database.

Collections are auto created on first `Insert` or `EnsureIndex` operation. Running a query, delete or update on a document in a non existing collection does not create one.

`LiteCollection<T>` is a generic class that can be used with `<T>` as `BsonDocument` for schema-less documents.  Internally LiteDB converts `T` to `BsonDocument` and all operations use the this generic document.

In this example, both code snippets produce the same results.

```C#
// Strong Type Class
using(var db = new LiteDatabase("mydb.db"))
{
    // Get collection instance
    var col = db.GetCollection<Customer>("customer");
    
    // Insert document to collection - if collection do not exists, create now
    col.Insert(new Customer { Id = 1, Name = "John Doe" });
    
    // Create, if not exists, new index on Name field
    col.EnsureIndex(x => x.Name);
    
    // Now, search for document your document
    var customer = col.FindOne(x => x.Name == "john doe");
}

// Generic BsonDocument
using(var db = new LiteDatabase("mydb.db"))
{
    // Get collection instance
    var col = db.GetCollection("customer");
    
    // Insert document to collection - if collection do not exists, create now
    col.Insert(new BsonDocument().Add("_id", 1).Add("Name", "John Doe"));
    
    // Create, if not exists, new index on Name field
    col.EnsureIndex("Name");
    
    // Now, search for document your document
    var customer = col.FindOne(Query.EQ("Name", "john doe"));
}
```
# LiteDatabase API Instance Methods

- **`GetCollection<T>`** - This method returns a new instance of `LiteCollection`. If `<T>` is omitted, `<T>` is `BsonDocument`. This is the only way to get a collection instance.
- **`RenameCollection`** - Rename a collection name only - do not change any document
- **`CollectionExists`** - Check if a collection already exists in database
- **`GetCollectionNames`** - Get all collections names in database
- **`DropCollection`** - Delete all documents, all indexes and the collection reference on database

### LiteCollection API Instance Methods

- **`Insert`** - Inserts a new document or an `IEnumerable` of documents. If your document has no `_id` field, Insert will create a new one using `ObjectId`. If you have a mapped object, `AutoId` can be used. See [Object Mapping](Object-Mapping)
- **`InsertBulk`** - Used for inserting a high volume of documents. Breaks documents into batches and controls transaction per batch. This method keeps memory usage low by cleaning a cache after each batch inserted.
- **`Update`** - Update one document identified by `_id` field. If not found, returns false
- **`Delete`** - Delete document by `_id` or by a `Query` result. If not found, returns false
- **`Find`** - Find documents using LiteDB queries. See [Query](Queries)
- **`EnsureIndex`** - Create a new index in a field. See [Indexes](Indexes)
- **`DropIndex`** - Drop an existing index
