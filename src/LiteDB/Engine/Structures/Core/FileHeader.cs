namespace LiteDB.Engine;

/// <summary>
/// First initial data structure at start of disk. 
/// All information data here are immutable and initialize when database are created and never changes
/// </summary>
internal class FileHeader
{
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
    public const int P_IS_LITTLE_ENDIAN = 80; // 80-80 [bool]

    // reserved 81-96

    #endregion

    private readonly string _headerInfo = "";
    private readonly byte _fileVersion = 0;

    public readonly bool IsEncrypted = false;
    public readonly byte[] EncryptionSalt = Array.Empty<byte>();

    public readonly Guid InstanceID = Guid.Empty;
    public readonly DateTime CreationTime = DateTime.MinValue;
    public readonly Collation Collation = Collation.Binary;
    public readonly Version EngineVersion = new();
    public readonly bool IsLittleEndian = true;

    public FileHeader()
    {
    }

    /// <summary>
    /// Read file header from a existing buffer data
    /// </summary>
    public FileHeader(Span<byte> buffer)
    {
        _headerInfo = buffer[P_HEADER_INFO..(P_HEADER_INFO + HEADER_INFO.Length)].ReadFixedString();
        _fileVersion = buffer[P_FILE_VERSION];

        this.IsEncrypted = buffer[P_ENCRYPTED] == 1;
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
        this.IsLittleEndian = buffer[P_IS_LITTLE_ENDIAN] == 1;
    }

    /// <summary>
    /// Create a new file header structure and write direct on buffer
    /// </summary>
    public FileHeader(IEngineSettings settings)
    {
        _headerInfo = HEADER_INFO;
        _fileVersion = FILE_VERSION;

        this.IsEncrypted = settings.Password is not null;
        this.EncryptionSalt = this.IsEncrypted ? AesStream.NewSalt() : new byte[ENCRYPTION_SALT_SIZE];

        this.InstanceID = Guid.NewGuid();
        this.CreationTime = DateTime.UtcNow;
        this.Collation = settings.Collation;
        this.EngineVersion = typeof(LiteEngine).Assembly.GetName().Version!;
        this.IsLittleEndian = BitConverter.IsLittleEndian;
    }

    /// <summary>
    /// Convert header variables into a new array
    /// </summary>
    public void Write(Span<byte> buffer)
    {
        // write flags/data into file header buffer
        buffer[P_HEADER_INFO..].WriteFixedString(HEADER_INFO);
        buffer[P_FILE_VERSION] = FILE_VERSION;

        buffer[P_ENCRYPTED] = this.IsEncrypted ? (byte)1 : (byte)0;
        buffer[P_ENCRYPTION_SALT..].WriteBytes(this.EncryptionSalt);

        buffer[P_INSTANCE_ID..].WriteGuid(this.InstanceID);
        buffer[P_CREATION_TIME..].WriteDateTime(this.CreationTime);
        buffer[P_COLLATION_LCID..].WriteInt32(this.Collation.Culture.LCID);
        buffer[P_COLLATION_OPTS..].WriteInt32((int)this.Collation.CompareOptions);
        buffer[P_ENGINE_VER_MAJOR] = (byte)this.EngineVersion.Major;
        buffer[P_ENGINE_VER_MINOR] = (byte)this.EngineVersion.Minor;
        buffer[P_ENGINE_VER_BUILD] = (byte)this.EngineVersion.Build;
    }

    public void ValidateFileHeader()
    {
        if (_headerInfo != HEADER_INFO) throw ERR_INVALID_DATABASE();

        if (_fileVersion != FILE_VERSION) throw ERR_INVALID_FILE_VERSION();

        if (this.IsLittleEndian != BitConverter.IsLittleEndian) throw ERR("Different original file architecture and current running architecture byte order (little/big endian)");
    }

    public override string ToString()
    {
        return Dump.Object(new { IsEncrypted, InstanceID, Collation });
    }

}
