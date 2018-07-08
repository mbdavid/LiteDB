using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Upgrade datafile from old versions - use same process as Shrink: use new engine with same WAL filename and checkpoint over same datafile
        /// </summary>
        private void Upgrade()
        {
            _log.Info($"Upgrading datafile from {_header.FileVersion} to new v8 version");

            var factory = _settings.GetDiskFactory();

            // only FileStream can be upgratable
            if (!(factory is FileStreamDiskFactory))
            {
                throw new NotSupportedException("Current datafile must be upgrade but are not using FileStreamDiskFactory.");
            }

            // make a backup from old version datafile
            var backup = FileHelper.GetTempFile(factory.FileName, "-backup-v" + _header.FileVersion, true);

            File.Copy(factory.FileName, backup);

            using (var stream = factory.GetDataFileStream(false))
            {
                var u = new FileReaderV7(stream);

                var cc = u.GetCollections();
            }

        }
    }
}
