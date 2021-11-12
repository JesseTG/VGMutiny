using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using JetBrains.Annotations;

namespace Ymfm.Opm
{
    //*********************************************************
    //  REGISTER CLASSES
    //*********************************************************

    // ======================> opm_registers

    //
    // OPM register map:
    //
    //      System-wide registers:
    //           01 xxxxxx-x Test register
    //              ------x- LFO reset
    //           08 -x------ Key on/off operator 4
    //              --x----- Key on/off operator 3
    //              ---x---- Key on/off operator 2
    //              ----x--- Key on/off operator 1
    //              -----xxx Channel select
    //           0F x------- Noise enable
    //              ---xxxxx Noise frequency
    //           10 xxxxxxxx Timer A value (upper 8 bits)
    //           11 ------xx Timer A value (lower 2 bits)
    //           12 xxxxxxxx Timer B value
    //           14 x------- CSM mode
    //              --x----- Reset timer B
    //              ---x---- Reset timer A
    //              ----x--- Enable timer B
    //              -----x-- Enable timer A
    //              ------x- Load timer B
    //              -------x Load timer A
    //           18 xxxxxxxx LFO frequency
    //           19 0xxxxxxx AM LFO depth
    //              1xxxxxxx PM LFO depth
    //           1B xx------ CT (2 output data lines)
    //              ------xx LFO waveform
    //
    //     Per-channel registers (channel in address bits 0-2)
    //        20-27 x------- Pan right
    //              -x------ Pan left
    //              --xxx--- Feedback level for operator 1 (0-7)
    //              -----xxx Operator connection algorithm (0-7)
    //        28-2F -xxxxxxx Key code
    //        30-37 xxxxxx-- Key fraction
    //        38-3F -xxx---- LFO PM sensitivity
    //              ------xx LFO AM shift
    //
    //     Per-operator registers (channel in address bits 0-2, operator in bits 3-4)
    //        40-5F -xxx---- Detune value (0-7)
    //              ----xxxx Multiple value (0-15)
    //        60-7F -xxxxxxx Total level (0-127)
    //        80-9F xx------ Key scale rate (0-3)
    //              ---xxxxx Attack rate (0-31)
    //        A0-BF x------- LFO AM enable
    //              ---xxxxx Decay rate (0-31)
    //        C0-DF xx------ Detune 2 value (0-3)
    //              ---xxxxx Sustain rate (0-31)
    //        E0-FF xxxx---- Sustain level (0-15)
    //              ----xxxx Release rate (0-15)
    //
    //     Internal (fake) registers:
    //           1A -xxxxxxx PM depth
    //
    public sealed partial class OpmRegisters : IFmRegisters<OpmRegisters.OpmOperatorMapping>
    {
        // LFO waveforms are 256 entries long
        public const uint LFO_WAVEFORM_LENGTH = 256;
        public const uint WAVEFORM_LENGTH = 0x400;


        // constants  
        public const uint OUTPUTS = 2;
        public const uint CHANNELS = 8;
        public const uint ALL_CHANNELS = (1 << (int)CHANNELS) - 1;
        public const uint OPERATORS = CHANNELS * 4;
        public const uint WAVEFORMS = 1;
        public const uint REGISTERS = 0x100;
        public const uint DEFAULT_PRESCALE = 2;
        public const uint EG_CLOCK_DIVIDER = 3;
        public const uint CSM_TRIGGER_MASK = ALL_CHANNELS;
        public const uint REG_MODE = 0x14;
        public const byte STATUS_TIMERA = 0x01;
        public const byte STATUS_TIMERB = 0x02;
        public const byte STATUS_BUSY = 0x80;
        public const byte STATUS_IRQ = 0;

        // constructor
        public OpmRegisters()
        {
            m_lfo_counter = 0;
            m_noise_lfsr = 1;
            m_noise_counter = 0;
            m_noise_state = 0;
            m_noise_lfo = 0;
            m_lfo_am = 0;
            m_lfo_waveform = new short[4, LFO_WAVEFORM_LENGTH];

            // create the waveforms
            for (uint index = 0; index < WAVEFORM_LENGTH; index++)
            {
                m_waveform[0, index] = (ushort)(Utils.AbsSinAttenuation(index) | (Utils.Bitfield(index, 9) << 15));
            }

            // create the LFO waveforms; AM in the low 8 bits, PM in the upper 8
            // waveforms are adjusted to match the pictures in the application manual
            for (uint index = 0; index < LFO_WAVEFORM_LENGTH; index++)
            {
                // waveform 0 is a sawtooth
                var am = (byte)(index ^ 0xff);
                var pm = (sbyte)(index);
                m_lfo_waveform[0, index] = (short)(am | (pm << 8));

                // waveform 1 is a square wave
                am = (byte)(Utils.Bitfield(index, 7) != 0 ? 0 : 0xff);
                pm = (sbyte)(am ^ 0x80);
                m_lfo_waveform[1, index] = (short)(am | (pm << 8));

                // waveform 2 is a triangle wave
                am = (byte)(Utils.Bitfield(index, 7) != 0 ? (index << 1) : ((index ^ 0xff) << 1));
                pm = (sbyte)(Utils.Bitfield(index, 6) != 0 ? am : ~am);
                m_lfo_waveform[2, index] = (short)(am | (pm << 8));

                // waveform 3 is noise; it is filled in dynamically
            }
        }

        // reset to initial state
        public void Reset()
        {
            m_regdata.Data.Fill(0);

            // enable output on both channels by default
            m_regdata.Data[0x20] = m_regdata.Data[0x21] = m_regdata.Data[0x22] = m_regdata.Data[0x23] = 0xc0;
            m_regdata.Data[0x24] = m_regdata.Data[0x25] = m_regdata.Data[0x26] = m_regdata.Data[0x27] = 0xc0;
        }

        // save/restore
        public void SaveRestore(YmfmSavedState state)
        {
            state.SaveRestore(ref m_lfo_counter);
            state.SaveRestore(ref m_lfo_am);
            state.SaveRestore(ref m_noise_lfsr);
            state.SaveRestore(ref m_noise_counter);
            state.SaveRestore(ref m_noise_state);
            state.SaveRestore(ref m_noise_lfo);
            state.SaveRestore(m_regdata.Data);
        }

        //-------------------------------------------------
        //  operator_map - return an array of operator
        //  indices for each channel; for OPM this is fixed
        //-------------------------------------------------
        public void OperatorMap(ref OpmOperatorMapping dest)
        {
            FixedMap.CopyTo(dest.OperatorIndexes);
        }

        //-------------------------------------------------
        //  write - handle writes to the register array
        //-------------------------------------------------
        public bool Write(ushort index, byte data, ref uint channel, ref uint opMask)
        {
            if (index >= REGISTERS)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            // LFO AM/PM depth are written to the same register (0x19);
            // redirect the PM depth to an unused neighbor (0x1a)
            if (index == 0x19)
            {
                m_regdata.Data[(int)(index + Utils.Bitfield(data, 7))] = data;
            }
            else if (index != 0x1a)
            {
                m_regdata.Data[index] = data;
            }

            // handle writes to the key on index
            if (index == 0x08)
            {
                channel = Utils.Bitfield(data, 0, 3);
                opMask = Utils.Bitfield(data, 3, 4);
                return true;
            }

            return false;
        }

        //-------------------------------------------------
        //  clock_noise_and_lfo - clock the noise and LFO,
        //  handling clock division, depth, and waveform
        //  computations
        //-------------------------------------------------
        public int ClockNoiseAndLfo()
        {
            // base noise frequency is measured at 2x 1/2 FM frequency; this
            // means each tick counts as two steps against the noise counter
            var freq = NoiseFrequency;
            for (var rep = 0; rep < 2; rep++)
            {
                // evidence seems to suggest the LFSR is clocked continually and just
                // sampled at the noise frequency for output purposes; note that the
                // low 8 bits are the most recent 8 bits of history while bits 8-24
                // contain the 17 bit LFSR state
                m_noise_lfsr <<= 1;
                m_noise_lfsr |= Utils.Bitfield(m_noise_lfsr, 17) ^ Utils.Bitfield(m_noise_lfsr, 14) ^ 1;

                // compare against the frequency and latch when we exceed it
                if (m_noise_counter++ >= freq)
                {
                    m_noise_counter = 0;
                    m_noise_state = (byte)Utils.Bitfield(m_noise_lfsr, 17);
                }
            }

            // treat the rate as a 4.4 floating-point step value with implied
            // leading 1; this matches exactly the frequencies in the application
            // manual, though it might not be implemented exactly this way on chip
            var rate = LfoRate;
            m_lfo_counter += (0x10 | Utils.Bitfield(rate, 0, 4)) << (int)Utils.Bitfield(rate, 4, 4);

            // bit 1 of the test register is officially undocumented but has been
            // discovered to hold the LFO in reset while active
            if (LfoReset != 0)
            {
                m_lfo_counter = 0;
            }

            // now pull out the non-fractional LFO value
            var lfo = Utils.Bitfield(m_lfo_counter, 22, 8);

            // fill in the noise entry 1 ahead of our current position; this
            // ensures the current value remains stable for a full LFO clock
            // and effectively latches the running value when the LFO advances
            var lfoNoise = Utils.Bitfield(m_noise_lfsr, 17, 8);
            m_lfo_waveform[3, (lfo + 1) & 0xff] = (short)(lfoNoise | (lfoNoise << 8));

            // fetch the AM/PM values based on the waveform; AM is unsigned and
            // encoded in the low 8 bits, while PM signed and encoded in the upper
            // 8 bits
            int ampm = m_lfo_waveform[LfoWaveform, lfo];

            // apply depth to the AM value and store for later
            m_lfo_am = (byte)(((ampm & 0xff) * LfoAmDepth) >> 7);

            // apply depth to the PM value and return it
            return ((ampm >> 8) * (int)(LfoPmDepth)) >> 7;
        }

        public uint LfoAmOffset(uint channelOffset)
        {
            // OPM maps AM quite differently from OPN

            // shift value for AM sensitivity is [*, 0, 1, 2],
            // mapping to values of [0, 23.9, 47.8, and 95.6dB]
            var amSensitivity = ChannelLfoAmSens(channelOffset);
            if (amSensitivity == 0)
            {
                return 0;
            }

            // QUESTION: see OPN note below for the dB range mapping; it applies
            // here as well

            // raw LFO AM value on OPM is 0-FF, which is already a factor of 2
            // larger than the OPN below, putting our staring point at 2x theirs;
            // this works out since our minimum is 2x their maximum
            return (uint)(m_lfo_am << (int)(amSensitivity - 1));
        }

        // return the current noise state, gated by the noise clock
        public uint NoiseState => m_noise_state;

        //-------------------------------------------------
        //  cache_operator_data - fill the operator cache
        //  with prefetched data
        //-------------------------------------------------
        public void CacheOperatorData(uint channelOffset, uint operatorOffset, ref OpDataCache cache)
        {
            // set up the easy stuff
            // cache.Waveform = m_waveform[0, 0];
            // TODO: Cache the waveform

            // get frequency from the channel
            var blockFreq = cache.BlockFreq = ChannelBlockFreq(channelOffset);

            // compute the keycode: block_freq is:
            //
            //     BBBCCCCFFFFFF
            //     ^^^^^
            //
            // the 5-bit keycode is just the top 5 bits (block + top 2 bits
            // of the key code)
            var keycode = Utils.Bitfield(blockFreq, 8, 5);

            // detune adjustment
            cache.Detune = Utils.DetuneAdjustment(OpDetune(operatorOffset), keycode);

            // multiple value, as an x.1 value (0 means 0.5)
            cache.Multiple = OpMultiple(operatorOffset) * 2;
            if (cache.Multiple == 0)
            {
                cache.Multiple = 1;
            }

            // phase step, or PHASE_STEP_DYNAMIC if PM is active; this depends on
            // block_freq, detune, and multiple, so compute it after we've done those
            if (LfoPmDepth == 0 || ChannelLfoPmSens(channelOffset) == 0)
            {
                cache.PhaseStep = ComputePhaseStep(channelOffset, operatorOffset, cache, 0);
            }
            else
            {
                cache.PhaseStep = OpDataCache.PhaseStepDynamic;
            }

            // total level, scaled by 8
            cache.TotalLevel = OpTotalLevel(operatorOffset) << 3;

            // 4-bit sustain level, but 15 means 31 so effectively 5 bits
            cache.EgSustain = OpSustainLevel(operatorOffset);
            cache.EgSustain |= (cache.EgSustain + 1) & 0x10;
            cache.EgSustain <<= 5;

            // determine KSR adjustment for enevlope rates
            var ksrVal = keycode >> (int)(OpKsr(operatorOffset) ^ 3);
            cache.EgRate[(int)EnvelopeState.Attack] =
                (byte)IFmRegisters<OpmOperatorMapping>.EffectiveRate(OpAttackRate(operatorOffset) * 2, ksrVal);
            cache.EgRate[(int)EnvelopeState.Decay] =
                (byte)IFmRegisters<OpmOperatorMapping>.EffectiveRate(OpDecayRate(operatorOffset) * 2, ksrVal);
            cache.EgRate[(int)EnvelopeState.Sustain] =
                (byte)IFmRegisters<OpmOperatorMapping>.EffectiveRate(OpSustainRate(operatorOffset) * 2, ksrVal);
            cache.EgRate[(int)EnvelopeState.Release] =
                (byte)IFmRegisters<OpmOperatorMapping>.EffectiveRate(OpReleaseRate(operatorOffset) * 4 + 2, ksrVal);
        }

        //-------------------------------------------------
        //  compute_phase_step - compute the phase step
        //-------------------------------------------------
        public uint ComputePhaseStep(uint channelOffset, uint operatorOffset, in OpDataCache cache, int lfoRawPm)
        {
            // OPM logic is rather unique here, due to extra detune
            // and the use of key codes (not to be confused with keycode)

            int delta = Detune2Delta[OpDetune2(operatorOffset)];

            // add in the PM delta
            var pmSensitivity = ChannelLfoPmSens(operatorOffset);
            if (pmSensitivity != 0)
            {
                // raw PM value is -127..128 which is +/- 200 cents
                // manual gives these magnitudes in cents:
                //    0, +/-5, +/-10, +/-20, +/-50, +/-100, +/-400, +/-700
                // this roughly corresponds to shifting the 200-cent value:
                //    0  >> 5,  >> 4,  >> 3,  >> 2,  >> 1,   << 1,   << 2
                if (pmSensitivity < 6)
                {
                    delta += lfoRawPm >> (int)(6 - pmSensitivity);
                }
                else
                {
                    delta += lfoRawPm << (int)(pmSensitivity - 5);
                }
            }

            // apply delta and convert to a frequency number
            var phaseStep = Utils.OpmKeyCodeToPhaseStep(cache.BlockFreq, delta);

            // apply detune based on the keycode
            phaseStep += (uint)cache.Detune;

            // apply frequency multiplier (which is cached as an x.1 value)
            return (phaseStep * cache.Multiple) >> 1;
        }


        [SuppressMessage("ReSharper", "HeapView.BoxingAllocation")]
        public string LogKeyOn(uint channelOffset, uint operatorOffset)
        {
            var builder = new StringBuilder();
            builder.AppendFormat(
                "{0}.{1:00} freq={2:X4} dt2={3} dt={4} fb={5} alg={6:X} mul={7:X} tl={8:X2} ksr={9} adsr={10:X2}/{11:X2}/{12:X2}/{13:X} sl={14:X} out={15}{16}",
                channelOffset,
                operatorOffset,
                ChannelBlockFreq(channelOffset),
                OpDetune2(operatorOffset),
                OpDetune(operatorOffset),
                ChannelFeedback(channelOffset),
                ChannelAlgorithm(channelOffset),
                OpMultiple(operatorOffset),
                OpTotalLevel(operatorOffset),
                OpKsr(operatorOffset),
                OpAttackRate(operatorOffset),
                OpDecayRate(operatorOffset),
                OpSustainRate(operatorOffset),
                OpReleaseRate(operatorOffset),
                OpSustainLevel(operatorOffset),
                ChannelOutput0(channelOffset) != 0 ? 'L' : '-',
                ChannelOutput1(channelOffset) != 0 ? 'R' : '-'
            );


            var am = (LfoAmDepth != 0 && ChannelLfoAmSens(channelOffset) != 0 && OpLfoAmEnable(operatorOffset) != 0);
            if (am)
            {
                builder.AppendFormat(" am={0}/{1:X2}", ChannelLfoAmSens(channelOffset), LfoAmDepth);
            }

            var pm = (LfoPmDepth != 0 && ChannelLfoPmSens(channelOffset) != 0);
            if (pm)
            {
                builder.AppendFormat(" pm={0}/{1:X2}", ChannelLfoPmSens(channelOffset), LfoPmDepth);
            }

            if (am || pm)
            {
                builder.AppendFormat(" lfo={0:X2}/{1}", LfoRate, "WQTN"[(int)LfoWaveform]);
            }

            if (NoiseEnable != 0 && operatorOffset == 31)
            {
                builder.Append(" noise=1");
            }

            return builder.ToString();
        }

        // system-wide registers
        public uint Test => @byte(0x01, 0, 8);
        public uint LfoReset => @byte(0x01, 1, 1);
        public uint NoiseFrequency => @byte(0x0f, 0, 5);

        public uint Outputs => OUTPUTS;
        public uint Channels => CHANNELS;
        public uint AllChannels => ALL_CHANNELS;
        public uint Operators => OPERATORS;
        public uint Waveforms => WAVEFORMS;
        public uint Registers => REGISTERS;
        public uint DefaultPrescale => DEFAULT_PRESCALE;
        public uint EgClockDivider => EG_CLOCK_DIVIDER;
        public uint CsmTriggerMask => CSM_TRIGGER_MASK;
        public uint RegisterMode => REG_MODE;
        public byte StatusTimerA => STATUS_TIMERA;
        public byte StatusTimerB => STATUS_TIMERB;
        public byte StatusBusy => STATUS_BUSY;
        public byte StatusIrq => STATUS_IRQ;
        public uint NoiseEnable => @byte(0x0f, 7, 1);
        public uint TimerAValue => word(0x10, 0, 8, 0x11, 0, 2);
        public uint TimerBValue => @byte(0x12, 0, 8);
        public uint Csm => @byte(0x14, 7, 1);
        public uint ResetTimerB => @byte(0x14, 5, 1);
        public uint ResetTimerA => @byte(0x14, 4, 1);
        public uint EnableTimerB => @byte(0x14, 3, 1);
        public uint EnableTimerA => @byte(0x14, 2, 1);
        public uint LoadTimerB => @byte(0x14, 1, 1);
        public uint LoadTimerA => @byte(0x14, 0, 1);
        public uint LfoRate => @byte(0x18, 0, 8);
        public uint LfoAmDepth => @byte(0x19, 0, 7);
        public uint LfoPmDepth => @byte(0x1a, 0, 7);
        public uint OutputBits => @byte(0x1b, 6, 2);
        public uint LfoWaveform => @byte(0x1b, 0, 2);

        // per-channel registers
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ChannelOutputAny(uint channelOffset) => @byte(0x20, 6, 2, channelOffset);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ChannelOutput0(uint channelOffset) => @byte(0x20, 6, 1, channelOffset);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ChannelOutput1(uint channelOffset) => @byte(0x20, 7, 1, channelOffset);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ChannelOutput2(uint _) => 0;

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ChannelOutput3(uint _) => 0;

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ChannelFeedback(uint channelOffset) => @byte(0x20, 3, 3, channelOffset);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ChannelAlgorithm(uint channelOffset) => @byte(0x20, 0, 3, channelOffset);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ChannelBlockFreq(uint channelOffset) => word(0x28, 0, 7, 0x30, 2, 6, channelOffset);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ChannelLfoPmSens(uint channelOffset) => @byte(0x38, 4, 3, channelOffset);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint ChannelLfoAmSens(uint channelOffset) => @byte(0x38, 0, 2, channelOffset);

        // per-operator registers
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint OpDetune(uint operatorOffset) => @byte(0x40, 4, 3, operatorOffset);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint OpMultiple(uint operatorOffset) => @byte(0x40, 0, 4, operatorOffset);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint OpTotalLevel(uint operatorOffset) => @byte(0x60, 0, 7, operatorOffset);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint OpKsr(uint operatorOffset) => @byte(0x80, 6, 2, operatorOffset);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint OpAttackRate(uint operatorOffset) => @byte(0x80, 0, 5, operatorOffset);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint OpLfoAmEnable(uint operatorOffset) => @byte(0xa0, 7, 1, operatorOffset);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint OpDecayRate(uint operatorOffset) => @byte(0xa0, 0, 5, operatorOffset);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint OpDetune2(uint operatorOffset) => @byte(0xc0, 6, 2, operatorOffset);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint OpSustainRate(uint operatorOffset) => @byte(0xc0, 0, 5, operatorOffset);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint OpSustainLevel(uint operatorOffset) => @byte(0xe0, 4, 4, operatorOffset);

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint OpReleaseRate(uint operatorOffset) => @byte(0xe0, 0, 4, operatorOffset);

        // return a bitfield extracted from a byte
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint @byte(uint offset, uint start, uint count, uint extraOffset = 0)
        {
            return Utils.Bitfield(m_regdata.Data[(int)(offset + extraOffset)], (int)start, (int)count);
        }

        // return a bitfield extracted from a pair of bytes, MSBs listed first
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        private uint word(
            uint offset1,
            uint start1,
            uint count1,
            uint offset2,
            uint start2,
            uint count2,
            uint extraOffset = 0
        )
        {
            return (@byte(offset1, start1, count1, extraOffset) << (int)count2) |
                   @byte(offset2, start2, count2, extraOffset);
        }

        // internal state
        private uint m_lfo_counter; // LFO counter
        private uint m_noise_lfsr; // noise LFSR state
        private byte m_noise_counter; // noise counter
        private byte m_noise_state; // latched noise state
        private byte m_noise_lfo; // latched LFO noise value
        private byte m_lfo_am; // current LFO AM value
        private RegisterData m_regdata; // register data
        private readonly short[,] m_lfo_waveform; // LFO waveforms; AM in low 8, PM in upper 8
        private readonly WaveformTable m_waveform; // waveforms

        private unsafe struct RegisterData
        {
            public readonly Span<byte> Data
            {
                get
                {
                    fixed (byte* d = _data)
                    {
                        return new Span<byte>(d, (int)REGISTERS);
                    }
                }
            }

            private fixed byte _data[(int)REGISTERS];
        }

        private unsafe struct WaveformTable
        {
            private fixed ushort _waveform[(int)(WAVEFORMS * WAVEFORM_LENGTH)];

            public readonly Span<ushort> Table
            {
                get
                {
                    fixed (ushort* ptr = _waveform)
                    {
                        return new Span<ushort>(ptr, (int)(WAVEFORMS * WAVEFORM_LENGTH));
                    }
                }
            }

            public ushort this[int x, int y]
            {
                [Pure]
                readonly get
                {
                    ValidateX(x);
                    ValidateY(y);
                    return _waveform[WAVEFORMS * x + y];
                }
                set
                {
                    ValidateX(x);
                    ValidateY(y);
                    _waveform[WAVEFORMS * x + y] = value;
                }
            }


            public ushort this[uint x, uint y]
            {
                [Pure]
                readonly get
                {
                    ValidateX(x);
                    ValidateY(y);
                    return _waveform[WAVEFORMS * x + y];
                }
                set
                {
                    ValidateX(x);
                    ValidateY(y);
                    _waveform[WAVEFORMS * x + y] = value;
                }
            }

            public ushort this[int x, uint y]
            {
                [Pure]
                readonly get
                {
                    ValidateX(x);
                    ValidateY(y);
                    return _waveform[WAVEFORMS * x + y];
                }
                set
                {
                    ValidateX(x);
                    ValidateY(y);
                    _waveform[WAVEFORMS * x + y] = value;
                }
            }

            public ushort this[uint x, int y]
            {
                [Pure]
                readonly get
                {
                    ValidateX(x);
                    ValidateY(y);
                    return _waveform[WAVEFORMS * x + y];
                }
                set
                {
                    ValidateX(x);
                    ValidateY(y);
                    _waveform[WAVEFORMS * x + y] = value;
                }
            }

            private static void ValidateX(int x)
            {
                if (x < 0 || x >= WAVEFORMS)
                {
                    throw new ArgumentOutOfRangeException(nameof(x), x, $"Expected 0 <= x < {WAVEFORMS}, got {x}");
                }
            }

            private static void ValidateX(uint x)
            {
                if (x >= WAVEFORMS)
                {
                    throw new ArgumentOutOfRangeException(nameof(x), x, $"Expected 0 <= x < {WAVEFORMS}, got {x}");
                }
            }

            private static void ValidateY(int y)
            {
                if (y < 0 || y >= WAVEFORM_LENGTH)
                {
                    throw new ArgumentOutOfRangeException(nameof(y), y, $"Expected 0 <=y < {WAVEFORM_LENGTH}, got {y}");
                }
            }

            private static void ValidateY(uint y)
            {
                if (y >= WAVEFORM_LENGTH)
                {
                    throw new ArgumentOutOfRangeException(nameof(y), y, $"Expected 0 <=y < {WAVEFORM_LENGTH}, got {y}");
                }
            }
        }

        public unsafe struct OpmOperatorMapping : IOperatorMapping
        {
            private fixed uint _indexes[(int)CHANNELS];

            public readonly Span<uint> OperatorIndexes
            {
                get
                {
                    fixed (uint* i = _indexes)
                    {
                        return new Span<uint>(i, (int)CHANNELS);
                    }
                }
            }
        }
    };
}
