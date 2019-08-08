---
title: 'DbRef'
date: 2019-02-11T19:30:08+10:00
draft: false
weight: 2
---

LiteDB is a document database, so there is no JOIN between collections. You can use embedded documents (sub-documents) or create a reference between collections. To create this reference you can use `[BsonRef]` attribute or use `DbRef` method from fluent API mapper.

### Mapping a reference on database initialization

```C#
public class Customer
{
    public int CustomerId { get; set; }
    public string Name { get; set; }
}

public class Order
{
    public int OrderId { get; set; }
    public Customer Customer { get; set; }
}
```

If you didn't do any mapping, when you save an `Order`, `Customer` are saved as an embedded document (with no link to any other collection). If you change customer name in `Customer` collection this change will not affect `Order`.

```JS
Order => { _id: 123, Customer: { CustomerId: 99, Name: "John Doe" } }
```

If you want store only customer reference in `Order`, you can decorate your class:

```C#
public class Order
{
    public int OrderId { get; set; }

    [BsonRef("customers")] // where "customers" are Customer collection name
    public Customer Customer { get; set; }
}
```

Note that `BsonRef` decorates the full object being referenced, not an int `customerid` field that references an object in the other collection.

Or use fluent API:

```C#
BsonMapper.Global.Entity<Order>()
    .DbRef(x => x.Customer, "customers"); // where "customers" are Customer collection name
```

**Note:** `Customer` needs to have a `[BsonId]` defined.

Now, when you store `Order` you are storing only link reference.

```JS
Order => { _id: 123, Customer: { $id: 4, $ref: "customers"} }
```

### Querying results

When you query a document with a cross collection reference, you can auto load references using `Include` method before query. 

```C#
var orders = db.GetCollection<Order>("orders");

var order1 = orders
    .Include(x => x.Customer)
    .FindById(1);
```

DbRef also support `List<T>` or `Array`, like:

```C#
public class Product
{
    public int ProductId { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
}

public class Order
{
    public int OrderId { get; set; }
    public DateTime OrderDate { get; set; }
    public List<Product> Products { get; set; }
}

BsonMapper.Global.Entity<Order>()
    .DbRef(x => x.Products, "products");
```

LiteDB will respect if your `Products` field are null or empty list when restore from datafile. If you do not use `Include` in query, classes are loaded with only `ID` set (all other properties will stay with default/null value).

In v4, this include process occurs on BsonDocument engine level. That also support any level of include, just using `Path` syntax:

```C#
orders.Include(new string[] { "$.Customer", "$.Products[*]" });
```

If you are using in `LiteCollection` or `Repository` you can use Linq syntax too:

```C#
// repository fluent syntax
db.Query<Order>()
    .Include(x => x.Customer)
    .Include(x => x.Products)
    .ToList();
```

