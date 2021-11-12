using System;
using JetBrains.Annotations;

namespace Ymfm
{
    public interface IChip<TOutput> where TOutput : struct, IOutput
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


        public void Generate(Span<TOutput> output, uint numSamples = 1);

        public uint Outputs { get; }
    }
}
