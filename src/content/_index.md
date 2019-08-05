---
title: 'LiteDB - A .NET NoSQL Document Store in a single data file'
date: 2018-11-28T15:14:39+10:00
---

### LiteDB Studio

LiteDB v5 project contains another way to connect, see and modify your database: `LiteDB.Studio`. It's a Windows GUI interface to simplify your data maniputation using `SQL` language. And support `Ctrl+Space` for code complete.

<center>
    ![LiteDB Stuio Screen 1](screen1.png)
</center>

### SQL Syntax

Access and modity your database using LINQ or SQL query language. LiteDB support a SQL-Like language to filter, group, order and transform you data, as simples as any SQL language.

```SQL
-- Insert some data
INSERT INTO customers
     VALUES { _id: 1, name: 'John', age: 41 },
            { _id: 2, name: 'Carlos', age: 29 };

-- Or update
UPDATE customers
   SET age = age + LENGTH(name)
 WHERE _id > 0;

-- Do simple queries
SELECT _id, name, age
  FROM customers
 WHERE name LIKE 'J%'
 ORDER BY age;

 -- Or group by queries
SELECT @key AS age, COUNT(*) AS total, MAX(*.Name) AS maxName
  FROM customers
 GROUP BY age;
```

Also, SQL support many others commands, like

- `INCLUDE`, `INTO`, `HAVING`, `LIMIT`, `OFFSET`
- `DELETE`, `DELETE COLLECTION`, `DELETE INDEX`
- `CREATE INDEX`

### Example

Take a look how simple is insert and query data in LiteDB.

```C#
// Basic example
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string[] Phones { get; set; }
    public bool IsActive { get; set; }
}

// Open database (or create if not exits)
using(var db = new LiteDatabase(@"MyData.db"))
{
    // Get customer collection
    var customers = db.GetCollection<Customer>("customers");

    // Create your new customer instance
    var customer = new Customer
    { 
        Name = "John Doe", 
        Phones = new string[] { "8000-0000", "9000-0000" }, 
        IsActive = true
    };

    // Insert new customer document (Id will be auto-incremented)
    customers.Insert(customer);

    // Update a document inside a collection
    customer.Name = "Joana Doe";

    customers.Update(customer);

    // Index document using a document property
    customers.EnsureIndex(x => x.Name);

    // Now, let's create a simple query
    var results = customers.Find(x => x.Name.StartsWith("Jo"));

    // Or you can query using new Query() syntax
    var results = customers.Query()
        .Where(x => x.Phones.Any(p => p.StartsWith("8000")))
        .Limit(10)
        .Select(x => new { x.Id, x.Name })
        .ToList();

    // Or using SQL
    var reader = db.Execute(
        @"SELECT _id, Name 
            FROM customers 
           WHERE Phones ANY LIKE @0
           ORDER BY Name", 
        "8000%");
}
```

### New features in v5

New v5 was a complete engine rewrite on which I spend more than a year of development.

- New WAL (Write-Ahead Logging) for fast durability
- MultiVersion Concurrency Control (Snapshots & Checkpoint)
- Database lock per collection
- Multi concurrent `Stream` readers - with no locks
- Single async writer thread
- New cache system - virtual memory mapped file 
- Up to 32 indexes per collection
- Atomic multi-document/multi-collection transactions
- System collections
- Import/Export to CSV/JSON in SQL
- New BsonExpression parser with complete expression support
- New LINQ mapper converting to BsonExpression
- ... and much more ...

### Compatibility

This new version is not direct compatible with v4, but support full database upgrade and keeps almost the same API from v4.