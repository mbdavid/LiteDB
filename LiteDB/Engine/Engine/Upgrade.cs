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
        /// Test if header buffer is old version of LiteDB (works only with v7 - LiteDB4)
        /// Create backup before any change
        /// </summary>
        private void TryUpgrade(BufferSlice buffer)
        {
            var info = buffer.ReadString(25, 27);
            var ver = buffer.ReadByte(52);

            if (info == "** This is a LiteDB file **" && ver == 7)
            {
                var recovery = buffer.ReadBool(4095);

                if (recovery) throw new LiteException(0, "Datafile in recovery mode. Before upgrade datafile version, you must open/recovery in LiteDB 4.1.x");

                var backup = FileHelper.GetTempFile(_settings.Filename, "-backup", true);

                File.Copy(_settings.Filename, backup);

                using (var stream = new FileStream(_settings.Filename, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    // there is only v7 upgrade version
                    var reader = new FileReaderV7(stream);

                    // upgrade is same operation than Shrink, but use custom file reader
                    this.Shrink(reader);
                }
            }
        }
    }
}
