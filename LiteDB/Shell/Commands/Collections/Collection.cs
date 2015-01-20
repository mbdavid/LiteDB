using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiteDB.Shell.Commands
{
    class Collection
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
            if (s.Match(@"top\s+\d+\s*$"))
            {
                return Convert.ToInt32(s.Scan(@"top\s+(\d+)", 1));
            }

            return int.MaxValue;
        }

        public Query ReadQuery(StringScanner s)
        {
            if (s.HasTerminated)
            {
                return Query.All();
            }
            else
            {
                var field = s.Scan(@"[\w\.]+\s*").Trim();
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
                    default: throw new Exception("Its not a valid operator");
                }
            }
        }

    }
}
