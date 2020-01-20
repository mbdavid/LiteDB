---
title: 'LiteDB - A .NET NoSQL Document Store in a single data file'
date: 2018-11-28T15:14:39+10:00
---

### Getting Started

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

    // Or you can query using new Query() syntax (filter, sort, transform)
    var results = customers.Query()
        .Where(x => x.Phones.Any(p => p.StartsWith("8000")))
        .OrderBy(x => x.Name)
        .Select(x => new { x.Id, x.Name })
        .Limit(10)
        .ToList();

    // Or using SQL
    var reader = db.Execute(
        @"SELECT _id, Name 
            FROM customers 
           WHERE Phones ANY LIKE '8000%'
           ORDER BY Name
           LIMIT 10");
}
```

### LiteDB Studio

LiteDB v5 project contains another way to connect, see and modify your database: `LiteDB.Studio`. It's a Windows GUI interface to simplify your data maniputation using `SQL` language. And support `Ctrl+Space` for code complete.

<center>
    ![LiteDB Stuio Screen 1](screen1.png)
</center>


### LiteDB features

- Serverless NoSQL Document Store
- Simple API, similar to MongoDB
- 100% C# code for .NET 4.5 / NETStandard 2.0 in a single DLL (less than 450kb)
- Multi thread (thread-safe)
- ACID with full transaction support (transaction per thread)
- Data recovery after write failure (WAL log file)
- Datafile encryption using DES (AES) cryptography
- Map your POCO classes to BsonDocument using attributes or fluent mapper API
- Store files and stream data (like GridFS in MongoDB)
- Single data file storage (like SQLite)
- Index document fields for fast search (up to 32 indexes per collection)
- LINQ support for queries
- SQL-Like commands to read/write data
    - Filter, sort, transform data
    - Map/Reduce using intuitive `GROUP BY` and `HAVING`
    - Insert, update or delete
    - Create indexes


### New features in v5

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
- New `EXPLAIN PLAN` to check how engine will run your query
- System collections to see transactions, cache, pages or any database information
- ... and much more ...

### Compatibility

This new version is not direct compatible with v4, but support full database upgrade and keeps almost the same API from v4.

# Meet LiteDB team

Meet who is working in this project to provide

<div class="team row">
    <div class="col-md-4 team-item">
        <img class="team-avatar" src="mbdavid.jpg">
        <p class="team-name">Maurício David</p>
        <p class="team-footer">CEO/Software Engineer</p>
    </div>
    <div class="col-md-4 team-item">
        <img class="team-avatar" src="mbdavid.jpg">
        <p class="team-name">Maurício David</p>
        <p class="team-footer">CEO/Software Engineer</p>
    </div>
    <div class="col-md-4 team-item">
        <img class="team-avatar" src="mbdavid.jpg">
        <p class="team-name">Maurício David</p>
        <p class="team-footer">CEO/Software Engineer</p>
    </div>
</div>

# Who is using LiteDB

Meet some companies that are using LiteDB in production:

<div class="cust row">
    <div class="cust-item col-md-4">
        <div class="cust-center">
            <img class="cust-logo" src="screen1.png">
            <div class="cust-title">Company A</div>
        </div>
        <div class="cust-text">Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer nec fermentum mi, vel placerat est. Phasellus venenatis leo eu commodo suscipit. Praesent maximus in est tincidunt pulvinar.</div>
        <div class="cust-footer">John Doe, CEO</div>
    </div>
    <div class="cust-item col-md-4">
        <div class="cust-center">
            <img class="cust-logo" src="screen1.png">
            <div class="cust-title">Company A</div>
        </div>
        <div class="cust-text">Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer nec fermentum mi, vel placerat est. Phasellus venenatis leo eu commodo suscipit. Praesent maximus in est tincidunt pulvinar.</div>
        <div class="cust-footer">John Doe, CEO</div>
    </div>
    <div class="cust-item col-md-4">
        <div class="cust-center">
            <img class="cust-logo" src="screen1.png">
            <div class="cust-title">Company A</div>
        </div>
        <div class="cust-text">Lorem ipsum dolor sit amet, consectetur adipiscing elit. Integer nec fermentum mi, vel placerat est. Phasellus venenatis leo eu commodo suscipit. Praesent maximus in est tincidunt pulvinar.</div>
        <div class="cust-footer">John Doe, CEO</div>
    </div>
</div>


# Register to LiteDB Community

Help LiteDB grow its user community by answering this simple survey.

# Mission

My mission is provide a great NoSQL database solution for .NET users with 
