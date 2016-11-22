using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// A simple file helper tool with static methods
    /// </summary>
    internal class FileHelper
    {
        /// <summary>
        /// Create a temp filename based on original filename - checks if file exists (if exists, append counter number)
        /// </summary>
        public static string GetTempFile(string filename, string sufix = "-temp", bool checkIfExists = true)
        {
            var count = 0;
            var temp = Path.Combine(Path.GetDirectoryName(filename), 
                Path.GetFileNameWithoutExtension(filename) + sufix + 
                Path.GetExtension(filename));

            while(checkIfExists && File.Exists(temp))
            {
                temp = Path.Combine(Path.GetDirectoryName(filename),
                    Path.GetFileNameWithoutExtension(filename) + sufix +
                    "-" + (++count) +
                    Path.GetExtension(filename));
            }

            return temp;
        }
    }
}
