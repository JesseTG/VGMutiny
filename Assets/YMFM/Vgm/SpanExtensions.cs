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

        public static uint ReadUInt32BigEndian(this ReadOnlySpan<byte> span, uint offset)
        {
            return BinaryPrimitives.ReadUInt32BigEndian(span[(int)offset..(int)(offset + sizeof(int))]);
        }

        public static ushort ReadUInt16LittleEndian(this ReadOnlySpan<byte> span, uint offset)
        {
            return BinaryPrimitives.ReadUInt16LittleEndian(span[(int)offset..(int)(offset + sizeof(ushort))]);
        }

        public static ReadOnlySpan<char> NextNullTerminatedString(this ReadOnlySpan<char> span, out string result)
        {
            var nullIndex = span.IndexOf('\0');

            result = nullIndex switch
            {
                0 => string.Empty,
                -1 => new string(span),
                _ => new string(span[..nullIndex]),
            };

            return span[(nullIndex + 1)..];
        }

        /// <summary>
        /// Reads a string that might or might not be null-terminated
        /// </summary>
        /// <param name="span">The span</param>
        /// <param name="result"></param>
        /// <param name="nullIndex">The index of the null terminator, or -1 if none</param>
        public static void ReadNullTerminatedString(this ReadOnlySpan<char> span, out string result, out int nullIndex)
        {
            nullIndex = span.IndexOf('\0');

            result = nullIndex switch
            {
                0 => string.Empty,
                -1 => new string(span),
                _ => new string(span[..nullIndex]),
            };
        }
    }
}
