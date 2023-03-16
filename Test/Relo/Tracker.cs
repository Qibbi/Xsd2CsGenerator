using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Relo;

// TODO: THIS NEEDS TESTING!
public sealed class Tracker : IDisposable
{
    internal readonly struct Bookmark
    {
        public readonly int Index;
        public readonly int From;
        public readonly int To;

        public Bookmark(int index, int from, int to)
        {
            Index = index;
            From = from;
            To = to;
        }

        public override string ToString()
        {
            return $"Bookmark [{Index}] From: 0x{From:X08} To: 0x{To:X08}";
        }
    }

    internal readonly struct Block
    {
        private readonly byte[] _data;

        public Memory<byte> Data => _data;

        public Block(uint size)
        {
            _data = new byte[size];
        }
    }

    public ref struct Context
    {
        private Tracker? _parent;

        internal Context(Tracker? parent)
        {
            _parent = parent;
        }

        public void Dispose()
        {
            if (_parent is not null)
            {
                _parent.Pop();
                _parent = null;
            }
        }
    }

    public static Tracker NullTracker { get; } = new Tracker();

    private uint _instanceBufferSize;
    private readonly System.Collections.Generic.List<int> _stack = new();
    private readonly System.Collections.Generic.List<Block> _blocks = new();
    private readonly System.Collections.Generic.List<Bookmark> _relocations = new();
    private readonly System.Collections.Generic.List<Bookmark> _imports = new();

    public bool IsBigEndian { get; }

    private Tracker()
    {
    }

    public Tracker(ref Memory<byte> rootPointer, uint size, bool isBigEndian)
    {
        IsBigEndian = isBigEndian;
        int index = Allocate(1u, size);
        _stack.Add(index);
        rootPointer = _blocks[index].Data;
    }

    private static bool BookmarkCompare(in Bookmark x, in Bookmark y)
    {
        return x.Index < y.Index || (x.Index <= y.Index && x.To < y.To);
    }

    private int Allocate(uint count, uint elementSize)
    {
        uint blockSize = (uint)(((count * elementSize) + 3) & -4);
        Block block = new(blockSize);
        _blocks.Add(block);
        _instanceBufferSize += blockSize;
        return _blocks.Count - 1;
    }

    public Context Push<T>(ref T pointerLocation, uint objectSize, uint objectCount)
    {
        if (Unsafe.IsNullRef(ref pointerLocation))
        {
            return new Context(null);
        }
        int index = _stack[^1];
        int newIndex = Allocate(objectCount, objectSize);
        _stack.Add(index);
        Bookmark bookmark = new(index, (int)Unsafe.ByteOffset(ref MemoryMarshal.GetReference(_blocks[index].Data.Span), ref Unsafe.As<T, byte>(ref pointerLocation)), newIndex);
        Unsafe.As<T, uint>(ref pointerLocation) = MemoryMarshal.GetReference(_blocks[newIndex].Data.Span);
        _relocations.Add(bookmark);
        return new Context(this);
    }

    public void Pop()
    {
        _stack.RemoveAt(_stack.Count - 1);
    }

    public void AddReference<T>(ref T location, int value)
    {
        int index = _stack[^1];
        Bookmark bookmark = new(index, (int)Unsafe.ByteOffset(ref MemoryMarshal.GetReference(_blocks[index].Data.Span), ref Unsafe.As<T, byte>(ref location)), value);
        Unsafe.As<T, int>(ref location) = value;
        _imports.Add(bookmark);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InplaceEndianToPlatform(ref short value)
    {
        if (IsBigEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InplaceEndianToPlatform(ref ushort value)
    {
        if (IsBigEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InplaceEndianToPlatform(ref int value)
    {
        if (IsBigEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void InplaceEndianToPlatform(ref uint value)
    {
        if (IsBigEndian)
        {
            value = BinaryPrimitives.ReverseEndianness(value);
        }
    }

    public void MakeRelocatable(Chunk chunk)
    {
        uint importsBufferSize = 0;
        if (_imports.Count > 0)
        {
            importsBufferSize = (uint)((_imports.Count + 1u) * 4u);
        }
        uint relocationBufferSize = 0;
        if (_relocations.Count > 0)
        {
            relocationBufferSize = (uint)((_relocations.Count + 1u) * 4u);
        }
        chunk.Allocate(_instanceBufferSize, relocationBufferSize, importsBufferSize);
        Span<byte> instanceBuffer = chunk.InstanceBuffer;
        int blockCount = _blocks.Count;
        int[] bookmarks = new int[blockCount];
        int idx = 0;
        foreach (Block block in _blocks)
        {
            block.Data.Span.CopyTo(instanceBuffer);
            bookmarks[idx++] = chunk.InstanceBuffer.Length - instanceBuffer.Length;
            instanceBuffer = instanceBuffer[block.Data.Length..];
        }
        instanceBuffer = chunk.InstanceBuffer;
        if (relocationBufferSize > 0)
        {
            _relocations.Sort(new Comparison<Bookmark>((x, y) => BookmarkCompare(x, y) ? -1 : 1));
            Span<int> relocationBuffer = MemoryMarshal.Cast<byte, int>(chunk.RelocationBuffer);
            foreach (Bookmark relocation in _relocations)
            {
                int from = bookmarks[relocation.Index] + relocation.From;
                relocationBuffer[0] = from;
                InplaceEndianToPlatform(ref MemoryMarshal.GetReference(relocationBuffer));
                int to = bookmarks[relocation.To];
                InplaceEndianToPlatform(ref to);
                Unsafe.As<byte, int>(ref Unsafe.Add(ref MemoryMarshal.GetReference(instanceBuffer), from)) = to;
                relocationBuffer = relocationBuffer[1..];
            }
            relocationBuffer[0] = -1;
        }
        if (importsBufferSize > 0)
        {
            _imports.Sort(new Comparison<Bookmark>((x, y) => BookmarkCompare(x, y) ? -1 : 1));
            Span<int> importsBuffer = MemoryMarshal.Cast<byte, int>(chunk.ImportsBuffer);
            foreach (Bookmark import in _imports)
            {
                int from = bookmarks[import.Index] + import.From;
                importsBuffer[0] = from;
                InplaceEndianToPlatform(ref MemoryMarshal.GetReference(importsBuffer));
                int to = import.To;
                InplaceEndianToPlatform(ref to);
                Unsafe.As<byte, int>(ref Unsafe.Add(ref MemoryMarshal.GetReference(instanceBuffer), from)) = to;
                importsBuffer = importsBuffer[1..];
            }
            importsBuffer[0] = -1;
        }
    }

    public void Dispose()
    {
        _imports.Clear();
        _relocations.Clear();
        _blocks.Clear();
        _stack.Clear();
    }
}
