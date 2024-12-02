using System.Buffers;

namespace Myitian.LibNCM;

public class PooledArray<T> : IDisposable
{
    public T[] UnderlyingArray { get; private set; }
    public Memory<T> Memory { get; private set; }
    public int Length { get; private set; }
    public int Capacity => UnderlyingArray.Length;
    public Span<T> Span => Memory.Span;

    public T this[int index]
    {
        get => UnderlyingArray[index];
        set => UnderlyingArray[index] = value;
    }

    public PooledArray(int length)
    {
        UnderlyingArray = ArrayPool<T>.Shared.Rent(length);
        try
        {
            Memory = UnderlyingArray.AsMemory(0, length);
            Length = length;
        }
        finally
        {
            Dispose();
        }
    }

    public void Dispose()
    {
        ArrayPool<T>.Shared.Return(UnderlyingArray);
        GC.SuppressFinalize(this);
    }

    public static implicit operator Memory<T>(PooledArray<T> arr) => arr.Memory;
    public static implicit operator ReadOnlyMemory<T>(PooledArray<T> arr) => arr.Memory;
    public static implicit operator Span<T>(PooledArray<T> arr) => arr.Span;
    public static implicit operator ReadOnlySpan<T>(PooledArray<T> arr) => arr.Span;
}

