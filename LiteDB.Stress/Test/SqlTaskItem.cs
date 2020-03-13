using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace LiteDB.Stress
{
    public class SqlTaskItem : ITestItem
    {
        public string Name { get; }
        public TimeSpan Sleep { get; }
        public string Sql { get; }

        public SqlTaskItem(XmlElement el)
        {
            this.Name = el["name"]?.Value ?? el.InnerText.Split(' ').First();
            this.Sleep = TimeSpanEx.Parse(el["sleep"].Value);
            this.Sql = el.InnerText;
        }

        public BsonValue Execute(LiteDatabase db)
        {
            using (var reader = db.Execute(this.Sql))
            {
                return reader.FirstOrDefault();
            }
        }
    }
}
