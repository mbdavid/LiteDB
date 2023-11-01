namespace LiteDB;

/// <summary>
/// A shared byte array to rent and return on dispose
/// </summary>
public readonly struct SharedArray<T> : IDisposable
{
    private readonly T[] _array;
    private readonly int _length;

    private SharedArray(T[] array, int length)
    {
        _array = array;
        _length = length;
    }

    public T this[int index]
    {
        get => _array[index];
        set => _array[index] = value;
    }

    public readonly Span<T> AsSpan() => _array.AsSpan(0, _length);

    public readonly Span<T> AsSpan(int start) => _array.AsSpan(start);

    public readonly Span<T> AsSpan(int start, int length) => _array.AsSpan(start, length);

    public static SharedArray<T> Rent(int length)
    {
        ENSURE(length < int.MaxValue, new { length });

        var array = ArrayPool<T>.Shared.Rent(length);

        return new (array, length);
    }

    public void Dispose()
    {
        ArrayPool<T>.Shared.Return(_array);
    }

    public override string ToString()
    {
        return Dump.Object(new { _length });
    }
}