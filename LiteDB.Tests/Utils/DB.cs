using System;
using System.Collections.Generic;
using LiteDB;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace UnitTest
{
    public class DB
    {
        // Delete Temp databases
        //static DB()
        //{
        //    var dir = System.IO.Path.GetDirectoryName(Path());

        //    foreach (var f in System.IO.Directory.GetFiles(dir, "*.db"))
        //    {
        //        System.IO.Directory.Delete(f);
        //    }
        //}


        // Get a unique database name in TestResults folder
        public static string Path()
        {
            var path = System.IO.Path.GetFullPath(
                System.IO.Directory.GetCurrentDirectory() + 
                string.Format("../../../../TestResults/test-{0}.db", Guid.NewGuid()));

            return path;
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
