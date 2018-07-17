using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Engine
{
    /// <summary>
    /// Represent an external file with text data content. Will split each document per line of text
    /// </summary>
    public class TextFileCollection : IFileCollection
    {
        private readonly string _filename;

        public TextFileCollection(string filename)
        {
            _filename = filename;
        }

        public string Name => Path.GetFileName(_filename);

        public IEnumerable<BsonDocument> Input()
        {
            var line = 0;

            using (var fs = new FileStream(_filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var reader = new StreamReader(fs))
                {
                    while(!reader.EndOfStream)
                    {
                        var text = reader.ReadLine();

                        yield return new BsonDocument
                        {
                            ["line"] = line++,
                            ["text"] = text
                        };
                    }
                }
            }
        }

        public int Output(IEnumerable<BsonValue> source)
        {
            throw new NotSupportedException();
        }
    }
}
