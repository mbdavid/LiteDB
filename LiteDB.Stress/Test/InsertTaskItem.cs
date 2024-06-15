namespace LiteDB.Stress;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

public class InsertTaskItem : ITestItem
{
    private readonly Random _rnd = new Random();

    private ILiteCollection<BsonDocument> _collection;

    public string Name { get; }
    public int TaskCount { get; }
    public TimeSpan Sleep { get; }
    public string Collection { get; }
    public BsonAutoId AutoId { get; }
    public int MinRange { get; }
    public int MaxRange { get; }
    public List<InsertField> Fields { get; }

    public InsertTaskItem(XmlElement el)
    {
        Name = string.IsNullOrEmpty(el.GetAttribute("name"))
            ? "INSERT_" + el.GetAttribute("collection").ToUpper()
            : el.GetAttribute("name");
        Sleep = string.IsNullOrEmpty(el.GetAttribute("sleep"))
            ? TimeSpan.FromSeconds(1)
            : TimeSpanEx.Parse(el.GetAttribute("sleep"));
        AutoId = string.IsNullOrEmpty(el.GetAttribute("autoId"))
            ? BsonAutoId.ObjectId
            : (BsonAutoId) Enum.Parse(typeof(BsonAutoId), el.GetAttribute("autoId"), true);
        Collection = el.GetAttribute("collection");
        TaskCount = string.IsNullOrEmpty(el.GetAttribute("tasks")) ? 1 : int.Parse(el.GetAttribute("tasks"));
        MinRange = string.IsNullOrEmpty(el.GetAttribute("docs"))
            ? 1
            : int.Parse(el.GetAttribute("docs").Split('~').First());
        MaxRange = string.IsNullOrEmpty(el.GetAttribute("docs"))
            ? 1
            : int.Parse(el.GetAttribute("docs").Split('~').Last());

        Fields = new List<InsertField>();

        foreach (XmlElement child in el.SelectNodes("*"))
        {
            Fields.Add(new InsertField(child));
        }
    }

    public BsonValue Execute(LiteDatabase db)
    {
        IEnumerable<BsonDocument> source()
        {
            var count = _rnd.Next(MinRange, MaxRange);

            for (var i = 0; i < count; i++)
            {
                var doc = new BsonDocument();

                foreach (var field in Fields)
                {
                    doc[field.Name] = field.GetValue();
                }

                yield return doc;
            }
        }

        _collection ??= db.GetCollection(Collection, AutoId);

        return _collection.Insert(source());
    }
}