namespace LiteDB.Tests;

internal class OrderDataSet
{
    public List<BsonDocument> Orders = new();
    public List<BsonDocument> Customers = new();

    public OrderDataSet(int size)
    {
        this.Customers = Enumerable.Range(1, size)
            .Select(x => new BsonDocument
            {
                ["_id"] = x,
                ["name"] = Faker.Fullname(),
                ["age"] = Faker.Age(),
                ["salary"] = (double)Faker.Next(5_000, 30_000),
                ["depto"] = Faker.Depto(),
                ["created"] = Faker.Created()
            })
            .ToList();

        this.Orders = Enumerable.Range(1, size)
            .Select(x => new BsonDocument
            {
                ["_id"] = x,
                ["created"] = x,
                ["customer"] = BsonDocument.DbRef(Faker.Next(1, size), "customers"),
            })
            .ToList();
    }
}
