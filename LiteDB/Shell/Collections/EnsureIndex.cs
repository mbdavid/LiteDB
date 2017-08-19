using System;
using System.Collections.Generic;

namespace LiteDB.Shell
{
    internal class CollectionEnsureIndex : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "ensure[iI]ndex");
        }

        public IEnumerable<BsonValue> Execute(StringScanner s, LiteEngine engine)
        {
            var col = this.ReadCollection(engine, s);
            var field = s.Scan(this.FieldPattern).Trim().ThrowIfEmpty("Invalid field name");
            var unique = false;
            string expression = null;

            s.Scan(@"\s*");

            if (s.HasTerminated == false)
            {
                var options = JsonSerializer.Deserialize(s.ToString());

                if (options.IsBoolean)
                {
                    unique = options.AsBoolean;
                }
                else if (options.IsDocument) // support old version index definitions
                {
                    var doc = options.AsDocument;

                    unique = doc.ContainsKey("unique") ? doc["unique"].AsBoolean : false;
                    expression = doc.ContainsKey("expr") ? doc["expr"].AsString : null;
                }
            }

            yield return engine.EnsureIndex(col, field, unique, expression);
        }
    }
}