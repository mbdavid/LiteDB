using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Rebuild database removing all empty pages.
        /// </summary>
        public static long Shrink(string filename, string password = null, string newPassword = null)
        {
            var logFile = FileHelper.GetLogFile(filename);

            // first step: if there is log file, runs checkpoint first
            if (File.Exists(logFile))
            {
                using (var e = new LiteEngine(new EngineSettings { Filename = filename, Password = password }))
                {
                    e.Checkpoint();
                }
            }

            // getting original file length
            var originalLength = new FileInfo(filename).Length;

            // create new empty log file
            using (var log = new FileStream(logFile, FileMode.CreateNew))
            {
                // configure a new engine instance to works with 1 page in memory (data) and all data into log file
                var settings = new EngineSettings
                {
                    DataStream = new MemoryStream(),
                    LogStream = log,
                    Password = newPassword
                };

                // now, copy all reader into this new engine
                using (var e = new LiteEngine(settings))
                using (var reader = new FileReaderV8(filename, password))
                {
                    e.Rebuild(reader);
                }

                // now i can shrink my data file to same size as new log
                using (var f = new FileStream(filename, FileMode.Open))
                {
                    f.SetLength(log.Length);
                }
            }

            // open again database to run last checkpoint and update all datafile
            using (var e = new LiteEngine(new EngineSettings { Filename = filename, Password = newPassword }))
            {
                e.Checkpoint();
            }

            // return shrink size (in bytes)
            return originalLength - (new FileInfo(filename).Length);
        }
    }
}