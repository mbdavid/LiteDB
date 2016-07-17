using System;
using System.Collections.Generic;
using System.Text;

namespace LiteDB.Tests
{
    public class TempFile : IDisposable
    {
        public string Filename { get; private set; }
        public string ConnectionString { get; private set; }

        public TempFile(string connectionString = null)
        {
            var path = this.Filename = TestPlatform.GetTempFilePath("db");

            this.Filename = path;
            this.ConnectionString = connectionString == null ?
                path : "filename=" + path + ";" + connectionString;
        }

        public void Dispose()
        {
            TestPlatform.DeleteFile(Filename);
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