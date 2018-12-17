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
        /// Upgrade datafile from old versions - use same process as Shrink
        /// </summary>
        private void Upgrade()
        {
            throw new NotImplementedException(); /*
            LOG($"upgrading datafile from {_header.FileVersion} to new v8 version", "UPGRADE");

            // only FileStream can be upgratable
            if (!(_factory is FileStreamDiskFactory))
            {
                throw new NotSupportedException("Current datafile must be upgrade but are not using FileStreamDiskFactory.");
            }

            // make a backup to original datafile
            var backup = FileHelper.GetTempFile(_factory.Filename, "-backup", true);

            File.Copy(_factory.Filename, backup);

            using (var stream = _factory.GetDataFileStream(false))
            {
                // there is only v7 upgrade version
                var reader = new FileReaderV7(stream);

                // upgrade is same operation than Shrink, but use custom file reader
                this.Shrink(reader);
            }*/
        }
    }
}
