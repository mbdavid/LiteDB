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
        /// Upgrade old version of LiteDB into new LiteDB file structure. Returns true if database was completed converted
        /// If database already in current version just return false
        /// </summary>
        public static bool Upgrade(string filename, string password = null)
        {
            if (filename.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(filename));
            if (!File.Exists(filename)) return false;

            var settings = new EngineSettings
            {
                Filename = filename,
                Password = password,
                Checkpoint = 0
            };

            var backup = FileHelper.GetSufixFile(filename, "-backup", true);

            settings.Filename = FileHelper.GetSufixFile(filename, "-temp", true);

            var buffer = new byte[PAGE_SIZE * 2];
            IFileReader reader;

            using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                // read first 16k
                stream.Read(buffer, 0, buffer.Length);

                // checks if v8 plain data or encrypted (first byte = 1)
                if ((Encoding.UTF8.GetString(buffer, HeaderPage.P_HEADER_INFO, HeaderPage.HEADER_INFO.Length) == HeaderPage.HEADER_INFO &&
                    buffer[HeaderPage.P_FILE_VERSION] == HeaderPage.FILE_VERSION) ||
                    buffer[0] == 1)
                {
                    return false;
                }

                // checks if v7 (plain or encrypted)
                if (Encoding.UTF8.GetString(buffer, 25, HeaderPage.HEADER_INFO.Length) == HeaderPage.HEADER_INFO &&
                    buffer[52] == 7)
                {
                    reader = new FileReaderV7(stream, password);
                }
                else
                {
                    throw new LiteException(0, "Invalid data file format to upgrade");
                }

                try
                {
                    using (var engine = new LiteEngine(settings))
                    {
                        engine.Rebuild(reader);

                        engine.Checkpoint();
                    }
                }
                finally
                {
                    reader.Dispose();
                }
            }

            // rename source filename to backup name
            File.Move(filename, backup);

            // rename temp file into filename
            File.Move(settings.Filename, filename);

            return true;
        }
    }
}