using System;
using System.Buffers.Binary;
using System.Text;

namespace OpenRGB.NET.Utils
{
#if DEBUG
    [NonCopyable]
#endif
    internal ref struct SpanReader
    {
        public ReadOnlySpan<byte> Span { get; }
        public int Position { get; private set; }

        public SpanReader(in ReadOnlySpan<byte> span)
        {
            Span = span;
            Position = 0;
        }

        public ushort ReadUInt16()
        {
            var value = BinaryPrimitives.ReadUInt16LittleEndian(Span.Slice(Position));
            Position += sizeof(ushort);
            return value;
        }

        public int ReadInt32()
        {
            var value = BinaryPrimitives.ReadInt32LittleEndian(Span.Slice(Position));
            Position += sizeof(int);
            return value;
        }

        public uint ReadUInt32()
        {
            var value = BinaryPrimitives.ReadUInt32LittleEndian(Span.Slice(Position));
            Position += sizeof(uint);
            return value;
        }

        public ReadOnlySpan<byte> ReadBytes(int length)
        {
            var value = Span.Slice(Position, length);
            Position += length;
            return value;
        }

        public byte ReadByte()
        {
            var value = Span[Position];
            Position += sizeof(byte);
            return value;
        }

        public string ReadLengthAndString()
        {
            int length = ReadUInt16();
            var readBytes = ReadBytes(length);
            return Encoding.ASCII.GetString(readBytes.Slice(0, readBytes.Length - 1).ToArray());
        }
    }
}