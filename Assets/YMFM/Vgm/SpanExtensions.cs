using System;
using System.Buffers.Binary;

namespace Ymfm.Vgm
{
    public static class SpanExtensions
    {
        public static uint ReadUInt32LittleEndian(this ReadOnlySpan<byte> span, uint offset)
        {
            return BinaryPrimitives.ReadUInt32LittleEndian(span[(int)offset..(int)(offset + sizeof(int))]);
        }

        public static ushort ReadUInt16LittleEndian(this ReadOnlySpan<byte> span, uint offset)
        {
            return BinaryPrimitives.ReadUInt16LittleEndian(span[(int)offset..(int)(offset + sizeof(ushort))]);
        }
    }
}
