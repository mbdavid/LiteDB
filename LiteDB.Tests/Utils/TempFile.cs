using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Tests
{
    public class TempFile : IDisposable
    {
        private bool _checkIntegrity = false;

        public string Filename { get; private set; }

        public TempFile(string ext = "db", bool checkIntegrity = true)
        {
            this.Filename = Path.Combine(Path.GetTempPath(), string.Format("test-{0}.{1}", Guid.NewGuid(), ext));
            _checkIntegrity = checkIntegrity;
        }

        public void CreateDatafile()
        {
            using (var s = new FileStream(Filename, System.IO.FileMode.CreateNew))
            {
                LiteEngine.CreateDatabase(s);
            }
        }

        public IDiskService Disk(bool journal = true)
        {
            return new FileDiskService(Filename, journal);
        }

        public IDiskService Disk(FileOptions options)
        {
            return new FileDiskService(Filename, options);
        }

        public string Conn(string connectionString)
        {
            return "filename=\"" + this.Filename + "\";" + connectionString;
        }

        #region Dispose

        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~TempFile()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // free other managed objects that implement
                // IDisposable only
            }

            // check file integrity
            if (_checkIntegrity)
            {
                this.CheckIntegrity();
            }

            File.Delete(this.Filename);

            _disposed = true;
        }

        #endregion

        public long Size
        {
            get { return new FileInfo(this.Filename).Length; }
        }

        public string ReadAsText()
        {
            return File.ReadAllText(this.Filename);
        }

        /// <summary>
        /// Read all colleciton, indexes and documents inside current datafile
        /// Drop per index, per collection and shrink
        /// This steps will check/validate all file data
        /// </summary>
        private void CheckIntegrity()
        {
            using (var db = new LiteEngine(this.Filename))
            {
                var cols = db.GetCollectionNames().ToArray();

                foreach(var col in cols)
                {
                    var indexes = db.GetIndexes(col).ToArray();

                    foreach(var idx in indexes)
                    {
                        var q = db.Find(col, Query.All(idx.Field));

                        foreach(var doc in q)
                        {
                            // document are ok!
                        }

                        // lets drop this index (if not _id)
                        if(idx.Field != "_id")
                        {
                            db.DropIndex(col, idx.Field);
                        }
                    }

                    // and drop collection
                    db.DropCollection(col);
                }

                // and now shrink
                db.Shrink();
            }
        }

        #region LoremIpsum Generator

        public static string LoremIpsum(int minWords, int maxWords,
            int minSentences, int maxSentences,
            int numParagraphs)
        {
            var words = new[] { "lorem", "ipsum", "dolor", "sit", "amet", "consectetuer",
                "adipiscing", "elit", "sed", "diam", "nonummy", "nibh", "euismod",
                "tincidunt", "ut", "laoreet", "dolore", "magna", "aliquam", "erat" };

            var rand = new Random(DateTime.Now.Millisecond);
            var numSentences = rand.Next(maxSentences - minSentences) + minSentences + 1;
            var numWords = rand.Next(maxWords - minWords) + minWords + 1;

            var result = new StringBuilder();

            for (int p = 0; p < numParagraphs; p++)
            {
                for (int s = 0; s < numSentences; s++)
                {
                    for (int w = 0; w < numWords; w++)
                    {
                        if (w > 0) { result.Append(" "); }
                        result.Append(words[rand.Next(words.Length)]);
                    }
                    result.Append(". ");
                }
                result.AppendLine();
            }

            return result.ToString();
        }

        #endregion
    }
}