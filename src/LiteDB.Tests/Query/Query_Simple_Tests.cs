namespace Query;

public class Query_Simple_Tests
{

    async ValueTask Exec(Func<OrderSet, IEnumerable<BsonDocument>> docs, string query)
    {
        var dataset = new OrderSet(1_000);
        using var db = await TempDB.CreateOrderDBAsync(dataset);

        var resultSet = docs(dataset).ToArray();
        using var reader = await db.ExecuteReaderAsync(query);
        var resultDB = await reader.ToListAsync();

        var isEqual =
            JsonSerializer.Serialize(new BsonArray(resultSet)) ==
            JsonSerializer.Serialize(new BsonArray(resultDB));

        JsonSerializer.Serialize(new BsonArray(resultSet)).Should().BeEquivalentTo(JsonSerializer.Serialize(new BsonArray(resultDB)));


        await db.ShutdownAsync();
    }

    [Fact]
    public async void Query_Over_Belgium()
    {
        await Exec(
            d => from c in d.Customers
                 where c["country"] == "Belgium" && c["age"] > 35
                 orderby c["name"]
                 select new BsonDocument { ["_id"] = c["_id"], ["name"] = c["name"] },
            @"SELECT _id, name
                FROM customers
               WHERE country = 'Belgium' 
                 AND age > 35
               ORDER BY name");
    }

    [Fact]
    public async void Query_Not_English()
    {
        await Exec(
            d => from c in d.Customers
                 where c["lang"] != "English"
                 orderby c["name"]
                 select new BsonDocument { ["_id"] = c["_id"], ["name"] = c["name"], ["lang"] = c["lang"] },
            @"SELECT _id, name, lang
                FROM customers
               WHERE lang != 'English'
               ORDER BY name");
    }

    [Fact]
    public async void Query_RenamingWithAS()
    {
        await Exec(
            d => from c in d.Customers
                 where c["country"] != "Belgium"
                 orderby c["name"]
                 select new BsonDocument { ["SerialID"] = c["_id"], ["Fullname"] = c["name"], ["Nacionality"] = c["country"], ["Language"] = c["lang"] },
            @"SELECT _id AS SerialID, name AS Fullname, country AS Nacionality, lang AS Language
                FROM customers
               WHERE country != 'Belgium'
               ORDER BY name");
    }

    [Fact]
    public async void Query_SalaryHigherThan10_000()
    {
        await Exec(
            d => from c in d.Customers
                 where c["salary"] > 10_000
                 orderby c["name"]
                 select new BsonDocument { ["_id"] = c["_id"], ["name"] = c["name"], ["salary"] = c["salary"] },
            @"SELECT _id, name, salary
                FROM customers
               WHERE salary > 10000
               ORDER BY name");
    }

    [Fact]
    public async void Query_CreatedAfter2019()
    {
        await Exec(
            d => from c in d.Customers
                 where c["created"].AsDateTime.Year > 2019
                 orderby c["name"]
                 select new BsonDocument { ["_id"] = c["_id"], ["name"] = c["name"], ["created"] = c["created"] },
            @"SELECT _id, name, created
                FROM customers
               WHERE YEAR(created) > 2019
               ORDER BY name");
    }

    [Fact]
    public async void Query_DescendingNameOrder()
    {
        await Exec(
            d => from c in d.Customers
                 orderby c["name"] descending
                 select new BsonDocument { ["_id"] = c["_id"], ["name"] = c["name"] },
            @"SELECT _id, name
                FROM customers
               ORDER BY name DESC");
    }

    [Fact]
    public async void Query_OffSet10()
    {
        await Exec(
            d => from c in d.Customers.Skip(11)
                 orderby c["_id"]
                 select new BsonDocument { ["_id"] = c["_id"], ["name"] = c["name"] },
            @"SELECT _id, name
                FROM customers
               ORDER BY _id
                OFFSET 10");
    }

    [Fact]
    public async void Query_CountLangSpeakers()
    {
        await Exec(
            d => from c in d.Customers
                 orderby c["lang"]
                  group c by c["lang"] into L
                 select new BsonDocument { ["lang"] = L.Key, ["count"] = L.Count() },
            @"SELECT lang, COUNT($) AS count
                FROM customers
                GROUP BY lang");
    }

    [Fact]
    public async void Query_LanguagesWithMoreThan90Speakers()
    {
        await Exec(
            d => from c in d.Customers
                 orderby c["lang"]
                 group c by c["lang"] into L
                 where L.Count() > 90
                 select new BsonDocument { ["lang"] = L.Key, ["count"] = L.Count() },
            @"SELECT lang, COUNT($) AS count
                FROM customers
                GROUP BY lang
                HAVING count>90");
    }

}