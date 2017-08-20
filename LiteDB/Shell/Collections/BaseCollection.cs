using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LiteDB.Shell
{
    internal class BaseCollection
    {
        /// <summary>
        /// Field (or path) regex pattern
        /// </summary>
        public Regex FieldPattern = new Regex(@"[\w\.-]+\s*", RegexOptions.Compiled);

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

        public string[] ReadIncludes(StringScanner s)
        {
            if (s.Scan(@"\s*include[s]?\s+").Length > 0)
            {
                var includes = JsonSerializer.Deserialize(s);

                if (includes.IsString)
                {
                    return new string[] { includes.AsString };
                }
                else if(includes.IsArray)
                {
                    return includes.AsArray.Select(x => x.AsString).ToArray();
                }
                else
                {
                    throw LiteException.InvalidFormat(includes.ToString());
                }
            }

            return new string[0];
        }

        public Query ReadQuery(StringScanner s)
        {
            if (s.HasTerminated || s.Match(@"skip\s+\d") || s.Match(@"limit\s+\d") || s.Match(@"include[s]?\s+[\""\[]"))
            {
                return Query.All();
            }

            return this.ReadInlineQuery(s);
        }

        private Query ReadInlineQuery(StringScanner s)
        {
            var left = this.ReadOneQuery(s);
            var oper = s.Scan(@"\s+(and|or)\s+").ToLower().Trim();

            // there is no right side
            if (oper.Length == 0) return left;

            var right = this.ReadInlineQuery(s);

            return oper == "and" ? Query.And(left, right) : Query.Or(left, right);
        }

        private Query ReadOneQuery(StringScanner s)
        {
            var field = LiteExpression.Extract(s).TrimToNull() ??
                s.Scan(this.FieldPattern).Trim();

            var oper = s.Scan(@"(=|!=|>=|<=|>|<|like|starts[Ww]ith|in|between|contains)").ToLower().ThrowIfEmpty("Invalid query operator");
            var value = JsonSerializer.Deserialize(s);

            switch (oper)
            {
                case "=": return Query.EQ(field, value);
                case "!=": return Query.Not(field, value);
                case ">": return Query.GT(field, value);
                case ">=": return Query.GTE(field, value);
                case "<": return Query.LT(field, value);
                case "<=": return Query.LTE(field, value);
                case "like":
                case "startswith": return Query.StartsWith(field, value);
                case "in": return Query.In(field, value.AsArray);
                case "between": return Query.Between(field, value.AsArray[0], value.AsArray[1]);
                case "contains": return Query.Contains(field, value);
                default: throw new LiteException("Invalid query operator");
            }
        }
    }
}