using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LiteDB.Tests
{
    public class DB
    {
        public static List<string> _files = new List<string>();

        // Get a unique database name in TestResults folder
        public static string RandomFile(string ext = "db")
        {
            var path = System.IO.Path.GetFullPath(
                System.IO.Directory.GetCurrentDirectory() +
                string.Format("../../../../TestResults/test-{0}.{1}", Guid.NewGuid(), ext));

            _files.Add(path);

            return path;
        }

        public static void DeleteFiles()
        {
            foreach(var f in _files)
            {
                File.Delete(f);
            }
            _files = new List<string>();
        }

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
    }
}