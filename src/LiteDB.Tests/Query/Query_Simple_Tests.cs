using System.Linq;

namespace Query;

public class Query_Simple_Tests
{

    async ValueTask Exec(Func<OrderSet, IEnumerable<BsonDocument>> docs, string query, BsonValue args0 = null)
    {
        var dataset = new OrderSet(1_000);
        using var db = await TempDB.CreateOrderDBAsync(dataset);

        var resultSet = docs(dataset).ToArray();
        using var reader = await db.ExecuteReaderAsync(query, args0);
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

    [Fact]
    public async void Query_After2020Salary10000OrMoreFromBrazil()
    {
        await Exec(
            d => from c in d.Customers
                 orderby c["_id"]
                 where c["created"].AsDateTime.Year > 2020 && c["salary"] >= 10_000 && c["country"] == "Brazil"
                 select new BsonDocument { ["_id"] = c["_id"], ["name"] = c["name"], ["created"] = c["created"], ["salary"] = c["salary"], ["country"] = c["country"] },
            @"SELECT _id, name, created, salary, country
                FROM customers
                WHERE YEAR(created) > 2020 AND salary >= 10000 AND country = 'Brazil'
               ORDER BY _id");
    }

    [Fact]
    public async void Query_Orders()
    {
        await Exec(
            d => from c in d.Orders
                 orderby c["_id"]
                 where c["items"].AsArray.Count > 1
                 select new BsonDocument { ["_id"] = c["_id"], ["name"] = c["customer"].AsDocument["name"], ["job"] = c["customer"].AsDocument["job"], ["ItemsQnt"] = c["items"].AsArray.Count },
            @"SELECT { _id, name: customer.name, job: customer.job, ItemsQnt: COUNT(items) }
                FROM orders
                INCLUDE customer
                WHERE customer.job = 'Information Systems Manager' AND COUNT(items) > 1
                ORDER BY _id");
    }

    [Fact]
    public async void Query_AverageSalaryBelgium()
    {
        await Exec(
            d => from c in d.Customers
                      orderby c["country"]
                      group c by c["country"] into L
                      where L.Key == "Belgium"
                      select new BsonDocument { ["Country"] = L.Key, ["AverageSalary"] = L.Average(p => p["salary"].AsDouble) },
                @"SELECT country AS Country, AVG(salary) AS AverageSalary
                FROM customers
                WHERE salary>0
                GROUP BY country
                HAVING country = 'Belgium'");
    }

    [Fact]
    public async void Query_AverageSalaryBrazil2020()
    {
        await Exec(
                d => from c in d.Customers
                     orderby c["country"]
                     where c["created"].AsDateTime.Year == 2020
                     group c by c["country"] into L
                     where L.Key == "Brazil"
                     select new BsonDocument { ["Country"] = L.Key, ["AverageSalary"] = L.Average(p => p["salary"].AsDouble) }, 
                @"SELECT country AS Country, AVG(salary) AS AverageSalary
                FROM customers
                WHERE YEAR(created) = 2020 AND salary>0
                GROUP BY country
                HAVING country = 'Brazil'");
    }

    [Fact]
    public async void Query_MandarinSpeakerInBelgium()
    {
        await Exec(
                d => from c in d.Customers
                     orderby c["country"]
                     where c["lang"] == "Mandarin Chinese"
                     group c by c["country"] into L
                     where L.Key == "Belgium"
                     select new BsonDocument { ["Country"] = L.Key, ["MandarinSpeakers"] = L.Count() },
                @"SELECT country AS Country, COUNT($) AS MandarinSpeakers
                FROM customers
                WHERE lang = 'Mandarin Chinese'
                GROUP BY country
                HAVING country = 'Belgium'");
    }

    [Fact]
    public async void Query_TotalMandarinSpeaker()
    {
        await Exec(
                d => from c in d.Customers
                     orderby c["country"]
                     where c["lang"] == "Mandarin Chinese"
                     select new BsonDocument { ["MandarinSpeakers"] = d.Customers.Count() },
                @"SELECT COUNT($) AS MandarinSpeakers
                FROM customers
                WHERE lang = 'Mandarin Chinese'");
    }

    [Fact]
    public async void Query_MandarinSpeakerBelgiumOrAlbania2019()
    {
        await Exec(
                d => from c in d.Customers
                     orderby c["country"]
                     where c["created"].AsDateTime.Year == 2019
                     group c by c["country"] into L
                     where L.Key == "Belgium" || L.Key == "Albania"
                     select new BsonDocument { ["MandarinSpeakers"] = L.Count() },
                @"SELECT COUNT($) AS MandarinSpeakers
                FROM customers
                WHERE YEAR(created) = 2019 AND salary>0
                GROUP BY country
                HAVING country = 'Belgium' OR country = 'Albania'");
    }

    [Fact]
    public async void Query_GlobalSalaryAverage()
    {
        await Exec(
                d => from c in d.Customers
                     orderby c["country"]
                     where c["created"].AsDateTime.Year == 2019
                     group c by c["country"] into L
                     where L.Key == "Belgium" || L.Key == "Albania"
                     select new BsonDocument { ["MandarinSpeakers"] = L.Count() },
                @"SELECT AVG($.salary) as AvgSalary
                FROM customers
                WHERE salary>0");
    }

    [Fact]
    public async void Query_MostCommomPrenames()
    {
        await Exec(
                d => from c in d.Customers
                     orderby c["country"]
                     group c by c["name"].AsString.Split(' ').First() into L
                     orderby L.Count() descending
                     select new BsonDocument { ["Name"] = L.Key, ["Total"] = L.Count() },
                @"SELECT FIRST(SPLIT(name, ' ')) AS Name, COUNT($) as Total
                FROM customers
                GROUP BY FIRST(SPLIT(name, ' '))
                ORDER BY COUNT($) DESC");
    }

}