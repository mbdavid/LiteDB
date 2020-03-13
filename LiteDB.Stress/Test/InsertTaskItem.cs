using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace LiteDB.Stress
{
    public class InsertTaskItem : ITestItem
    {
        private Random _rnd = new Random();

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
            this.Name = el["name"].Value;
            this.Sleep = TimeSpanEx.Parse(el["sleep"].Value);
            this.AutoId = (BsonAutoId)Enum.Parse(typeof(BsonAutoId), el["autoId"].Value);
            this.Collection = el["collation"].Value;
            this.TaskCount = int.Parse(el["tasks"].Value);
            this.MinRange = int.Parse(el["docs"].Value.Split('~').First());
            this.MaxRange = int.Parse(el["docs"].Value.Split('~').Last());

            this.Fields = new List<InsertField>();

            foreach (XmlElement child in el.SelectNodes("*"))
            {
                this.Fields.Add(new InsertField(child));
            }
        }

        public BsonValue Execute(LiteDatabase db)
        {
            IEnumerable<BsonDocument> source()
            {
                var count = _rnd.Next(this.MinRange, this.MaxRange);

                for(var i = 0; i < count; i++)
                {
                    var doc = new BsonDocument();

                    foreach(var field in this.Fields)
                    {
                        doc[field.Name] = field.GetValue();
                    }

                    yield return doc;
                }
            }

            _collection = _collection ?? db.GetCollection<BsonDocument>();

            return _collection.Insert(source());
        }
    }
}
