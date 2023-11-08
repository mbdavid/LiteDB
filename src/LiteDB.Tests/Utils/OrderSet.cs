internal class OrderSet
{
    public List<BsonDocument> Orders = new();
    public List<BsonDocument> Customers = new();

    public OrderSet(int size)
    {
        this.Customers = Enumerable.Range(1, size)
            .Select(x => new BsonDocument
            {
                ["_id"] = x,
                ["name"] = Faker.Fullname(),
                ["age"] = Faker.Age(),
                ["salary"] = Faker.NextDouble(5_000, 30_000),
                ["job"] = Faker.Job(),
                ["country"] = Faker.Country(),
                ["lang"] = Faker.Language(),
                ["depto"] = Faker.Departments(),
                ["created"] = Faker.Created()
            })
            .ToList();

        this.Orders = Enumerable.Range(1, size)
            .Select(x => new BsonDocument
            {
                ["_id"] = x,
                ["created"] = Faker.Created(),
                ["customer"] = BsonDocument.DbRef(Faker.Next(1, size), "customers"),
                ["items"] = new BsonArray(Enumerable.Range(1, Faker.Next(1, 15))
                    .Select(i => new BsonDocument
                    {
                        ["seq"] = i,
                        ["sku"] = Faker.SkuNumber(),
                        ["name"] = Guid.NewGuid().ToString(),
                        ["qty"] = Faker.Next(1, 4),
                        ["price"] = Faker.NextDouble(50, 400)
                    }))
            })
            .ToList();
    }
}
