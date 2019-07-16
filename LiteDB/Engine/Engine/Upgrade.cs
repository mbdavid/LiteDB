using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Upgrade old version of LiteDB into new LiteDB file structure. Returns true if database was completed converted
        /// </summary>
        public static bool Upgrade(string filename, string password = null)
        {
            if (filename.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(filename));

            var settings = new EngineSettings
            {
                Filename = filename,
                Password = password
            };

            try
            {
                var engine = new LiteEngine(settings);

                engine.Dispose();

                // if no problem in open database, it's already in v8
                return false;
            }
            catch(LiteException ex) when (ex.ErrorCode == LiteException.INVALID_DATABASE_VERSION)
            {
                var backup = FileHelper.GetSufixFile(filename, "-backup", true);

                File.Move(filename, backup);

                using (var engine = new LiteEngine(settings))
                using (var stream = new FileStream(backup, FileMode.Open, FileAccess.Read))
                {
                    engine.Rebuild(new FileReaderV7(stream, password));

                    engine.Checkpoint();
                }

                return true;
            }

            return false;
        }
    }
}