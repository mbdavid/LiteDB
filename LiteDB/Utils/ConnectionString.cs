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
    /// Manage ConnectionString to connect and create databases. Connection string are NameValue using Name1=Value1; Name2=Value2
    /// </summary>
    internal class ConnectionString
    {
        private Dictionary<string, string> _values;

        public ConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException("connectionString");

            // Create a dictionary from string name=value collection
            if(connectionString.Contains("="))
            {
                _values = connectionString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Split(new char[] { '=' }, 2))
                    .ToDictionary(t => t[0].Trim().ToLower(), t => t.Length == 1 ? "" : t[1].Trim(), StringComparer.InvariantCultureIgnoreCase);
            }
            else
            {
                // If connectionstring is only a filename, set filename 
                _values = new Dictionary<string, string>();
                _values["filename"] = Path.GetFullPath(connectionString);
            }
        }

        public T GetValue<T>(string key, T defaultValue)
        {
            try
            {
                return _values.ContainsKey(key) ?
                    (T)Convert.ChangeType(_values[key], typeof(T)) :
                    defaultValue;
            }
            catch(Exception)
            {
                throw new LiteException("Invalid connection string value type for " + key);
            }
        }
    }
}
