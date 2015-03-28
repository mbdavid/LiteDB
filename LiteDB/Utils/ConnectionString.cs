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
        /// Supports recovery mode if a fail during write pages to disk
        /// </summary>
        public bool JournalEnabled { get; private set; }

        /// <summary>
        /// Define, in connection string, the user database version. When you increse this value
        /// LiteDatabase will run OnUpdate method for each new version. If defined, must be >= 1. Default: 1
        /// </summary>
        public int UserVersion { get; private set; }

        public ConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException("connectionString");

            // If is only a name, get connectionString from App.config
            if (Regex.IsMatch(connectionString, @"^[\w-]+$"))
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
                values["filename"] = Path.GetFullPath(connectionString);
            }

            // Read connection string parameters with default value
            this.Timeout = this.GetValue<TimeSpan>(values, "timeout", new TimeSpan(0, 1, 0));
            this.Filename = Path.GetFullPath(this.GetValue<string>(values, "filename", ""));
            this.JournalEnabled = this.GetValue<bool>(values, "journal", true);
            this.UserVersion = this.GetValue<int>(values, "version", 1);

            // validade parameter values
            if (string.IsNullOrEmpty(Filename)) throw new ArgumentException("Missing FileName in ConnectionString");
            if (this.UserVersion <= 0) throw new ArgumentException("Connection String version must be greater or equals to 1");
        }

        private T GetValue<T>(Dictionary<string, string> values, string key, T defaultValue)
        {
            return values.ContainsKey(key) ?
                (T)Convert.ChangeType(values[key], typeof(T)) :
                defaultValue;
        }
    }
}
