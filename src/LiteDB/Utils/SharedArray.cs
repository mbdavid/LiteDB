namespace LiteDB;

/// <summary>
/// A shared byte array to rent and return on dispose
/// </summary>
public readonly struct SharedArray<T> : IIsEmpty, IDisposable
{
    private readonly T[] _array;
    private readonly int _length;

    public static SharedArray<T> Empty = new();

    public bool IsEmpty => _length == 0;

    public SharedArray()
    {
        _array = [];
        _length = 0;
    }

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

    public int Length => _length;

    public readonly Span<T> AsSpan() => _array.AsSpan(0, _length);

    public readonly Span<T> AsSpan(int start) => _array.AsSpan(start);

    public readonly Span<T> AsSpan(int start, int length) => _array.AsSpan(start, length);

    public readonly Memory<T> AsMemory() => _array.AsMemory(0, _length);

    public readonly Memory<T> AsMemory(int start) => _array.AsMemory(start);

    public readonly Memory<T> AsMemory(int start, int length) => _array.AsMemory(start, length);

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