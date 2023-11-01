namespace LiteDB;

/// <summary>
/// Represent a 12-bytes BSON type used in document Id
/// </summary>
public struct ObjectId : IComparable<ObjectId>, IEquatable<ObjectId>
{
    public readonly int Timestamp;
    public readonly int Machine;
    public readonly short Pid;
    public readonly int Increment;

    public DateTime CreationTime => BsonDateTime.UnixEpoch.AddSeconds(this.Timestamp);

    public static ObjectId Empty = new ();

    #region Ctor

    /// <summary>
    /// Initializes a new empty instance of the ObjectId class.
    /// </summary>
    public ObjectId()
    {
        this.Timestamp = 0;
        this.Machine = 0;
        this.Pid = 0;
        this.Increment = 0;
    }

    /// <summary>
    /// Initializes a new instance of the ObjectId class from ObjectId vars.
    /// </summary>
    public ObjectId(int timestamp, int machine, short pid, int increment)
    {
        this.Timestamp = timestamp;
        this.Machine = machine;
        this.Pid = pid;
        this.Increment = increment;
    }

    /// <summary>
    /// Initializes a new instance of ObjectId class from another ObjectId.
    /// </summary>
    public ObjectId(ObjectId from)
    {
        this.Timestamp = from.Timestamp;
        this.Machine = from.Machine;
        this.Pid = from.Pid;
        this.Increment = from.Increment;
    }

    /// <summary>
    /// Initializes a new instance of the ObjectId class from hex string.
    /// </summary>
    public ObjectId(string value)
        : this(FromHex(value))
    {
    }

    /// <summary>
    /// Initializes a new instance of the ObjectId class from byte array.
    /// </summary>
    public ObjectId(Span<byte> span)
    {
        this.Timestamp =
            (span[0] << 24) + 
            (span[1] << 16) + 
            (span[2] << 8) + 
            span[3];

        this.Machine = 
            (span[4] << 16) + 
            (span[5] << 8) + 
            span[6];

        this.Pid = (short)
            ((span[7] << 8) + 
            span[8]);

        this.Increment = 
            (span[9] << 16) + 
            (span[10] << 8) + 
            span[11];
    }

    /// <summary>
    /// Convert hex value string in byte array
    /// </summary>
    private static byte[] FromHex(string value)
    {
        if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));
        if (value.Length != 24) throw new ArgumentException(string.Format("ObjectId strings should be 24 hex characters, got {0} : \"{1}\"", value.Length, value));

        var bytes = new byte[12];

        for (var i = 0; i < 24; i += 2)
        {
            bytes[i / 2] = Convert.ToByte(value.Substring(i, 2), 16);
        }

        return bytes;
    }

    #endregion

    #region Equals/CompareTo/ToString

    /// <summary>
    /// Checks if this ObjectId is equal to the given object. Returns true
    /// if the given object is equal to the value of this instance. 
    /// Returns false otherwise.
    /// </summary>
    public bool Equals(ObjectId other)
    {
        return 
            this.Timestamp == other.Timestamp &&
            this.Machine == other.Machine &&
            this.Pid == other.Pid &&
            this.Increment == other.Increment;
    }

    /// <summary>
    /// Determines whether the specified object is equal to this instance.
    /// </summary>
    public override bool Equals(object other)
    {
        return Equals((ObjectId)other);
    }

    /// <summary>
    /// Returns a hash code for this instance.
    /// </summary>
    public override int GetHashCode() => HashCode.Combine(Timestamp, Machine, Pid, Increment);

    /// <summary>
    /// Compares two instances of ObjectId
    /// </summary>
    public int CompareTo(ObjectId other)
    {
        var r = this.Timestamp.CompareTo(other.Timestamp);
        if (r != 0) return r;

        r = this.Machine.CompareTo(other.Machine);
        if (r != 0) return r;

        r = this.Pid.CompareTo(other.Pid);
        if (r != 0) return r < 0 ? -1 : 1;

        return this.Increment.CompareTo(other.Increment);
    }

    /// <summary>
    /// Represent ObjectId as 12 bytes array
    /// </summary>
    public bool TryWriteBytes(Span<byte> span)
    {
        if (span.Length < 12) return false;

        span[0] = (byte)(this.Timestamp >> 24);
        span[1] = (byte)(this.Timestamp >> 16);
        span[2] = (byte)(this.Timestamp >> 8);
        span[3] = (byte)(this.Timestamp);
        span[4] = (byte)(this.Machine >> 16);
        span[5] = (byte)(this.Machine >> 8);
        span[6] = (byte)(this.Machine);
        span[7] = (byte)(this.Pid >> 8);
        span[8] = (byte)(this.Pid);
        span[9] = (byte)(this.Increment >> 16);
        span[10] = (byte)(this.Increment >> 8);
        span[11] = (byte)(this.Increment);

        return true;
    }

    public byte[] ToByteArray()
    {
        var bytes = new byte[12];

        this.TryWriteBytes(bytes.AsSpan());

        return bytes;
    }

    public override string ToString()
    {
        var sb = StringBuilderCache.Acquire();
        var bytes = this.ToByteArray();

        foreach(var b in bytes)
        {
            sb.AppendFormat("{0:x2}", b);
        }

        return StringBuilderCache.Release(sb);
    }

    #endregion

    #region Operators

    public static bool operator ==(ObjectId left, ObjectId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ObjectId left, ObjectId right)
    {
        return !(left == right);
    }

    public static bool operator >=(ObjectId left, ObjectId right)
    {
        return left.CompareTo(right) >= 0;
    }

    public static bool operator >(ObjectId left, ObjectId right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <(ObjectId left, ObjectId right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(ObjectId left, ObjectId right)
    {
        return left.CompareTo(right) <= 0;
    }

    #endregion

    #region Static methods

    private static int _machine;
    private static short _pid;
    private static int _increment;

    // static constructor
    static ObjectId()
    {
        _increment = Randomizer.Next();

        try
        {
            _machine = (GetMachineHash() +
                AppDomain.CurrentDomain.Id)
                & 0x00ffffff;

            _pid = (short)GetCurrentProcessId();
        }
        catch (Exception)
        {
            _machine = Randomizer.Next();
            _pid = (short)Randomizer.Next(1, 10000);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int GetCurrentProcessId()
    {
        return Process.GetCurrentProcess().Id;
    }

    private static int GetMachineHash()
    {
        var hostName = Environment.MachineName; // use instead of Dns.HostName so it will work offline

        return 0x00ffffff & hostName.GetHashCode(); // use first 3 bytes of hash
    }

    /// <summary>
    /// Creates a new ObjectId.
    /// </summary>
    public static ObjectId NewObjectId()
    {
        var timestamp = (long)Math.Floor((DateTime.UtcNow - BsonDateTime.UnixEpoch).TotalSeconds);
        var inc = Interlocked.Increment(ref _increment) & 0x00ffffff;

        return new ((int)timestamp, _machine, _pid, inc);
    }

    #endregion
}
