using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// If Upgrade=true, run this before open Disk service
        /// </summary>
        private void TryUpgrade()
        {
            var filename = _settings.Filename;

            // if file not exists, just exit
            if (!File.Exists(filename)) return;

            using (var stream = new FileStream(
                _settings.Filename,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read, 1024))
            {
                var buffer = new byte[1024];

                stream.Position = 0;
                stream.Read(buffer, 0, buffer.Length);

                if (FileReaderV7.IsVersion(buffer) == false) return;
            }

            // run rebuild process
            this.Recovery(_settings.Collation);
        }

        /// <summary>
        /// Upgrade old version of LiteDB into new LiteDB file structure. Returns true if database was completed converted
        /// If database already in current version just return false
        /// </summary>
        [Obsolete("Upgrade your LiteDB v4 datafiles using Upgrade=true in EngineSettings. You can use upgrade=true in connection string.")]
        public static bool Upgrade(string filename, string password = null, Collation collation = null)
        {
            if (filename.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(filename));
            if (!File.Exists(filename)) return false;

            var settings = new EngineSettings
            {
                Filename = filename,
                Password = password,
                Collation = collation,
                Upgrade = true
            };

            using (var db = new LiteEngine(settings))
            {
                // database are now converted to v5
            }

            return true;
        }
    }
}