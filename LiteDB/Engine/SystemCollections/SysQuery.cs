using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    /// <summary>
    /// This class implement $query experimental system function to run sub-queries. It's experimental only - possible not be present in final release
    /// </summary>
    internal class SysQuery : SystemCollection
    {
        public SysQuery() : base("$query")
        {
        }

        public override bool IsFunction => true;

        public override IEnumerable<BsonDocument> Input(LiteEngine engine, BsonValue options)
        {
            var query = options.AsString;

            using (var reader = engine.Execute(query, null))
            {
                while(reader.Read())
                {
                    var value = reader.Current;

                    yield return value.IsDocument ? value.AsDocument : new BsonDocument { ["expr"] = value };
                }
            }
        }

        public override int Output(IEnumerable<BsonValue> source, BsonValue options) => throw new NotSupportedException("$query do not support as output function");
    }
}