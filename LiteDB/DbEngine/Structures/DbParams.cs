namespace LiteDB
{
    /// <summary>
    /// DbParams is 200 bytes data stored in header page and used to setup variables to database
    /// </summary>
    internal class DbParams
    {
        /// <summary>
        /// Database user version [2 bytes]
        /// </summary>
        public ushort DbVersion = 0;

        /// <summary>
        /// Password hash in SHA1 [20 bytes]
        /// </summary>
        public byte[] Password = new byte[20];

        public void Read(ByteReader reader)
        {
            this.DbVersion = reader.ReadUInt16();
            this.Password = reader.ReadBytes(20);
            reader.Skip(178);
        }

        public void Write(ByteWriter writer)
        {
            writer.Write(this.DbVersion);
            writer.Write(this.Password);
            writer.Skip(178);
        }
    }
}