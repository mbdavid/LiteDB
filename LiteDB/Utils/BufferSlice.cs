using static LiteDB.Constants;

namespace LiteDB;

using System;
using System.Text;

/// <summary>
///     Internal class that implement same idea from ArraySegment[byte] but use a class (not a struct). Works for byte[]
///     only
/// </summary>
internal class BufferSlice
{
    public int Offset { get; }
    public int Count { get; }
    public byte[] Array { get; }

    public BufferSlice(byte[] array, int offset, int count)
    {
        Array = array;
        Offset = offset;
        Count = count;
    }

    public byte this[int index]
    {
        get => Array[Offset + index];
        set => Array[Offset + index] = value;
    }

    /// <summary>
    ///     Clear all page content byte array (not controls)
    /// </summary>
    public void Clear()
    {
        System.Array.Clear(Array, Offset, Count);
    }

    /// <summary>
    ///     Clear page content byte array
    /// </summary>
    public void Clear(int offset, int count)
    {
        ENSURE(offset + count <= Count, "must fit in this page");

        System.Array.Clear(Array, Offset + offset, count);
    }

    /// <summary>
    ///     Fill all content with value. Used for DEBUG propost
    /// </summary>
    public void Fill(byte value)
    {
        for (var i = 0; i < Count; i++)
        {
            Array[Offset + i] = value;
        }
    }

    /// <summary>
    ///     Checks if all values contains only value parameter (used for DEBUG)
    /// </summary>
    public bool All(byte value)
    {
        for (var i = 0; i < Count; i++)
        {
            if (Array[Offset + i] != value)
                return false;
        }

        return true;
    }

    /// <summary>
    ///     Return byte[] slice into hex digits
    /// </summary>
    public string ToHex()
    {
        var output = new StringBuilder();
        var position = 0L;

        while (position < Count)
        {
            //output.Append(position.ToString("X3") + "  ");

            for (var i = 0; i < 32 && position < Count; i++)
            {
                output.Append(Array[Offset + position].ToString("X2") + " ");

                position++;
            }

            output.AppendLine();
        }

        return output.ToString();
    }

    /// <summary>
    ///     Slice this buffer into new BufferSlice according new offset and new count
    /// </summary>
    public BufferSlice Slice(int offset, int count)
    {
        return new BufferSlice(Array, Offset + offset, count);
    }

    /// <summary>
    ///     Convert this buffer slice into new byte[]
    /// </summary>
    public byte[] ToArray()
    {
        var buffer = new byte[Count];

        Buffer.BlockCopy(Array, Offset, buffer, 0, Count);

        return buffer;
    }

    public override string ToString()
    {
        return $"Offset: {Offset} - Count: {Count}";
    }
}