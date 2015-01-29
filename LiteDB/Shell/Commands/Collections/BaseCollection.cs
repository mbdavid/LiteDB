using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    public class BaseCollection
    {
        /// <summary>
        /// Read collection name from db.(colname).(command)
        /// </summary>
        public Collection<BsonDocument> ReadCollection(LiteEngine db, StringScanner s)
        {
            return db.GetCollection(s.Scan(@"db\.(\w+)\.\w+\s*", 1));
        }

        public bool IsCollectionCommand(StringScanner s, string command)
        {
            return s.Match(@"db\.\w+\." + command);
        }

        public int ReadTop(StringScanner s)
        {
            if (s.Match(@"top\s+\d+\s*"))
            {
                return Convert.ToInt32(s.Scan(@"top\s+(\d+)\s*", 1));
            }

            return int.MaxValue;
        }

        public Query ReadQuery(StringScanner s)
        {
            if (s.HasTerminated)
            {
                return Query.All();
            }
            else if(s.Scan(@"\(").Length > 0)
            {
                return this.ReadInlineQuery(s);
            }
            else
            {
                return this.ReadOneQuery(s);
            }
        }

        private Query ReadInlineQuery(StringScanner s)
        {
            var left = this.ReadOneQuery(s);

            if (s.Scan(@"\s*\)\s*").Length > 0)
            {
                return left;
            }

            var oper = s.Scan(@"\s*(and|or)\s*").Trim();

            if(oper.Length == 0) throw new ApplicationException("Invalid query operator");

            return oper == "and" ?
                Query.AND(left, this.ReadInlineQuery(s)) :
                Query.OR(left, this.ReadInlineQuery(s));
        }

        private Query ReadOneQuery(StringScanner s)
        {
            var field = s.Scan(@"\w+(.\w+)*\s*").Trim();
            var oper = s.Scan(@"(=|!=|>=|<=|>|<|like|in|between)");
            var value = new JsonReader().ReadValue(s);

            switch (oper)
            {
                case "=": return Query.EQ(field, value.RawValue);
                case "!=": return Query.Not(field, value.RawValue);
                case ">": return Query.GT(field, value.RawValue);
                case ">=": return Query.GTE(field, value.RawValue);
                case "<": return Query.LT(field, value.RawValue);
                case "<=": return Query.LTE(field, value.RawValue);
                case "like": return Query.StartsWith(field, value.AsString);
                case "in": return Query.In(field, value.AsArray.RawValue.ToArray());
                case "between": return Query.Between(field, value.AsArray.RawValue[0], value.AsArray.RawValue[1]);
                default: throw new ApplicationException("Invalid query operator");
            }
        }

    }
}
