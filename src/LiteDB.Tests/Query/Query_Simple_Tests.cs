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
            JsonSerializer.Serialize(BsonArray.FromArray(resultSet)) ==
            JsonSerializer.Serialize(BsonArray.FromList(resultDB));



        await db.ShutdownAsync();

    }
}