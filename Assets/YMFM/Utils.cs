using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;

namespace Ymfm
{
    public static partial class Utils
    {
        //-------------------------------------------------
        //  bitfield - extract a bitfield from the given
        //  value, starting at bit 'start' for a length of
        //  'length' bits
        //-------------------------------------------------
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Bitfield(uint value, int start, int length = 1)
        {
            return (value >> start) & ((1u << length) - 1);
        }

        //-------------------------------------------------
        //  bitfield - extract a bitfield from the given
        //  value, starting at bit 'start' for a length of
        //  'length' bits
        //-------------------------------------------------
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Bit(uint value, int bit)
        {
            return ((value >> bit) & ((1u << 1) - 1)) != 0;
        }
        
        //-------------------------------------------------
        //  roundtrip_fp - compute the result of a round
        //  trip through the encode/decode process above
        //-------------------------------------------------

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short RoundtripFp(int value)
        {
            // handle overflows first
            if (value < short.MinValue)
                return short.MinValue;
            if (value > short.MaxValue)
                return short.MaxValue;

            // we need to count the number of leading sign bits after the sign
            // we can use count_leading_zeros if we invert negative values
            var scanvalue = value ^ (value >> 31);

            // exponent is related to the number of leading bits starting from bit 14
            var exponent = 7 - math.lzcnt(scanvalue << 17);

            // smallest exponent value allowed is 1
            exponent = Mathf.Max(exponent, 1);

            // apply the shift back and forth to zero out bits that are lost
            exponent -= 1;
            return (short)((value >> exponent) << exponent);
        }

        //-------------------------------------------------
        //  detune_adjustment - given a 5-bit key code
        //  value and a 3-bit detune parameter, return a
        //  6-bit signed phase displacement; this table
        //  has been verified against Nuked's equations,
        //  but the equations are rather complicated, so
        //  we'll keep the simplicity of the table
        //-------------------------------------------------
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DetuneAdjustment(uint detune, uint keycode)
        {
            int result = DetuneAdjustmentTable[keycode, detune & 3];
            return Bit(detune, 2) ? -result : result;
        }

        //-------------------------------------------------
        //  abs_sin_attenuation - given a sin (phase) input
        //  where the range 0-2*PI is mapped onto 10 bits,
        //  return the absolute value of sin(input),
        //  logarithmically-adjusted and treated as an
        //  attenuation value, in 4.8 fixed point format
        //-------------------------------------------------
        public static uint AbsSinAttenuation(uint input)
        {
            // if the top bit is set, we're in the second half of the curve
            // which is a mirror image, so invert the index
            if (Bit(input, 8))
            {
                input = ~input;
            }

            // return the value from the table
            return SineTable[input & 0xff];
        }


        //-------------------------------------------------
        //  opm_key_code_to_phase_step - converts an
        //  OPM concatenated block (3 bits), keycode
        //  (4 bits) and key fraction (6 bits) to a 0.10
        //  phase step, after applying the given delta;
        //  this applies to OPM and OPZ, so it lives here
        //  in a central location
        //-------------------------------------------------
        public static uint OpmKeyCodeToPhaseStep(uint blockFreq, int delta)
        {
            // extract the block (octave) first
            var block = Bitfield(blockFreq, 10, 3);

            // the keycode (bits 6-9) is "gappy", mapping 12 values over 16 in each
            // octave; to correct for this, we multiply the 4-bit value by 3/4 (or
            // rather subtract 1/4); note that a (invalid) value of 15 will bleed into
            // the next octave -- this is confirmed
            var adjustedCode = Bitfield(blockFreq, 6, 4) - Bitfield(blockFreq, 8, 2);

            // now re-insert the 6-bit fraction
            var effFreq = (int)((adjustedCode << 6) | Bitfield(blockFreq, 0, 6));

            // now that the gaps are removed, add the delta
            effFreq += delta;

            // handle over/underflow by adjusting the block:
            if ((uint)effFreq >= 768)
            {
                // minimum delta is -512 (PM), so we can only underflow by 1 octave
                if (effFreq < 0)
                {
                    effFreq += 768;
                    if (block-- == 0)
                    {
                        return PhaseStep[0] >> 7;
                    }
                }

                // maximum delta is +512+608 (PM+detune), so we can overflow by up to 2 octaves
                else
                {
                    effFreq -= 768;
                    if (effFreq >= 768)
                    {
                        block++;
                        effFreq -= 768;
                    }

                    if (block++ >= 7)
                    {
                        return PhaseStep[767];
                    }
                }
            }

            // look up the phase shift for the key code, then shift by octave
            return PhaseStep[effFreq] >> (int)(block ^ 7);
        }
    }
}
