namespace LiteDB.Engine;

using System.Collections.Generic;

public partial class LiteEngine
{
    private IEnumerable<BsonDocument> SysCols()
    {
        foreach (var col in _header.GetCollections())
        {
            yield return new BsonDocument
            {
                ["name"] = col.Key,
                ["type"] = "user"
            };
        }

        foreach (var item in _systemCollections)
        {
            yield return new BsonDocument
            {
                ["name"] = item.Key,
                ["type"] = "system"
            };
        }
    }
}