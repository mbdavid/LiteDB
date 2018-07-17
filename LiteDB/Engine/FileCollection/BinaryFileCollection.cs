using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Engine
{
    /// <summary>
    /// Represent an external file with binary data content. File will be break in chunks
    /// </summary>
    public class BinaryFileCollection : IFileCollection
    {
        private readonly string _filename;
        private readonly int _bufferSize;

        public BinaryFileCollection(string filename, int bufferSize = 8000)
        {
            _filename = filename;
            _bufferSize = bufferSize;
        }

        public string Name => Path.GetFileName(_filename);

        public IEnumerable<BsonDocument> Input()
        {
            var buffer = new byte[_bufferSize];
            var bytesRead = 0;
            var chunk = 0;

            using (var fs = new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                while((bytesRead = fs.Read(buffer, 0, _bufferSize)) > 0)
                {
                    yield return new BsonDocument
                    {
                        ["chunk"] = chunk++,
                        ["length"] = bytesRead,
                        ["data"] = buffer
                    };

                    buffer = new byte[_bufferSize];
                }
            }
        }

        public int Output(IEnumerable<BsonValue> source)
        {
            var index = 0;

            using (var fs = new FileStream(_filename, FileMode.CreateNew))
            {
                foreach(var value in source)
                {
                    var buffer = value.AsBinary;

                    fs.Write(buffer, 0, buffer.Length);

                    index++;
                }

                fs.Flush();
            }

            return index;
        }
    }
}
