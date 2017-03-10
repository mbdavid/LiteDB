using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace LiteDB.Shell.Commands
{
    internal class BaseCollection
    {
        public Regex FieldPattern = new Regex(@"[\w$\.-]+\s*", RegexOptions.Compiled);

        /// <summary>
        /// Read collection name from db.(collection).(command)
        /// </summary>
        public string ReadCollection(LiteEngine db, StringScanner s)
        {
            return s.Scan(@"db\.([\w-]+)\.\w+\s*", 1);
        }

        public bool IsCollectionCommand(StringScanner s, string command)
        {
            return s.Match(@"db\.[\w-]+\." + command);
        }

        public KeyValuePair<int, int> ReadSkipLimit(StringScanner s)
        {
            var skip = 0;
            var limit = int.MaxValue;

            if (s.Match(@"\s*skip\s+\d+"))
            {
                skip = Convert.ToInt32(s.Scan(@"\s*skip\s+(\d+)\s*", 1));
            }

            if (s.Match(@"\s*limit\s+\d+"))
            {
                limit = Convert.ToInt32(s.Scan(@"\s*limit\s+(\d+)\s*", 1));
            }

            // skip can be before or after limit command
            if (s.Match(@"\s*skip\s+\d+"))
            {
                skip = Convert.ToInt32(s.Scan(@"\s*skip\s+(\d+)\s*", 1));
            }

            return new KeyValuePair<int, int>(skip, limit);
        }

        public Query ReadQuery(StringScanner s)
        {
            if (s.HasTerminated || s.Match(@"skip\s+\d") || s.Match(@"limit\s+\d"))
            {
                return Query.All();
            }

            return this.ReadInlineQuery(s);
        }

        private Query ReadInlineQuery(StringScanner s)
        {
            var left = this.ReadOneQuery(s);
            var oper = s.Scan(@"\s+(and|or)\s+").Trim();

            // there is no right side
            if (oper.Length == 0) return left;

            var right = this.ReadInlineQuery(s);

            return oper == "and" ? Query.And(left, right) : Query.Or(left, right);
        }

        private Query ReadOneQuery(StringScanner s)
        {
            var field = s.Scan(this.FieldPattern).Trim().ThrowIfEmpty("Invalid field name");
            var oper = s.Scan(@"(=|!=|>=|<=|>|<|like|in|between|contains)").ThrowIfEmpty("Invalid query operator");
            var value = JsonSerializer.Deserialize(s);

            switch (oper)
            {
                case "=": return Query.EQ(field, value);
                case "!=": return Query.Not(field, value);
                case ">": return Query.GT(field, value);
                case ">=": return Query.GTE(field, value);
                case "<": return Query.LT(field, value);
                case "<=": return Query.LTE(field, value);
                case "like": return Query.StartsWith(field, value);
                case "in": return Query.In(field, value.AsArray);
                case "between": return Query.Between(field, value.AsArray[0], value.AsArray[1]);
                case "contains": return Query.Contains(field, value);
                default: throw new LiteException("Invalid query operator");
            }
        }
    }
}