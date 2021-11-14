using System;

namespace Ymfm
{
    /// <summary>
    /// Holds data that is computed once at the start of clocking  and remains
    /// static during subsequent sound generation.
    /// </summary>
    public unsafe struct OpDataCache
    {
        // set phase_step to this value to recalculate it each sample; needed
        // in the case of PM LFO changes
        public const uint PhaseStepDynamic = 1;

        /// <summary>
        /// base of sine table
        /// </summary>
        public ReadOnlyMemory<ushort> Waveform;

        /// <summary>
        /// phase step, or <see cref="PhaseStepDynamic"/> if PM is active
        /// </summary>
        public uint PhaseStep;

        /// <summary>
        /// total level * 8 + KSL
        /// </summary>
        public uint TotalLevel;

        /// <summary>
        /// raw block frequency value (used to compute phase_step)
        /// </summary>
        public uint BlockFreq;

        // detuning value (used to compute phase_step)
        public int Detune;

        // multiple value (x.1, used to compute phase_step)
        public uint Multiple;

        // sustain level, shifted up to envelope values
        public uint EgSustain;

        private fixed byte _egRate[(int)EnvelopeState.States];

        /// <summary>
        /// envelope rate, including KSR
        /// </summary>
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
