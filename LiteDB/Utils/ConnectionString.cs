using System;
using System.IO;
using System.Linq;

namespace LiteDB
{
    /// <summary>
    /// Manage ConnectionString to connect and create databases. Connection string are NameValue using Name1=Value1; Name2=Value2
    /// </summary>
    public class ConnectionString
    {
        private string _filename = string.Empty;

        /// <summary>
        /// "filename": Full path or relative path from DLL directory
        /// </summary>
        public string Filename
        {
            get => _filename;
            set
            {
                EnsureValidPath(value);
                _filename = value;
            }
        }

        /// <summary>
        /// "journal": Enabled or disable double write check to ensure durability (default: true)
        /// </summary>
        public bool Journal { get; set; } = true;

        /// <summary>
        /// "password": Encrypt (using AES) your datafile with a password (default: null - no encryption)
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// "cache size": Max number of pages in cache. After this size, flush data to disk to avoid too memory usage (default: 5000)
        /// </summary>
        public int CacheSize { get; set; } = 5000;

        /// <summary>
        /// "timeout": Timeout for waiting unlock operations (default: 1 minute)
        /// </summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// "mode": Define if datafile will be shared, exclusive or read only access (default in environments with file locking: Shared, otherwise: Exclusive)
        /// </summary>
#if HAVE_LOCK
        public FileMode Mode { get; set; } = FileMode.Shared;
#else
        public FileMode Mode { get; set; } = FileMode.Exclusive;
#endif

        /// <summary>
        /// "initial size": If database is new, initialize with allocated space - support KB, MB, GB (default: 0 bytes)
        /// </summary>
        public long InitialSize { get; set; } = 0;

        /// <summary>
        /// "limit size": Max limit of datafile - support KB, MB, GB (default: long.MaxValue - no limit)
        /// </summary>
        public long LimitSize { get; set; } = long.MaxValue;

        /// <summary>
        /// "log": Debug messages from database - use `LiteDatabase.Log` (default: Logger.NONE)
        /// </summary>
        public byte Log { get; set; } = Logger.NONE;

        /// <summary>
        /// "utc": Returns date in UTC timezone from BSON deserialization (default: false - LocalTime)
        /// </summary>
        public bool UtcDate { get; set; } = false;

        /// <summary>
        /// "upgrade": Test if database is in old version and update if needed (default: false)
        /// </summary>
        public bool Upgrade { get; set; } = false;

#if HAVE_SYNC_OVER_ASYNC
/// <summary>
/// "async": Use "sync over async" to UWP apps access any directory (default: false)
/// </summary>
        public bool Async { get; set; } = false;
#endif

#if HAVE_FLUSH_DISK
/// <summary>
/// "flush": If true, apply flush direct to disk, ignoring OS cache [FileStream.Flush(true)]
/// </summary>
        public bool Flush { get; set; } = false;
#endif

        /// <summary>
        /// Initialize empty connection string
        /// </summary>
        public ConnectionString()
        {
        }

        /// <summary>
        /// Initialize connection string parsing string in "key1=value1;key2=value2;...." format or only "filename" as default (when no ; char found)
        /// </summary>
        public ConnectionString(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(connectionString));

            Parse(connectionString);
        }

        private void Parse(string connectionString)
        {
            connectionString = connectionString.Trim().TrimEnd(';');
            if (!connectionString.Contains("="))
            {
                Filename = connectionString;
                return;
            }

            var index = 0;
            var working = connectionString;
            while (working.Contains("="))
            {
                working = working.Substring(index);
                ConsumeSettingToken(working, out var name, out index);

                working = working.Substring(index);
                ConsumeValue(working, out var value, out index);

                InitSetting(name, value);
            }

            if (working.Substring(index).Trim() != string.Empty)
                throw new FormatException($"Unexpected characters at position {index} in '{working}'");

            if (string.IsNullOrEmpty(Filename))
                throw new FormatException("Connection string did not contain a filename");
        }

        private void ConsumeSettingToken(string from, out string setting, out int endIndex)
        {
            var index = 0;
            var lastChar = '\0';
            var value = string.Empty;

            while (index < from.Length)
            {
                var ch = from[index++];

                if (ch == '=')
                    break;

                if (char.IsWhiteSpace(ch))
                {
                    // if we've not yet hit the keyword or there are multiple spaces between words (e.g. "  cache   size")
                    // then just skip the space
                    if (value == string.Empty || lastChar == ' ')
                        continue;

                    value += ' ';
                }
                else
                    value += ch;

                lastChar = ch;
            }

            setting = value.Trim().ToLower();
            endIndex = index;
        }

        private void ConsumeValue(string from, out string value, out int endIndex)
        {
            var index = 0;
            var isQuoted = false;
            var possibleEscape = false;
            var expectEnd = false;
            var trim = true;
            var workingValue = string.Empty;

            while (index < from.Length)
            {
                var ch = from[index++];

                if (char.IsWhiteSpace(ch) && (workingValue == string.Empty || expectEnd))
                    continue;

                // if a ; is surrounded by quotes then it's part of the value
                if (ch == ';' && (!isQuoted || expectEnd))
                    break;

                if (expectEnd && !char.IsWhiteSpace(ch))
                    throw new FormatException($"Unexpected character '{ch}' at position {index} in '{from}'");

                // this may mean the next character is escaped, or it may just be part of a Windows file path.
                // currently the only escaped character is " so if the next character is not " then the slash will 
                // just get added to the string later in this method
                if (ch == '\\')
                {
                    possibleEscape = true;
                    continue;
                }

                if (ch == '"' && !possibleEscape)
                {
                    // have we've reached the end of the literal block
                    if (isQuoted)
                        expectEnd = true;

                    // or are we about to enter a literal block
                    else if (workingValue == string.Empty)
                    {
                        isQuoted = true;

                        // spacing within a literal block is preserved
                        trim = false;
                    }
                    else
                        throw new FormatException($"Unexpected character '{ch}' at position {index} in '{from}'");

                    continue;
                }

                if (possibleEscape)
                {
                    if (ch != '"')
                        workingValue += '\\';

                    possibleEscape = false;
                }

                workingValue += ch;
            }

            value = trim ? workingValue.Trim() : workingValue;
            endIndex = index;
        }

        private void InitSetting(string name, string value)
        {
            switch (name)
            {
                case "filename":
                    Filename = value;
                    break;
                case "journal":
                    Journal = ParseBoolSetting(name, value);
                    break;
                case "password":
                    Password = value;
                    break;
                case "cache size":
                    CacheSize = ParseIntSetting(name, value);
                    break;
                case "timeout":
                    Timeout = ParseTimepanSetting(name, value);
                    break;
                case "mode":
                    Mode = ParseFileModeSetting(name, value);
                    break;
                case "initial size":
                    InitialSize = ParseSize(name, value);
                    break;
                case "limit size":
                    LimitSize = ParseSize(name, value);
                    break;
                case "upgrade":
                    Upgrade = ParseBoolSetting(name, value);
                    break;
                case "utc":
                    UtcDate = ParseBoolSetting(name, value);
                    break;
                case "log":
                    Log = ParseByteSetting(name, value);
                    break;
                case "async":
#if HAVE_SYNC_OVER_ASYNC
                    Async = ParseBoolSetting(name, value);
#endif
                    break;
                case "flush":
#if HAVE_FLUSH_DISK
                    Flush = ParseBoolSetting(name, value);
#endif
                    break;
                default:
                    throw new FormatException($"Unexpecting setting name '{name}'");
            }
        }

        private void EnsureValidPath(string value)
        {
            if (value.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                throw new FormatException($"Expected filename but read '{value}'");
        }

        private bool ParseBoolSetting(string setting, string value)
        {
            if (!bool.TryParse(value, out var result))
                throw new FormatException($"Expected boolean value for '{setting}' but read '{value}'");

            return result;
        }

        private byte ParseByteSetting(string setting, string value)
        {
            if (!byte.TryParse(value, out var result))
                throw new FormatException($"Expected byte value for '{setting}' but read '{value}'");

            return result;
        }

        private int ParseIntSetting(string setting, string value)
        {
            if (!int.TryParse(value, out var result))
                throw new FormatException($"Expected integer value for '{setting}' but read '{value}'");

            return result;
        }

        private TimeSpan ParseTimepanSetting(string setting, string value)
        {
            if (!TimeSpan.TryParse(value, out var result))
                throw new FormatException($"Expected timespan value for '{setting}' but read '{value}'");

            return result;
        }

        private FileMode ParseFileModeSetting(string setting, string value)
        {
            try
            {
                return (FileMode) Enum.Parse(typeof(FileMode), value, true);
            }
            catch
            {
                throw new FormatException($"Expected timespan value for '{setting}' but read '{value}'");
            }
        }

        private long ParseSize(string setting, string value)
        {
            if (long.TryParse(value, out var parsedSize))
                return parsedSize;

            var numString = string.Empty;
            var unitString = string.Empty;
            var isNumEnded = false;
            foreach (var c in value.Where(c => !char.IsWhiteSpace(c)))
            {
                if (!isNumEnded && char.IsDigit(c))
                    numString += c;
                else
                {
                    isNumEnded = true;
                    unitString += char.ToLower(c);
                }
            }

            if (!long.TryParse(numString, out parsedSize) || parsedSize <= 0)
                throw new FormatException($"Expected size definition for '{setting}' but read '{value}'");

            switch (unitString)
            {
                case "kb":
                    parsedSize *= 1024L;
                    break;
                case "mb":
                    parsedSize *= 1024L * 1024L;
                    break;
                case "gb":
                    parsedSize *= 1024L * 1024L * 1024L;
                    break;
                case "tb": // is this practical?
                    parsedSize *= 1024L * 1024L * 1024L * 1024L;
                    break;
                default:
                {
                    throw new FormatException(
                        $"Unexpected unit of '{unitString}' in '{setting}'.  Valid units are; KB, MB, GB");
                }
            }

            return parsedSize;
        }
    }
}