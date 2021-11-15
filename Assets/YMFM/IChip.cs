using System;
using JetBrains.Annotations;

namespace Ymfm
{
    public interface IChip
    {
        //-------------------------------------------------
        //  reset - reset the system
        //-------------------------------------------------
        public void Reset();


        [Pure]
        public uint SampleRate(uint inputClock);

        //-------------------------------------------------
        //  write - handle a write to the register
        //  interface
        //-------------------------------------------------
        public void Write(uint offset, byte data);

        public void Generate(Span<int> output);

        public uint Outputs { get; }
    }
}
