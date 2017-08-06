using System;

namespace LiteDB.Shell.Commands
{
    internal class CollectionEnsureIndex : BaseCollection, ICommand
    {
        public bool IsCommand(StringScanner s)
        {
            return this.IsCollectionCommand(s, "ensure[iI]ndex");
        }

        public void Execute(StringScanner s, Env env)
        {
            var col = this.ReadCollection(env.Engine, s);
            var field = s.Scan(this.FieldPattern).Trim().ThrowIfEmpty("Invalid field name");
            var unique = false;

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
                    unique = options.AsDocument["unique"].AsBoolean;
                }
            }

            env.Display.WriteResult(env.Engine.EnsureIndex(col, field, unique));
        }
    }
}