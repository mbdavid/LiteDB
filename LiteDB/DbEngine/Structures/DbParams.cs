namespace LiteDB
{
    /// <summary>
    /// DbParams is 200 bytes data stored in header page and used to setup variables to database
    /// </summary>
    internal class DbParams
    {
        /// <summary>
        /// Database user version 
        /// </summary>
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