using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// DbParams is 200 bytes data stored in header page and used to setup variables to database
    /// </summary>
    internal class DbParams
    {
        public ushort DbVersion = 0;

        public void Read(ByteReader reader)
        {
            this.DbVersion = reader.ReadUInt16();
            reader.Skip(198);
        }

        public void Write(ByteWriter writer)
        {
            writer.Write(this.DbVersion);
            writer.Skip(198);
        }
    }
}
