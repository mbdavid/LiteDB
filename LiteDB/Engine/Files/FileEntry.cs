using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// Represets a file inside files collection
    /// </summary>
    public class FileEntry
    {
        public string Key { get; set; }
        public int Length { get; set; }
        public DateTime UploadDate { get; set; }
        public Dictionary<string, object> Metadata { get; set; }

        internal uint PageID { get; set; }

        internal FileEntry(string key, Dictionary<string, object> metadata)
        {
            this.PageID = uint.MaxValue;
            this.Key = key;
            this.Metadata = metadata == null ? new Dictionary<string, object>() : metadata;
            this.UploadDate = DateTime.Now;
        }

        public FileEntry(BsonDocument doc)
        {
            this.Key = doc["Key"].AsString;
            this.Length = doc["Length"].AsInt;
            this.UploadDate = doc["UploadDate"].AsDateTime;
            this.Metadata = (Dictionary<string, object>)doc["Metadata"].RawValue;
            this.PageID = (uint)doc["PageID"].RawValue;
        }

        internal BsonDocument ToBson()
        {
            var doc = new BsonDocument();

            doc["Key"] = new BsonValue(this.Key);
            doc["Length"] = new BsonValue(this.Length);
            doc["UploadDate"] = new BsonValue(this.UploadDate);
            doc["Metadata"] = new BsonValue(this.Metadata);
            doc["PageID"] = new BsonValue(this.PageID);

            return doc;
        }

        public LiteFileStream OpenRead(LiteEngine db)
        {
            return new LiteFileStream(db, this);
        }
    }
}
