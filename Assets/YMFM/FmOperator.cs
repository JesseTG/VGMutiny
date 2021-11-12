using System;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;

namespace Ymfm
{
    // fm_operator represents an FM operator (or "slot" in FM parlance), which
    // produces an output sine wave modulated by an envelope
    public sealed class FmOperator<TRegisterType, TOutputType, TOperatorMapping>
        where TRegisterType : class, IFmRegisters<TOperatorMapping>, new()
        where TOutputType : struct, IOutput
        where TOperatorMapping : struct, IOperatorMapping
    {
        // "quiet" value, used to optimize when we can skip doing working
        public const uint EgQuiet = 0x200;


        // constructor
        public FmOperator(FmEngineBase<TRegisterType, TOutputType, TOperatorMapping> owner, uint operatorOffset)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            Registers = owner.Registers;
            ChannelOffset = 0;
            OperatorOffset = operatorOffset;
            _phase = 0;
            _envelopeAttenuation = 0x3ff;
            _envelopeState = EnvelopeState.Release;
            _ssgInverted = 0;
            _keyState = 0;
            _keyOnLive = 0;
        }

        // save/restore
        public void SaveRestore(YmfmSavedState state)
        {
            state.SaveRestore(ref _phase);
            state.SaveRestore(ref _envelopeAttenuation);
            state.SaveRestore(ref _envelopeState);
            state.SaveRestore(ref _ssgInverted);
            state.SaveRestore(ref _keyState);
            state.SaveRestore(ref _keyOnLive);
        }

        // reset the operator state
        public void Reset()
        {
            // reset our data
            _phase = 0;
            _envelopeAttenuation = 0x3ff;
            _envelopeState = EnvelopeState.Release;
            _ssgInverted = 0;
            _keyState = 0;
            _keyOnLive = 0;
        }

        // return the operator/channel offset
        public uint OperatorOffset { get; }

        public uint ChannelOffset
        {
            get;
            // set the current channel
            set;
        }

        // prepare prior to clocking
        public bool Prepare()
        {
            // cache the data
            Registers.CacheOperatorData(ChannelOffset, OperatorOffset, ref _cache);

            // clock the key state
            ClockKeyState((_keyOnLive != 0) ? 1u : 0u);
            _keyOnLive &= unchecked((byte)~(1 << (int)KeyOnType.Csm));

            // we're active until we're quiet after the release
            return (_envelopeState != (Registers.EgHasReverb ? EnvelopeState.Reverb : EnvelopeState.Release) ||
                    _envelopeAttenuation < EgQuiet);
        }

        // master clocking function
        public void Clock(uint envCounter, int lfoRawPm)
        {
            // clock the SSG-EG state (OPN/OPNA)
            if (Registers.OpSsgEgEnable(OperatorOffset) != 0)
            {
                ClockSsgEgState();
            }

            // clock the envelope if on an envelope cycle; env_counter is a x.2 value
            if (Utils.Bitfield(envCounter, 0, 2) == 0)
            {
                ClockEnvelope(envCounter >> 2);
            }

            // clock the phase
            ClockPhase(lfoRawPm);
        }

        // return the current phase value
        public uint Phase => _phase >> 10;

        //-------------------------------------------------
        //  compute_volume - compute the 14-bit signed
        //  volume of this operator, given a phase
        //  modulation and an AM LFO offset
        //-------------------------------------------------
        public int ComputeVolume(uint phase, uint amOffset)
        {
            // the low 10 bits of phase represents a full 2*PI period over
            // the full sin wave


            // early out if the envelope is effectively off
            if (_envelopeAttenuation > EgQuiet)
            {
                return 0;
            }

            // get the absolute value of the sin, as attenuation, as a 4.8 fixed point value
            uint sinAttenuation = _cache.Waveform.Span[(int)(phase & (Registers.WaveformLength - 1))];

            // get the attenuation from the envelope generator as a 4.6 value, shifted up to 4.8
            var envAttenuation = EnvelopeAttenuation(amOffset) << 2;

            // combine into a 5.8 value, then convert from attenuation to 13-bit linear volume
            var result = (int)Tables.AttenuationToVolume((sinAttenuation & 0x7fff) + envAttenuation);

            // negate if in the negative part of the sin wave (sign bit gives 14 bits)
            return (Utils.Bitfield(sinAttenuation, 15) != 0) ? -result : result;
        }

        // compute volume for the OPM noise channel
        //-------------------------------------------------
        //  compute_noise_volume - compute the 14-bit
        //  signed noise volume of this operator, given a
        //  noise input value and an AM offset
        //-------------------------------------------------
        public int ComputeNoiseVolume(uint amOffset)
        {
            // application manual says the logarithmic transform is not applied here, so we
            // just use the raw envelope attenuation, inverted (since 0 attenuation should be
            // maximum), and shift it up from a 10-bit value to an 11-bit value
            var result = (int)((EnvelopeAttenuation(amOffset) ^ 0x3ff) << 1);

            // QUESTION: is AM applied still?

            // negate based on the noise state
            return Utils.Bitfield(Registers.NoiseState, 0) != 0 ? -result : result;
        }

        // key state control
        //-------------------------------------------------
        //  keyonoff - signal a key on/off event
        //-------------------------------------------------
        public void KeyOnOff(uint on, KeyOnType type)
        {
            _keyOnLive = (byte)((uint)(_keyOnLive & ~(1 << (int)(type))) | (Utils.Bitfield(@on, 0) << (int)(type)));
        }

        // return a reference to our registers
        [NotNull]
        public TRegisterType Registers { get; }

        // simple getters for debugging
        public EnvelopeState DebugEgState => _envelopeState;
        public ushort DebugEgAttenuation => _envelopeAttenuation;

        public ref OpDataCache DebugCache => ref _cache;


        //-------------------------------------------------
        //  start_attack - start the attack phase; called
        //  when a keyon happens or when an SSG-EG cycle
        //  is complete and restarts
        //-------------------------------------------------
        private unsafe void StartAttack(bool isRestart = false)
        {
            // don't change anything if already in attack state
            if (_envelopeState == EnvelopeState.Attack)
                return;
            _envelopeState = EnvelopeState.Attack;

            // generally not inverted at start, except if SSG-EG is enabled and
            // one of the inverted modes is specified; leave this alone on a
            // restart, as it is managed by the clock_ssg_eg_state() code
            if (Registers.EgHasSsg && !isRestart)
            {
                _ssgInverted = (byte)(Registers.OpSsgEgEnable(OperatorOffset) &
                                      Utils.Bitfield(Registers.OpSsgEgMode(OperatorOffset), 2));
            }

            // reset the phase when we start an attack due to a key on
            // (but not when due to an SSG-EG restart except in certain cases
            // managed directly by the SSG-EG code)
            if (!isRestart)
            {
                _phase = 0;
            }

            // if the attack rate >= 62 then immediately go to max attenuation
            if (_cache.EgRate[(int)EnvelopeState.Attack] >= 62)
            {
                _envelopeAttenuation = 0;
            }
        }


        //-------------------------------------------------
        //  start_release - start the release phase;
        //  called when a keyoff happens
        //-------------------------------------------------

        private void StartRelease()
        {
            // don't change anything if already in release state
            if (_envelopeState >= EnvelopeState.Release)
                return;
            _envelopeState = EnvelopeState.Release;

            // if attenuation if inverted due to SSG-EG, snap the inverted attenuation
            // as the starting point
            if (Registers.EgHasSsg && (_ssgInverted != 0))
            {
                _envelopeAttenuation = (ushort)((0x200 - _envelopeAttenuation) & 0x3ff);
                _ssgInverted = 0; // false
            }
        }

        //-------------------------------------------------
        //  clock_keystate - clock the keystate to match
        //  the incoming keystate
        //-------------------------------------------------
        private void ClockKeyState(uint keyState)
        {
            Debug.Assert(keyState is 0 or 1);

            // has the key changed?
            if ((keyState ^ _keyState) != 0)
            {
                _keyState = (byte)keyState;

                // if the key has turned on, start the attack
                if (keyState != 0)
                {
                    // OPLL has a DP ("depress"?) state to bring the volume
                    // down before starting the attack
                    if (Registers.EgHasDepress && _envelopeAttenuation < 0x200)
                    {
                        _envelopeState = EnvelopeState.Depress;
                    }
                    else
                    {
                        StartAttack();
                    }
                }

                // otherwise, start the release
                else
                {
                    StartRelease();
                }
            }
        }

        //-------------------------------------------------
        //  clock_ssg_eg_state - clock the SSG-EG state;
        //  should only be called if SSG-EG is enabled
        //-------------------------------------------------
        private void ClockSsgEgState()
        {
            // work only happens once the attenuation crosses above 0x200
            if (!Utils.Bit(_envelopeAttenuation, 9))
                return;

            // 8 SSG-EG modes:
            //    000: repeat normally
            //    001: run once, hold low
            //    010: repeat, alternating between inverted/non-inverted
            //    011: run once, hold high
            //    100: inverted repeat normally
            //    101: inverted run once, hold low
            //    110: inverted repeat, alternating between inverted/non-inverted
            //    111: inverted run once, hold high
            var mode = Registers.OpSsgEgMode(OperatorOffset);

            // hold modes (1/3/5/7)
            if (Utils.Bit(mode, 0))
            {
                // set the inverted flag to the end state (0 for modes 1/7, 1 for modes 3/5)
                _ssgInverted = (byte)(Utils.Bitfield(mode, 2) ^ Utils.Bitfield(mode, 1));

                // if holding, force the attenuation to the expected value once we're
                // past the attack phase
                if (_envelopeState != EnvelopeState.Attack)
                    _envelopeAttenuation = (ushort)(_ssgInverted != 0 ? 0x200 : 0x3ff);
            }

            // continuous modes (0/2/4/6)
            else
            {
                // toggle invert in alternating mode (even in attack state)
                _ssgInverted ^= (byte)Utils.Bitfield(mode, 1);

                // restart attack if in decay/sustain states
                if (_envelopeState is EnvelopeState.Decay or EnvelopeState.Sustain)
                    StartAttack(true);

                // phase is reset to 0 in modes 0/4
                if (Utils.Bitfield(mode, 1) == 0)
                    _phase = 0;
            }

            // in all modes, once we hit release state, attenuation is forced to maximum
            if (_envelopeState == EnvelopeState.Release)
                _envelopeAttenuation = 0x3ff;
        }

        //-------------------------------------------------
        //  clock_envelope - clock the envelope state
        //  according to the given count
        //-------------------------------------------------
        private unsafe void ClockEnvelope(uint envCounter)
        {
            // handle attack->decay transitions
            if (_envelopeState == EnvelopeState.Attack && _envelopeAttenuation == 0)
            {
                _envelopeState = EnvelopeState.Decay;
            }

            // handle decay->sustain transitions; it is important to do this immediately
            // after the attack->decay transition above in the event that the sustain level
            // is set to 0 (in which case we will skip right to sustain without doing any
            // decay); as an example where this can be heard, check the cymbals sound
            // in channel 0 of shinobi's test mode sound #5
            if (_envelopeState == EnvelopeState.Decay && _envelopeAttenuation >= _cache.EgSustain)
            {
                _envelopeState = EnvelopeState.Sustain;
            }

            // fetch the appropriate 6-bit rate value from the cache
            uint rate = _cache.EgRate[(int)_envelopeState];

            // compute the rate shift value; this is the shift needed to
            // apply to the env_counter such that it becomes a 5.11 fixed
            // point number
            var rateShift = rate >> 2;
            envCounter <<= (int)rateShift;

            // see if the fractional part is 0; if not, it's not time to clock
            if (Utils.Bitfield(envCounter, 0, 11) != 0)
                return;

            // determine the increment based on the non-fractional part of env_counter
            var relevantBits = Utils.Bitfield(envCounter, (int)((rateShift <= 11) ? 11 : rateShift), 3);
            var increment = AttenuationIncrement(rate, relevantBits);

            // attack is the only one that increases
            if (_envelopeState == EnvelopeState.Attack)
            {
                // glitch means that attack rates of 62/63 don't increment if
                // changed after the initial key on (where they are handled
                // specially); nukeykt confirms this happens on OPM, OPN, OPL/OPLL
                // at least so assuming it is true for everyone
                if (rate < 62)
                {
                    _envelopeAttenuation += (ushort)((~_envelopeAttenuation * increment) >> 4);
                }
            }

            // all other cases are similar
            else
            {
                // non-SSG-EG cases just apply the increment
                if (Registers.OpSsgEgEnable(OperatorOffset) == 0)
                {
                    _envelopeAttenuation += (ushort)increment;
                }

                // SSG-EG only applies if less than mid-point, and then at 4x
                else if (_envelopeAttenuation < 0x200)
                {
                    _envelopeAttenuation += (ushort)(4 * increment);
                }

                // clamp the final attenuation
                if (_envelopeAttenuation >= 0x400)
                {
                    _envelopeAttenuation = 0x3ff;
                }

                // transition from depress to attack
                if (Registers.EgHasDepress && _envelopeState == EnvelopeState.Depress && _envelopeAttenuation >= 0x200)
                {
                    StartAttack();
                }

                // transition from release to reverb, should switch at -18dB
                if (Registers.EgHasReverb && _envelopeState == EnvelopeState.Release && _envelopeAttenuation >= 0xc0)
                {
                    _envelopeState = EnvelopeState.Reverb;
                }
            }
        }

        //-------------------------------------------------
        //  clock_phase - clock the 10.10 phase value; the
        //  OPN version of the logic has been verified
        //  against the Nuked phase generator
        //-------------------------------------------------
        private void ClockPhase(int lfoRawPm)
        {
            // read from the cache, or recalculate if PM active
            var phaseStep = _cache.PhaseStep;
            if (phaseStep == OpDataCache.PhaseStepDynamic)
            {
                phaseStep = Registers.ComputePhaseStep(ChannelOffset, OperatorOffset, _cache, lfoRawPm);
            }

            // finally apply the step to the current phase value
            _phase += phaseStep;
        }

        // return effective attenuation of the envelope
        private uint EnvelopeAttenuation(uint amOffset)
        {
            var result = (uint)(_envelopeAttenuation >> _cache.EgShift);

            // invert if necessary due to SSG-EG
            if (Registers.EgHasSsg && (_ssgInverted) != 0)
            {
                result = (0x200 - result) & 0x3ff;
            }

            // add in LFO AM modulation
            if (Registers.OpLfoAmEnable(OperatorOffset) != 0)
            {
                result += amOffset;
            }

            // add in total level and KSL from the cache
            result += _cache.TotalLevel;

            // clamp to max, apply shift, and return
            return math.min(result, 0x3ff);
        }

        // internal state
        private uint _phase; // current phase value (10.10 format)
        private ushort _envelopeAttenuation; // computed envelope attenuation (4.6 format)
        private EnvelopeState _envelopeState; // current envelope state
        private byte _ssgInverted; // non-zero if the output should be inverted (bit 0)
        private byte _keyState; // current key state: on or off (bit 0)
        private byte _keyOnLive; // live key on state (bit 0 = direct, bit 1 = rhythm, bit 2 = CSM)
        private OpDataCache _cache; // cached values for performance

        //-------------------------------------------------
        //  attenuation_increment - given a 6-bit ADSR
        //  rate value and a 3-bit stepping index,
        //  return a 4-bit increment to the attenutaion
        //  for this step (or for the attack case, the
        //  fractional scale factor to decrease by)
        //-------------------------------------------------
        private static uint AttenuationIncrement(uint rate, uint index)
        {
            return Utils.Bitfield(Tables.IncrementTable[rate], (int)(4 * index), 4);
        }
    };

    internal static partial class Tables
    {
        internal static readonly uint[] IncrementTable =
        {
            0x00000000, 0x00000000, 0x10101010, 0x10101010, // 0-3    (0x00-0x03)
            0x10101010, 0x10101010, 0x11101110, 0x11101110, // 4-7    (0x04-0x07)
            0x10101010, 0x10111010, 0x11101110, 0x11111110, // 8-11   (0x08-0x0B)
            0x10101010, 0x10111010, 0x11101110, 0x11111110, // 12-15  (0x0C-0x0F)
            0x10101010, 0x10111010, 0x11101110, 0x11111110, // 16-19  (0x10-0x13)
            0x10101010, 0x10111010, 0x11101110, 0x11111110, // 20-23  (0x14-0x17)
            0x10101010, 0x10111010, 0x11101110, 0x11111110, // 24-27  (0x18-0x1B)
            0x10101010, 0x10111010, 0x11101110, 0x11111110, // 28-31  (0x1C-0x1F)
            0x10101010, 0x10111010, 0x11101110, 0x11111110, // 32-35  (0x20-0x23)
            0x10101010, 0x10111010, 0x11101110, 0x11111110, // 36-39  (0x24-0x27)
            0x10101010, 0x10111010, 0x11101110, 0x11111110, // 40-43  (0x28-0x2B)
            0x10101010, 0x10111010, 0x11101110, 0x11111110, // 44-47  (0x2C-0x2F)
            0x11111111, 0x21112111, 0x21212121, 0x22212221, // 48-51  (0x30-0x33)
            0x22222222, 0x42224222, 0x42424242, 0x44424442, // 52-55  (0x34-0x37)
            0x44444444, 0x84448444, 0x84848484, 0x88848884, // 56-59  (0x38-0x3B)
            0x88888888, 0x88888888, 0x88888888, 0x88888888 // 60-63  (0x3C-0x3F)
        };

        // the values here are 10-bit mantissas with an implied leading bit
        // this matches the internal format of the OPN chip, extracted from the die

        // as a nod to performance, the implicit 0x400 bit is pre-incorporated, and
        // the values are left-shifted by 2 so that a simple right shift is all that
        // is needed; also the order is reversed to save a NOT on the input
        static readonly ushort[] PowerTable =
        {
            X(0x3fa), X(0x3f5), X(0x3ef), X(0x3ea), X(0x3e4), X(0x3df), X(0x3da), X(0x3d4),
            X(0x3cf), X(0x3c9), X(0x3c4), X(0x3bf), X(0x3b9), X(0x3b4), X(0x3ae), X(0x3a9),
            X(0x3a4), X(0x39f), X(0x399), X(0x394), X(0x38f), X(0x38a), X(0x384), X(0x37f),
            X(0x37a), X(0x375), X(0x370), X(0x36a), X(0x365), X(0x360), X(0x35b), X(0x356),
            X(0x351), X(0x34c), X(0x347), X(0x342), X(0x33d), X(0x338), X(0x333), X(0x32e),
            X(0x329), X(0x324), X(0x31f), X(0x31a), X(0x315), X(0x310), X(0x30b), X(0x306),
            X(0x302), X(0x2fd), X(0x2f8), X(0x2f3), X(0x2ee), X(0x2e9), X(0x2e5), X(0x2e0),
            X(0x2db), X(0x2d6), X(0x2d2), X(0x2cd), X(0x2c8), X(0x2c4), X(0x2bf), X(0x2ba),
            X(0x2b5), X(0x2b1), X(0x2ac), X(0x2a8), X(0x2a3), X(0x29e), X(0x29a), X(0x295),
            X(0x291), X(0x28c), X(0x288), X(0x283), X(0x27f), X(0x27a), X(0x276), X(0x271),
            X(0x26d), X(0x268), X(0x264), X(0x25f), X(0x25b), X(0x257), X(0x252), X(0x24e),
            X(0x249), X(0x245), X(0x241), X(0x23c), X(0x238), X(0x234), X(0x230), X(0x22b),
            X(0x227), X(0x223), X(0x21e), X(0x21a), X(0x216), X(0x212), X(0x20e), X(0x209),
            X(0x205), X(0x201), X(0x1fd), X(0x1f9), X(0x1f5), X(0x1f0), X(0x1ec), X(0x1e8),
            X(0x1e4), X(0x1e0), X(0x1dc), X(0x1d8), X(0x1d4), X(0x1d0), X(0x1cc), X(0x1c8),
            X(0x1c4), X(0x1c0), X(0x1bc), X(0x1b8), X(0x1b4), X(0x1b0), X(0x1ac), X(0x1a8),
            X(0x1a4), X(0x1a0), X(0x19c), X(0x199), X(0x195), X(0x191), X(0x18d), X(0x189),
            X(0x185), X(0x181), X(0x17e), X(0x17a), X(0x176), X(0x172), X(0x16f), X(0x16b),
            X(0x167), X(0x163), X(0x160), X(0x15c), X(0x158), X(0x154), X(0x151), X(0x14d),
            X(0x149), X(0x146), X(0x142), X(0x13e), X(0x13b), X(0x137), X(0x134), X(0x130),
            X(0x12c), X(0x129), X(0x125), X(0x122), X(0x11e), X(0x11b), X(0x117), X(0x114),
            X(0x110), X(0x10c), X(0x109), X(0x106), X(0x102), X(0x0ff), X(0x0fb), X(0x0f8),
            X(0x0f4), X(0x0f1), X(0x0ed), X(0x0ea), X(0x0e7), X(0x0e3), X(0x0e0), X(0x0dc),
            X(0x0d9), X(0x0d6), X(0x0d2), X(0x0cf), X(0x0cc), X(0x0c8), X(0x0c5), X(0x0c2),
            X(0x0be), X(0x0bb), X(0x0b8), X(0x0b5), X(0x0b1), X(0x0ae), X(0x0ab), X(0x0a8),
            X(0x0a4), X(0x0a1), X(0x09e), X(0x09b), X(0x098), X(0x094), X(0x091), X(0x08e),
            X(0x08b), X(0x088), X(0x085), X(0x082), X(0x07e), X(0x07b), X(0x078), X(0x075),
            X(0x072), X(0x06f), X(0x06c), X(0x069), X(0x066), X(0x063), X(0x060), X(0x05d),
            X(0x05a), X(0x057), X(0x054), X(0x051), X(0x04e), X(0x04b), X(0x048), X(0x045),
            X(0x042), X(0x03f), X(0x03c), X(0x039), X(0x036), X(0x033), X(0x030), X(0x02d),
            X(0x02a), X(0x028), X(0x025), X(0x022), X(0x01f), X(0x01c), X(0x019), X(0x016),
            X(0x014), X(0x011), X(0x00e), X(0x00b), X(0x008), X(0x006), X(0x003), X(0x000),
        };


        private static ushort X(int a) => (ushort)((a | 0x400) << 2);

        //-------------------------------------------------
        //  attenuation_to_volume - given a 5.8 fixed point
        //  logarithmic attenuation value, return a 13-bit
        //  linear volume
        //-------------------------------------------------
        public static uint AttenuationToVolume(uint input)
        {
            // look up the fractional part, then shift by the whole
            return (uint)(PowerTable[input & 0xff] >> (int)(input >> 8));
        }
    }
}
