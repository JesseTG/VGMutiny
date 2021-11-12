using System.IO;

namespace Ymfm.Vgm
{
    public static class StreamExtensions
    {
        public static bool TryReadVgmHeader(this BinaryReader reader, out VgmHeader header)
        {
            header = default;
            return false;
        }
    }
}
