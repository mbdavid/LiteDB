namespace LiteDB.Engine;

[AutoInterface]
internal class MasterMapper : IMasterMapper
{
    #region $master document structure

    /*
    # Master Document Structure
    {
        "collections": {
            "<col-name>": {
                "colID": 1,
                "meta": { ... },
                "indexes": [{
                        "slot": 0,
                        "expr": "$._id",
                        "unique": true,
                        "head": [8,0]
                        "tail": [8,1]
                        "meta": { ... }
                    }, { ... }],
                    //...
                }
            },
            //...
        },
        "pragmas": {
            "user_version": 0,
            "limit": 0,
            "checkpoint": 1000
        }
    }
    */

    #endregion

    public BsonDocument MapToDocument(MasterDocument master)
    {
        var doc = new BsonDocument
        {
            ["collections"] = new BsonDocument(master.Collections.ToDictionary(x => x.Key, x => (BsonValue)new BsonDocument
            {
                ["colID"] = x.Value.ColID,
                ["indexes"] = new BsonArray(x.Value.Indexes.Select(i => new BsonDocument
                {
                    ["slot"] = (int)i.Slot,
                    ["name"] = i.Name,
                    ["expr"] = i.Expression.ToString()!,
                    ["unique"] = i.Unique,
                    ["head"] = BsonArray.FromArray(new BsonValue[] { i.HeadIndexNodeID.PageID, i.HeadIndexNodeID.Index }),
                    ["tail"] = BsonArray.FromArray(new BsonValue[] { i.TailIndexNodeID.PageID, i.TailIndexNodeID.Index }),
                }))
            })),
            ["pragmas"] = new BsonDocument
            {
                ["user_version"] = master.Pragmas.UserVersion,
                ["limit_size"] = master.Pragmas.LimitSizeID,
                ["checkpoint"] = master.Pragmas.Checkpoint,
            }
        };

        return doc;
    }

    public MasterDocument MapToMaster(BsonDocument doc)
    {
        var master = new MasterDocument
        {
            Collections = doc["collections"].AsDocument.ToDictionary(c => c.Key, c => new CollectionDocument
            {
                ColID = (byte)c.Value["colID"],
                Name = c.Key,
                Indexes = c.Value["indexes"].AsArray.Select(i => new IndexDocument
                {
                    Slot = (byte)i["slot"],
                    Name = i["name"],
                    Expression = BsonExpression.Create(i["expr"]),
                    Unique = i["unique"],
                    HeadIndexNodeID = new((uint)i["head"][0], (byte)i["head"][1]),
                    TailIndexNodeID = new((uint)i["tail"][0], (byte)i["tail"][1])
                }).ToList()
            }),
            Pragmas = new PragmaDocument
            {
                UserVersion = doc["pragmas"]["user_version"],
                LimitSizeID = doc["pragmas"]["limit_size"],
                Checkpoint = doc["pragmas"]["checkpoint"]
            }
        };

        return master;
    }
}