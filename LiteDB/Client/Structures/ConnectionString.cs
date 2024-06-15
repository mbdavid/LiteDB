namespace LiteDB;

using System;
using System.Collections.Generic;
using LiteDB.Engine;

/// <summary>
///     Manage ConnectionString to connect and create databases. Connection string are NameValue using Name1=Value1;
///     Name2=Value2
/// </summary>
public class ConnectionString
{
    private readonly Dictionary<string, string> _values;

    /// <summary>
    ///     "connection": Return how engine will be open (default: Direct)
    /// </summary>
    public ConnectionType Connection { get; set; } = ConnectionType.Direct;

    /// <summary>
    ///     "filename": Full path or relative path from DLL directory
    /// </summary>
    public string Filename { get; set; } = "";

    /// <summary>
    ///     "password": Database password used to encrypt/decypted data pages
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    ///     "initial size": If database is new, initialize with allocated space - support KB, MB, GB (default: 0)
    /// </summary>
    public long InitialSize { get; set; }

    /// <summary>
    ///     "readonly": Open datafile in readonly mode (default: false)
    /// </summary>
    public bool ReadOnly { get; set; }

    /// <summary>
    ///     "upgrade": Check if data file is an old version and convert before open (default: false)
    /// </summary>
    public bool Upgrade { get; set; }

    /// <summary>
    ///     "auto-rebuild": If last close database exception result a invalid data state, rebuild datafile on next open
    ///     (default: false)
    /// </summary>
    public bool AutoRebuild { get; set; }

    /// <summary>
    ///     "collation": Set default collaction when database creation (default: "[CurrentCulture]/IgnoreCase")
    /// </summary>
    public Collation Collation { get; set; }

    /// <summary>
    ///     Initialize empty connection string
    /// </summary>
    public ConnectionString()
    {
        _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Initialize connection string parsing string in "key1=value1;key2=value2;...." format or only "filename" as default
    ///     (when no ; char found)
    /// </summary>
    public ConnectionString(string connectionString)
        : this()
    {
        if (string.IsNullOrEmpty(connectionString))
            throw new ArgumentNullException(nameof(connectionString));

        // create a dictionary from string name=value collection
        if (connectionString.Contains("="))
        {
            _values.ParseKeyValue(connectionString);
        }
        else
        {
            _values["filename"] = connectionString;
        }

        // setting values to properties
        Connection = _values.GetValue("connection", Connection);
        Filename = _values.GetValue("filename", Filename).Trim();

        Password = _values.GetValue("password", Password);

        if (Password == string.Empty)
        {
            Password = null;
        }

        InitialSize = _values.GetFileSize(@"initial size", InitialSize);
        ReadOnly = _values.GetValue("readonly", ReadOnly);

        Collation = _values.ContainsKey("collation") ? new Collation(_values.GetValue<string>("collation")) : Collation;

        Upgrade = _values.GetValue("upgrade", Upgrade);
        AutoRebuild = _values.GetValue("auto-rebuild", AutoRebuild);
    }

    /// <summary>
    ///     Get value from parsed connection string. Returns null if not found
    /// </summary>
    public string this[string key] => _values.GetOrDefault(key);

    /// <summary>
    ///     Create ILiteEngine instance according string connection parameters. For now, only Local/Shared are supported
    /// </summary>
    internal ILiteEngine CreateEngine()
    {
        var settings = new EngineSettings
        {
            Filename = Filename,
            Password = Password,
            InitialSize = InitialSize,
            ReadOnly = ReadOnly,
            Collation = Collation,
            Upgrade = Upgrade,
            AutoRebuild = AutoRebuild,
        };

        // create engine implementation as Connection Type
        if (Connection == ConnectionType.Direct)
        {
            return new LiteEngine(settings);
        }

        if (Connection == ConnectionType.Shared)
        {
            return new SharedEngine(settings);
        }

        throw new NotImplementedException();
    }
}