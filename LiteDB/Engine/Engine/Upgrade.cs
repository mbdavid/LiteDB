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
            catch (LiteException ex) when (ex.ErrorCode == LiteException.INVALID_DATABASE)
            {
                var backup = FileHelper.GetSufixFile(filename, "-backup", true);

                settings.Filename = FileHelper.GetSufixFile(filename, "-temp", true);

                try
                {
                    // current versions works only converting from v7
                    using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
                    using (var reader = new FileReaderV7(stream, password))
                    using (var engine = new LiteEngine(settings))
                    {
                        engine.Rebuild(reader);

                        engine.Checkpoint();
                    }

                    // rename source filename to backup name
                    File.Move(filename, backup);

                    // rename temp file into filename
                    File.Move(settings.Filename, filename);
                }
                catch (Exception)
                {
                    FileHelper.TryDelete(settings.Filename);

                    throw;
                }

                return true;
            }
        }
    }
}