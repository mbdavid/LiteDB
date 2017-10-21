using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace LiteDB.Shell
{
    internal class BaseCollection
    {
        /// <summary>
        /// Field only regex pattern (My.Demo.T_est)
        /// </summary>
        public Regex FieldPattern = new Regex(@"^[\$\w](\.?[\w\$][\w-]*)*\s*", RegexOptions.Compiled);

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

        /// <summary>
        /// Read includes paths using comma delimiter: xxx include $.Books[*], $.Customer
        /// </summary>
        public string[] ReadIncludes(StringScanner s)
        {
            if (s.Scan(@"\s*include[s]?\s+").Length > 0)
            {
                var includes = new List<string>();
                var include = BsonExpression.ReadExpression(s, true, true);

                while (include != null)
                {
                    includes.Add(include.Source);

                    // capture next only if found comma symbol
                    include = s.Scan(@"\s*,\s*").Length > 0 ?
                        BsonExpression.ReadExpression(s, true, true) :
                        null;
                }

                return includes.ToArray();
            }

            return new string[0];
        }

        public Query ReadQuery(StringScanner s, bool required)
        {
            s.Scan(@"\s*");

            if (required && s.HasTerminated) throw LiteException.SyntaxError(s, "Unexpected finish of line");

            if (s.HasTerminated || s.Match(@"skip\s+\d") || s.Match(@"limit\s+\d") || s.Match(@"include[s]?\s+[\$\w]"))
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
            var field = BsonExpression.ReadExpression(s, false, false)?.Source ?? s.Scan(this.FieldPattern).Trim().ThrowIfEmpty("Invalid field", s);
            var oper = s.Scan(@"\s*(=|!=|>=|<=|>|<|like|starts[Ww]ith|in|between|contains)\s*").Trim().ToLower().ThrowIfEmpty("Invalid query operator", s);

            if (s.HasTerminated) throw LiteException.SyntaxError(s, "Missing value");

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

        /// <summary>
        /// Read BsonValue from StringScanner or returns null if not a valid bson value
        /// </summary>
        public BsonValue ReadBsonValue(StringScanner s)
        {
            var start = s.Index;

            try
            {
                return JsonSerializer.Deserialize(s);
            }
            catch (LiteException ex) when (ex.ErrorCode == LiteException.UNEXPECTED_TOKEN)
            {
                s.Index = start;
                return null;
            }
        }
    }
}