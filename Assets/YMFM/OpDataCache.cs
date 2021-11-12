using System;

namespace Ymfm
{
    // this class holds data that is computed once at the start of clocking
    // and remains static during subsequent sound generation
    public unsafe struct OpDataCache
    {
        // set phase_step to this value to recalculate it each sample; needed
        // in the case of PM LFO changes
        public const uint PhaseStepDynamic = 1;

        // base of sine table
        public ReadOnlyMemory<ushort> Waveform;

        // phase step, or PHASE_STEP_DYNAMIC if PM is active
        public uint PhaseStep;

        // total level * 8 + KSL
        public uint TotalLevel;

        // raw block frequency value (used to compute phase_step)
        public uint BlockFreq;

        // detuning value (used to compute phase_step)
        public int Detune;

        // multiple value (x.1, used to compute phase_step)
        public uint Multiple;

        // sustain level, shifted up to envelope values
        public uint EgSustain;

        // envelope rate, including KSR
        private fixed byte _egRate[(int)EnvelopeState.States];

        public readonly Span<byte> EgRate
        {
            get
            {
                fixed (byte* ptr = _egRate)
                {
                    return new Span<byte>(ptr, (int)EnvelopeState.States);
                }
            }
        }

        // envelope shift amount
        public byte EgShift;
    };
}
