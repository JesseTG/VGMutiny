using System;

namespace Ymfm
{
    /// <summary>
    /// Holds data that is computed once at the start of clocking  and remains
    /// static during subsequent sound generation.
    /// </summary>
    public struct OpDataCache
    {
        /// <summary>
        /// set <see cref="PhaseStep"/> to this value to recalculate it each
        /// sample; needed in the case of PM LFO changes
        /// </summary>
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
        /// raw block frequency value (used to compute <see cref="PhaseStep"/>)
        /// </summary>
        public uint BlockFreq;

        /// <summary>
        /// detuning value (used to compute <see cref="PhaseStep"/>)
        /// </summary>
        public int Detune;

        /// <summary>
        /// multiple value (x.1, used to compute <see cref="PhaseStep"/>)
        /// </summary>
        public uint Multiple;

        /// <summary>
        /// sustain level, shifted up to envelope values
        /// </summary>
        public uint EgSustain;

        /// <summary>
        /// envelope rate, including KSR
        /// </summary>
        public byte[] EgRate;

        // envelope shift amount
        public byte EgShift;
    };
}
