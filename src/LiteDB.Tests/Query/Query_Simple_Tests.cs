namespace Query;

public class Query_Simple_Tests
{
    [Fact]
    public async void Query_Over_Belgium()
    {
        var dataset = new OrderSet(1_000);
        using var db = await TempDB.CreateOrderDBAsync(dataset);

        var resultSet = dataset.Customers
            .Where(x => x["country"] == "Belgium" && x["age"] > 35)
            .Select(x => new BsonDocument { ["_id"] = x["_id"], ["name"] = x["name"] })
            .OrderBy(x => x["name"])
            .ToArray();

        var resultDB = await (await db.ExecuteReaderAsync(
            @"SELECT _id, name
                FROM customers
               WHERE country = 'Belgium' 
                 AND age > @0
               ORDER BY name", 35))
            .ToListAsync();

        var isEqual =
            JsonSerializer.Serialize(new BsonArray(resultSet)) ==
            JsonSerializer.Serialize(new BsonArray(resultDB));

        await db.ShutdownAsync();

        isEqual.Should().BeTrue();

    }


    [Fact]
    public async void Query_Not_English()
    {
        var dataset = new OrderSet(1_000);
        using var db = await TempDB.CreateOrderDBAsync(dataset);

        var resultSet = dataset.Customers
            .Where(x => x["lang"] != "English")
            .Select(x => new BsonDocument { ["_id"] = x["_id"], ["name"] = x["name"], ["lang"] = x["lang"] })
            .OrderBy(x => x["name"])
            .ToArray();

        var resultDB = await (await db.ExecuteReaderAsync(
            @"SELECT _id, name, lang
                FROM customers
               WHERE lang != 'English'
               ORDER BY name"))
            .ToListAsync();

        var isEqual =
            JsonSerializer.Serialize(new BsonArray(resultSet)) ==
            JsonSerializer.Serialize(new BsonArray(resultDB));

        await db.ShutdownAsync();

        isEqual.Should().BeTrue();

    }

    [Fact]
    public async void Query_RenamingWithAS()
    {
        var dataset = new OrderSet(1_000);
        using var db = await TempDB.CreateOrderDBAsync(dataset);

        var resultSet = dataset.Customers
            .Where(x => x["country"] != "Belgium")
            .Select(x => new BsonDocument { ["SerialID"] = x["_id"], ["Fullname"] = x["name"], ["Nacionality"] = x["country"], ["Language"] = x["lang"] })
            .OrderBy(x => x["Fullname"])
            .ToArray();

        var resultDB = await (await db.ExecuteReaderAsync(
            @"SELECT _id AS SerialID, name AS Fullname, country AS Nacionality, lang AS Language
                FROM customers
               WHERE country != 'Belgium'
               ORDER BY name"))
            .ToListAsync();

        var isEqual =
            JsonSerializer.Serialize(new BsonArray(resultSet)) ==
            JsonSerializer.Serialize(new BsonArray(resultDB));

        await db.ShutdownAsync();

        isEqual.Should().BeTrue();

    }

    [Fact]
    public async void Query_SalaryHigherThan10_000()
    {
        var dataset = new OrderSet(1_000);
        using var db = await TempDB.CreateOrderDBAsync(dataset);

        var resultSet = dataset.Customers
            .Where(x => x["salary"] > 10_000)
            .Select(x => new BsonDocument { ["_id"] = x["_id"], ["name"] = x["name"], ["salary"] = x["salary"]})
            .OrderBy(x => x["name"])
            .ToArray();

        var resultDB = await (await db.ExecuteReaderAsync(
            @"SELECT _id, name, salary
                FROM customers
               WHERE salary > 10000
               ORDER BY name"))
            .ToListAsync();

        var isEqual =
            JsonSerializer.Serialize(new BsonArray(resultSet)) ==
            JsonSerializer.Serialize(new BsonArray(resultDB));

        await db.ShutdownAsync();

        isEqual.Should().BeTrue();

    }

    [Fact]
    public async void Query_CreatedAfter2019()
    {
        var dataset = new OrderSet(1_000);
        using var db = await TempDB.CreateOrderDBAsync(dataset);

        var resultSet = dataset.Customers
            .Where(x => x["created"].AsDateTime.Year > 2019)
            .Select(x => new BsonDocument { ["_id"] = x["_id"], ["name"] = x["name"], ["created"] = x["created"] })
            .OrderBy(x => x["name"])
            .ToArray();

        var resultDB = await (await db.ExecuteReaderAsync(
            @"SELECT _id, name, created
                FROM customers
               WHERE YEAR(created) > 2019
               ORDER BY name"))
            .ToListAsync();

        var isEqual =
            JsonSerializer.Serialize(new BsonArray(resultSet)) ==
            JsonSerializer.Serialize(new BsonArray(resultDB));

        await db.ShutdownAsync();

        isEqual.Should().BeTrue();

    }

    [Fact]
    public async void Query_DescendingNameOrder()
    {
        var dataset = new OrderSet(1_000);
        using var db = await TempDB.CreateOrderDBAsync(dataset);

        var resultSet = dataset.Customers
            .Select(x => new BsonDocument { ["_id"] = x["_id"], ["name"] = x["name"] })
            .OrderByDescending(x => x["name"])
            .ToArray();

        var resultDB = await (await db.ExecuteReaderAsync(
            @"SELECT _id, name
                FROM customers
               ORDER BY name DESC"))
            .ToListAsync();

        var isEqual =
            JsonSerializer.Serialize(new BsonArray(resultSet)) ==
            JsonSerializer.Serialize(new BsonArray(resultDB));

        await db.ShutdownAsync();

        isEqual.Should().BeTrue();

    }

    [Fact]
    public async void Query_OffSet10()
    {
        var dataset = new OrderSet(1_000);
        using var db = await TempDB.CreateOrderDBAsync(dataset);

        var resultSet = dataset.Customers
            .Select(x => new BsonDocument { ["_id"] = x["_id"], ["name"] = x["name"] })
            .OrderBy(x => x["_id"])
            .Skip(11)
            .ToArray();

        var resultDB = await (await db.ExecuteReaderAsync(
            @"SELECT _id, name
                FROM customers
               ORDER BY _id
                OFFSET 10"))
            .ToListAsync();

        var isEqual =
            JsonSerializer.Serialize(new BsonArray(resultSet)) ==
            JsonSerializer.Serialize(new BsonArray(resultDB));

        await db.ShutdownAsync();

        isEqual.Should().BeTrue();

    }


}