using System;

namespace YMFM.Vgm
{
    public struct VgmDataBlock
    {
        public byte Type;

        public uint Length;

        public ReadOnlyMemory<byte> Data;
    }
}
