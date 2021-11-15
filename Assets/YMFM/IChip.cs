using System;
using JetBrains.Annotations;

namespace Ymfm
{
    public interface IChip<TOutput> where TOutput : unmanaged, IOutput
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

        public unsafe void Generate(ref TOutput output)
        {
            fixed (TOutput* ptr = &output)
            {
                var span = new Span<TOutput>(ptr, 1);
                Generate(span);
            }
        }

        public void Generate(Span<TOutput> output);

        public uint Outputs { get; }
    }
}
