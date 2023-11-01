namespace LiteDB.Engine;

/// <summary>
/// First initial data structure at start of disk. 
/// All information data here are immutable. Only flag controls are changed (IsDisposed)
/// </summary>
internal class FileHeader
{
    /// <summary>
    /// Header info the validate that datafile is a LiteDB file (27 bytes)
    /// </summary>
    public const string HEADER_INFO = "** This is a LiteDB file **";

    /// <summary>
    /// Datafile specification version
    /// </summary>
    public const byte FILE_VERSION = 9;

    #region Buffer Field Positions

    public const int P_HEADER_INFO = 0;  // 0-26 [string(27)]
    public const int P_FILE_VERSION = 27; // 27-27 [byte]

    public const int P_ENCRYPTED = 28; // 28-28 [byte]
    public const int P_ENCRYPTION_SALT = 29; // 29-44 [guid]

    public const int P_INSTANCE_ID = 45; // 45-60 [guid]
    public const int P_CREATION_TIME = 61; // 61-68 [datetime-long]
    public const int P_COLLATION_LCID = 69; // 69-72 [int]
    public const int P_COLLATION_OPTS = 73; // 73-77 [int]
    public const int P_ENGINE_VER_MAJOR = 77; // 77-79 [byte "6.*.*"]
    public const int P_ENGINE_VER_MINOR = 78; // 77-79 [byte "*.1.*"]
    public const int P_ENGINE_VER_BUILD = 79; // 77-79 [byte "*.*.4"]

    // reserved 80-97 (18 bytes)

    public const int P_IS_DIRTY = 95; // [byte]

    #endregion

    private readonly string _headerInfo;
    private readonly byte _fileVersion;

    public readonly bool Encrypted;
    public readonly byte[] EncryptionSalt;

    public readonly Guid InstanceID;
    public readonly DateTime CreationTime;
    public readonly Collation Collation;
    public readonly Version EngineVersion;

    public bool IsDirty;

    /// <summary>
    /// Create empty version of file header
    /// </summary>
    public FileHeader()
    {
        _headerInfo = "";
        _fileVersion = 0;

        this.Encrypted = false;
        this.EncryptionSalt = Array.Empty<byte>();
        this.InstanceID = Guid.Empty;
        this.CreationTime = DateTime.MinValue;
        this.Collation = Collation.Default;
        this.EngineVersion = new Version();
        this.IsDirty = false;
    }

    /// <summary>
    /// Read file header from a existing buffer data
    /// </summary>
    public FileHeader(Span<byte> buffer)
    {
        _headerInfo = buffer[P_HEADER_INFO..(P_HEADER_INFO + HEADER_INFO.Length)].ReadFixedString();
        _fileVersion = buffer[P_FILE_VERSION];

        this.Encrypted = buffer[P_ENCRYPTED] == 1;
        this.EncryptionSalt = buffer[P_ENCRYPTION_SALT..(P_ENCRYPTION_SALT + ENCRYPTION_SALT_SIZE)].ToArray();

        this.InstanceID = buffer[P_INSTANCE_ID..].ReadGuid();
        this.CreationTime = buffer[P_CREATION_TIME..].ReadDateTime();

        var lcid = buffer[P_COLLATION_LCID..].ReadInt32();
        var opts = buffer[P_COLLATION_OPTS..].ReadInt32();

        this.Collation = new Collation(lcid, (CompareOptions)opts);

        var major = buffer[P_ENGINE_VER_MAJOR];
        var minor = buffer[P_ENGINE_VER_MINOR];
        var build = buffer[P_ENGINE_VER_BUILD];

        this.EngineVersion = new Version(major, minor, build);

        this.IsDirty = buffer[P_IS_DIRTY] != 0;
    }

    /// <summary>
    /// Create a new file header structure and write direct on buffer
    /// </summary>
    public FileHeader(IEngineSettings settings)
    {
        _headerInfo = HEADER_INFO;
        _fileVersion = FILE_VERSION;

        this.Encrypted = settings.Password is not null;
        this.EncryptionSalt = this.Encrypted ? AesStream.NewSalt() : new byte[ENCRYPTION_SALT_SIZE];

        this.InstanceID = Guid.NewGuid();
        this.CreationTime = DateTime.UtcNow;
        this.Collation = settings.Collation;
        this.EngineVersion = typeof(LiteEngine).Assembly.GetName().Version;

        this.IsDirty = false;
    }

    /// <summary>
    /// Convert header variables into a new array
    /// </summary>
    public byte[] ToArray()
    {
        var array = new byte[FILE_HEADER_SIZE];

        // get buffer
        var buffer = array.AsSpan();

        // write flags/data into file header buffer
        buffer[P_HEADER_INFO..].WriteFixedString(HEADER_INFO);
        buffer[P_FILE_VERSION] = FILE_VERSION;

        buffer[P_ENCRYPTED] = this.Encrypted ? (byte)1 : (byte)0;
        buffer[P_ENCRYPTION_SALT..].WriteBytes(this.EncryptionSalt);

        buffer[P_INSTANCE_ID..].WriteGuid(this.InstanceID);
        buffer[P_CREATION_TIME..].WriteDateTime(this.CreationTime);
        buffer[P_COLLATION_LCID..].WriteInt32(this.Collation.Culture.LCID);
        buffer[P_COLLATION_OPTS..].WriteInt32((int)this.Collation.CompareOptions);
        buffer[P_ENGINE_VER_MAJOR] = (byte)this.EngineVersion.Major;
        buffer[P_ENGINE_VER_MINOR] = (byte)this.EngineVersion.Minor;
        buffer[P_ENGINE_VER_BUILD] = (byte)this.EngineVersion.Build;

        return array;
    }

    public void ValidateFileHeader()
    {
        if (_headerInfo != HEADER_INFO)
            throw ERR_INVALID_DATABASE();

        if (_fileVersion != FILE_VERSION)
            throw ERR_INVALID_FILE_VERSION();
    }

    public override string ToString()
    {
        return Dump.Object(this);
    }

}
