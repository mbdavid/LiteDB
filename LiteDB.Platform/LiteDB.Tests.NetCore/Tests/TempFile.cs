using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Tests.NetCore.Tests
{
    public class TempFile : IDisposable
    {
        public string Filename { get; private set; }
        public string ConnectionString { get; private set; }

        public TempFile(string connectionString = null, string ext = "db")
        {
            this.Filename = TestPlatform.GetFullPath(string.Format("test-{0}.{1}", Guid.NewGuid(), ext));
            this.ConnectionString = "filename=" + this.Filename + ";" + connectionString;
        }

        public void Dispose()
        {
            TestPlatform.DeleteFile(this.Filename);
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
