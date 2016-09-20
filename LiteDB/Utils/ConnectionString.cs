using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LiteDB
{
    /// <summary>
    /// Manage ConnectionString to connect and create databases. Connection string are NameValue using Name1=Value1; Name2=Value2
    /// </summary>
    public class ConnectionString
    {
        private Dictionary<string, string> _values;

        public ConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException("connectionString");

            _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var inClosure = false;
            var inValue = false;
            var key = string.Empty;
            var value = string.Empty;
            for (var i = 0; i < connectionString.Length; i++)
            {
                if (inValue)
                {
                    if (inClosure)
                    {
                        if (connectionString[i] == '"' && !(i > 0 && connectionString[i -1] == '\\'))
                        {
                            inClosure = false;
                        }
                        else
                        {
                            value += connectionString[i];
                        }
                    }
                    else
                    {
                        if (connectionString[i] == '"' && !(i > 0 && connectionString[i - 1] == '\\'))
                        {
                            inClosure = true;
                        }
                        else if (connectionString[i] == ';')
                        {
                            _values.Add(key.Trim(), value);
                            key = string.Empty;
                            value = string.Empty;
                            inValue = false;
                        }
                        else
                        {
                            value += connectionString[i];
                        }
                    }
                }
                else
                {
                    if (connectionString[i] == '=')
                    {
                        inValue = true;
                    }
                    else if (connectionString[i] == ';')
                    {
                        _values.Add(key.Trim(), string.Empty);
                        key = string.Empty;
                    }
                    else
                    {
                        key += connectionString[i];
                    }
                }
            }
            if (!string.IsNullOrEmpty(key.Trim()))
            {
                _values.Add(key, value);
            }

            // return if there is more than one pair
            //           or the only pair's name is 'filename'
            //           or the only pair's value is not empty
            if (_values.Count > 1 || _values.ContainsKey("filename") || !_values.ContainsValue(string.Empty))
                return;

            _values.Clear();
            _values["filename"] = (_values.Count > 0 ? _values.Keys.FirstOrDefault() : connectionString.Trim().Trim('"')) ??
                                  string.Empty;
#if !PCL
            _values["filename"] = Path.GetFullPath(_values["filename"]);
#endif
        }

        public T GetValue<T>(string key, T defaultValue)
        {
            try
            {
                return _values.ContainsKey(key) ?
                    (T)Convert.ChangeType(_values[key], typeof(T)) :
                    defaultValue;
            }
            catch (Exception)
            {
                throw new LiteException("Invalid connection string value type for " + key);
            }
        }

        /// <summary>
        /// Get a value from a key converted in file size format: "1gb", "10 mb", "80000"
        /// </summary>
        public long GetFileSize(string key, long defaultSize)
        {
            var size = this.GetValue<string>(key, "");

            if (size.Length == 0) return defaultSize;

            var match = Regex.Match(size, @"^(\d+)\s*([tgmk])?(b|byte|bytes)?$", RegexOptions.IgnoreCase);

            if (!match.Success) return 0;

            var num = Convert.ToInt64(match.Groups[1].Value);

            switch (match.Groups[2].Value.ToLower())
            {
                case "t": return num * 1024L * 1024L * 1024L * 1024L;
                case "g": return num * 1024L * 1024L * 1024L;
                case "m": return num * 1024L * 1024L;
                case "k": return num * 1024L;
                case "": return num;
            }

            return 0;
        }

        public static String FormatFileSize(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }
    }
}