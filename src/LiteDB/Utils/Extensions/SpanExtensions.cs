namespace LiteDB;

internal static class SpanExtensions
{
    private static readonly IBsonReader _reader = new BsonReader();
    private static readonly IBsonWriter _writer = new BsonWriter();

    #region Read Extensions

    public static Int16 ReadInt16(this Span<byte> span)
    {
        return BinaryPrimitives.ReadInt16LittleEndian(span);
    }

    public static UInt16 ReadUInt16(this Span<byte> span)
    {
        return BinaryPrimitives.ReadUInt16LittleEndian(span);
    }

    public static Int32 ReadInt32(this Span<byte> span)
    {
        return BinaryPrimitives.ReadInt32LittleEndian(span);
    }

    public static UInt32 ReadUInt32(this Span<byte> span)
    {
        return BinaryPrimitives.ReadUInt32LittleEndian(span);
    }

    public static Int64 ReadInt64(this Span<byte> span)
    {
        return BinaryPrimitives.ReadInt64LittleEndian(span);
    }

    public static double ReadDouble(this Span<byte> span)
    {
        return BitConverter.ToDouble(span);
    }

    public static Decimal ReadDecimal(this Span<byte> span)
    {
        var a = span.ReadInt32();
        var b = span[4..].ReadInt32();
        var c = span[8..].ReadInt32();
        var d = span[12..].ReadInt32();
        return new Decimal(new int[] { a, b, c, d });
    }

    public static ObjectId ReadObjectId(this Span<byte> span)
    {
        return new ObjectId(span);
    }

    public static Guid ReadGuid(this Span<byte> span)
    {
        return new Guid(span[..16]);
    }

    public static DateTime ReadDateTime(this Span<byte> span, bool utc = true)
    {
        var ticks = span.ReadInt64();

        if (ticks == 0) return DateTime.MinValue;
        if (ticks == 3155378975999999999) return DateTime.MaxValue;

        var utcDate = new DateTime(ticks, DateTimeKind.Utc);

        return utc ? utcDate : utcDate.ToLocalTime();
    }

    public static RowID ReadRowID(this Span<byte> span)
    {
        return new RowID(span.ReadUInt32(), span[4..].ReadUInt16());
    }

    public static string ReadFixedString(this Span<byte> span)
    {
        return Encoding.UTF8.GetString(span);
    }

    /// <summary>
    /// Read string utf8 inside span using int32 bytes length at start. Returns lengths for all string + 4
    /// </summary>
    public static string ReadVString(this Span<byte> span, out int length)
    {
        var strLength = span.ReadInt32();

        length = strLength + sizeof(int);

        return Encoding.UTF8.GetString(span.Slice(sizeof(int), strLength));
    }

    /// <summary>
    /// Read a variable string byte to byte until find \0. Returns utf8 string and how many bytes (including \0) used on span
    /// </summary>
    public static string ReadCString(this Span<byte> span, out int length)
    {
        var indexOf = span.IndexOf((byte)0);

        if (indexOf == -1) throw new ArgumentException("Not found \\0 in span finish read string");

        length = indexOf + 1;

        return Encoding.UTF8.GetString(span.Slice(0, indexOf));
    }


    /// <summary>
    /// Read a BsonValue from Span using singleton instance of IBsonReader. Used for SortItem
    /// </summary>
    public static BsonValue ReadBsonValue(this Span<byte> span, out int length)
    {
        var result = _reader.ReadValue(span, false, out length)!; // skip = false - always returns a BsonValue

        if (result.Fail) throw result.Exception;

        return result.Value;
    }

    #endregion

    #region Write Extensions

    public static void WriteInt16(this Span<byte> span, Int16 value)
    {
        BinaryPrimitives.WriteInt16LittleEndian(span, value);
    }

    public static void WriteUInt16(this Span<byte> span, UInt16 value)
    {
        BinaryPrimitives.WriteUInt16LittleEndian(span, value);
    }

    public static void WriteInt32(this Span<byte> span, Int32 value)
    {
        BinaryPrimitives.WriteInt32LittleEndian(span, value);
    }

    public static void WriteUInt32(this Span<byte> span, UInt32 value)
    {
        BinaryPrimitives.WriteUInt32LittleEndian(span, value);
    }

    public static void WriteInt64(this Span<byte> span, Int64 value)
    {
        BinaryPrimitives.WriteInt64LittleEndian(span, value);
    }

    public static void WriteDouble(this Span<byte> span, Double value)
    {
        MemoryMarshal.Write(span, ref value);
    }

    public static void WriteDecimal(this Span<byte> buffer, Decimal value)
    {
        var bits = Decimal.GetBits(value);
        buffer.WriteInt32(bits[0]);
        buffer[4..].WriteInt32(bits[1]);
        buffer[8..].WriteInt32(bits[2]);
        buffer[12..].WriteInt32(bits[3]);
    }

    public static void WriteDateTime(this Span<byte> buffer, DateTime value)
    {
        buffer.WriteInt64(value.ToUniversalTime().Ticks);
    }

    public static void WriteRowID(this Span<byte> span, RowID value)
    {
        span.WriteUInt32(value.PageID);
        span[4..].WriteUInt16(value.Index);
        span[6..].WriteUInt16(0);
    }

    public static void WriteGuid(this Span<byte> span, Guid value)
    {
        if (value.TryWriteBytes(span) == false) throw new ArgumentException("Span too small for Guid");
    }

    public static void WriteObjectId(this Span<byte> span, ObjectId value)
    {
        value.TryWriteBytes(span);
    }

    public static void WriteBytes(this Span<byte> span, byte[] value)
    {
        value.CopyTo(span);
    }

    public static void WriteFixedString(this Span<byte> span, string value)
    {
        Encoding.UTF8.GetBytes(value.AsSpan(), span);
    }

    /// <summary>
    /// Write string value initialized with int32 size length. Returns used span length (includes int32 length)
    /// </summary>
    public static void WriteVString(this Span<byte> span, string value, out int length)
    {
        var strLength = Encoding.UTF8.GetByteCount(value);

        span.WriteInt32(strLength);

        Encoding.UTF8.GetBytes(value.AsSpan(), span.Slice(sizeof(int), strLength));

        length = sizeof(int) + strLength;
    }

    /// <summary>
    /// Write full bytes string from utf8 and end's with \0 at end. Returns how many bytes (including this \0 at end) this string used in span
    /// </summary>
    public static void WriteCString(this Span<byte> span, string value, out int length)
    {
        var strLength = Encoding.UTF8.GetByteCount(value);

        Encoding.UTF8.GetBytes(value.AsSpan(), span[..strLength]);

        span[strLength] = 0;

        length = strLength + 1; // for \0
    }

    /// <summary>
    /// Write BsonValue direct into a byte[]. Used for SortItems
    /// </summary>
    public static void WriteBsonValue(this Span<byte> span, BsonValue value, out int length)
    {
        _writer.WriteValue(span, value, out length);
    }

    #endregion

    #region Utils

    public static Span<byte> Slice(this Span<byte> span, PageSegment segment)
    {
        return span.Slice(segment.Location, segment.Length);
    }

    public static unsafe bool IsFullZero(this Span<byte> span)
    {
        fixed (byte* bytes = span)
        {
            int len = span.Length;
            int rem = len % (sizeof(long) * 16);
            long* b = (long*)bytes;
            long* e = (long*)(bytes + len - rem);

            while (b < e)
            {
                if ((*(b) | *(b + 1) | *(b + 2) | *(b + 3) | *(b + 4) |
                    *(b + 5) | *(b + 6) | *(b + 7) | *(b + 8) |
                    *(b + 9) | *(b + 10) | *(b + 11) | *(b + 12) |
                    *(b + 13) | *(b + 14) | *(b + 15)) != 0)
                    return false;
                b += 16;
            }

            for (int i = 0; i < rem; i++)
                if (span[len - 1 - i] != 0)
                    return false;

            return true;
        }
    }

    #endregion
}
