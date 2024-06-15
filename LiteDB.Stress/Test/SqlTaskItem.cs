namespace LiteDB.Stress;

using System;
using System.Linq;
using System.Xml;

public class SqlTaskItem : ITestItem
{
    public string Name { get; }
    public int TaskCount { get; }
    public TimeSpan Sleep { get; }
    public string Sql { get; }

    public SqlTaskItem(XmlElement el)
    {
        Name = string.IsNullOrEmpty(el.GetAttribute("name"))
            ? el.InnerText.Split(' ').First()
            : el.GetAttribute("name");
        TaskCount = string.IsNullOrEmpty(el.GetAttribute("tasks")) ? 1 : int.Parse(el.GetAttribute("tasks"));
        Sleep = TimeSpanEx.Parse(el.GetAttribute("sleep"));
        Sql = el.InnerText;
    }

    public BsonValue Execute(LiteDatabase db)
    {
        using (var reader = db.Execute(Sql))
        {
            return reader.FirstOrDefault();
        }
    }
}