using LiteDB.Tests.Utils;

using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace LiteDB.Tests.Issues;

/// <summary>
/// The issue here is that litedb does not support complex linq queries (``.Where(x => x.PhoneBooks.Any(x => ids.Contains(x.Id)))``)
/// </summary>
public class Issue2475_tests
{
    private class PhoneBookCategory
    {
        public Guid Id { get; set; }

        [BsonRef("PhoneBook")]
        public List<PhoneBook> PhoneBooks { get; set; }
    }

    private class PhoneBook
    {
        public Guid Id { get; set; }
    }

    [FailingFact]
    public void Supports_Complex_Queries()
    {
        List<Guid> ids = new() { Guid.NewGuid(), Guid.NewGuid() };

        using var db = new LiteDatabase(":memory:");
        var collection = db.GetCollection<PhoneBookCategory>("data");

        var set = collection.Query()
            .Where(x => x.PhoneBooks.Any(x => ids.Contains(x.Id)))
            .ToList();
    }
}