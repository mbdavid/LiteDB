using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;
using System.Text.RegularExpressions;

namespace LiteDB
{
    /// <summary>
    /// Manage ConnectionString to connect and create databases. Can be used as:
    /// * If only a word - get from App.Config
    /// * If is a path - use all parameters as default
    /// * Otherwise, is name=value collection
    /// </summary>
    public class ConnectionString
    {
        /// <summary>
        /// Path of filename (no default - required key)
        /// </summary>
        public string Filename { get; private set; }

        /// <summary>
        /// Default Timeout connection to wait for unlock (default: 1 minute)
        /// </summary>
        public TimeSpan Timeout { get; private set; }

        /// <summary>
        /// Limits the datafile to grow up than a limit
        /// </summary>
        public long MaxFileLength { get; private set; }

        /// <summary>
        /// Returns MaxPageID
        /// </summary>
        internal uint MaxPageID { get; private set; }

        /// <summary>
        /// Supports recovery mode if a fail during write pages to disk
        /// </summary>
        public bool JournalEnabled { get; private set; }

        public ConnectionString(string connectionString)
        {
            // If is only a name, get connectionString from App.config
            if (Regex.IsMatch(connectionString, @"^\w+$"))
                connectionString = ConfigurationManager.ConnectionStrings[connectionString].ConnectionString;

            // Create a dictionary from string name=value collection
            var values = new Dictionary<string, string>();

            if(connectionString.Contains("="))
            {
                values = connectionString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Split(new char[] { '=' }, 2))
                    .ToDictionary(t => t[0].Trim().ToLower(), t => t.Length == 1 ? "" : t[1].Trim(), StringComparer.InvariantCultureIgnoreCase);
            }
            else
            {
                // If connectionstring is only a filename, set filename 
                values["filename"] = connectionString;
            }

            // Read connection string parameters with default value
            this.Timeout = this.GetValue<TimeSpan>(values, "timeout", new TimeSpan(0, 1, 0));
            this.Filename = this.GetValue<string>(values, "filename", "");
            this.MaxFileLength = this.GetValue<long>(values, "maxfilelength", long.MaxValue);
            this.JournalEnabled = this.GetValue<bool>(values, "journal", true);

            // Validade parameter values
            if (string.IsNullOrEmpty(Filename)) throw new ArgumentException("Missing FileName in ConnectionString");
            if (this.MaxFileLength < (256 * 1024)) throw new ArgumentException("MaxFileLength must be bigger than 262.144 (256Kb)");

            // calcs MaxPageID
            this.MaxPageID = this.MaxFileLength == long.MaxValue ? uint.MaxValue : (uint)(this.MaxFileLength / BasePage.PAGE_SIZE);
        }

        private T GetValue<T>(Dictionary<string, string> values, string key, T defaultValue)
        {
            return values.ContainsKey(key) ?
                (T)Convert.ChangeType(values[key], typeof(T)) :
                defaultValue;
        }
    }
}
