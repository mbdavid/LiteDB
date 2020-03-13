using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace LiteDB.Stress
{
    public enum InsertFieldType { Name, Int, Guid, Bool, Now, Date, Json }

    public class InsertField
    {
        private Random _rnd = new Random();

        public string Name { get; }
        public InsertFieldType Type { get; }
        public int StartIntRange { get; }
        public int EndIntRange { get; }
        public DateTime StartDateRange { get; }
        public int DaysDateRange { get; }
        public BsonValue Value { get; }

        public InsertField(XmlElement el)
        {
            this.Name = el.Name;
            this.Type = el["type"].Value.ToLower() switch
            {
                "name" => InsertFieldType.Name,
                "int" => InsertFieldType.Int,
                "guid" => InsertFieldType.Guid,
                "bool" => InsertFieldType.Bool,
                "now" => InsertFieldType.Now,
                "date" => InsertFieldType.Date,
                _ => InsertFieldType.Json
            };

            if (this.Type == InsertFieldType.Int)
            {
                this.StartIntRange = int.Parse(el["range"].Value.Split('~').First());
                this.EndIntRange = int.Parse(el["range"].Value.Split('~').Last());
            }
            else if (this.Type == InsertFieldType.Date)
            {
                this.StartDateRange = DateTime.Parse(el["range"].Value.Split('~').First());
                var endDateRange = DateTime.Parse(el["range"].Value.Split('~').Last());
                this.DaysDateRange = (int)endDateRange.Subtract(this.StartDateRange).TotalDays;
            }

            this.Value = this.Type == InsertFieldType.Json ?
                JsonSerializer.Deserialize(el.InnerText) :
                null;
        }

        public BsonValue GetValue()
        {
            switch(this.Type)
            {
                case InsertFieldType.Name: return "";
                case InsertFieldType.Int: return _rnd.Next(this.StartIntRange, this.EndIntRange);
                case InsertFieldType.Guid: return Guid.NewGuid();
                case InsertFieldType.Bool: return _rnd.NextDouble() > .5;
                case InsertFieldType.Now: return DateTime.Now;
                case InsertFieldType.Date: return this.StartDateRange.AddDays(_rnd.Next(this.DaysDateRange));
                default: return this.Value;
            }
        }
    }
}
