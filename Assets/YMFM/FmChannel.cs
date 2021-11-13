using System;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Ymfm
{
    // fm_channel represents an FM channel which combines the output of 2 or 4
    // operators into a final result
    public sealed class FmChannel<TRegisterType, TOutputType, TOperatorMapping>
        where TRegisterType : class, IFmRegisters<TOperatorMapping>, new()
        where TOutputType : struct, IOutput
        where TOperatorMapping : struct, IOperatorMapping
    {
        // constructor
        public FmChannel(FmEngineBase<TRegisterType, TOutputType, TOperatorMapping> owner, uint channelOffset)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            Registers = owner.Registers;
            ChannelOffset = channelOffset;
            _feedbackMemory = new short[] { 0, 0 };
            _feedbackInput = 0;
            _operators = new FmOperator<TRegisterType, TOutputType, TOperatorMapping>[] { null, null, null, null };
        }

        //-------------------------------------------------
        //  save_restore - save or restore the data
        //-------------------------------------------------
        public void SaveRestore(YmfmSavedState state)
        {
            state.SaveRestore(ref _feedbackMemory[0]);
            state.SaveRestore(ref _feedbackMemory[1]);
            state.SaveRestore(ref _feedbackInput);
        }

        // reset the channel state
        public void Reset()
        {
            // reset our data
            _feedbackMemory[0] = _feedbackMemory[1] = 0;
            _feedbackInput = 0;
        }

        // return the channel offset
        public uint ChannelOffset { get; }

        // assign operators
        public void Assign(uint index, FmOperator<TRegisterType, TOutputType, TOperatorMapping> op)
        {
            Assert.IsTrue(index < _operators.Length);
            _operators[index] = op;
            if (op != null)
            {
                op.ChannelOffset = ChannelOffset;
            }
        }

        // signal key on/off to our operators
        public void KeyOnOff(uint states, KeyOnType type, uint channelNumber)
        {
            for (var opnum = 0; opnum < _operators.Length; opnum++)
            {
                _operators[opnum]?.KeyOnOff(Utils.Bitfield(states, opnum), type);
            }
        }

        // prepare prior to clocking
        public bool Prepare()
        {
            uint activeMask = 0;

            // prepare all operators and determine if they are active
            for (var opNum = 0; opNum < _operators.Length; opNum++)
            {
                if (_operators[opNum] != null && _operators[opNum].Prepare())
                {
                    activeMask |= 1u << opNum;
                }
            }

            return (activeMask != 0);
        }

        //-------------------------------------------------
        //  clock - master clock of all operators
        //-------------------------------------------------
        public void Clock(uint envCounter, int lfoRawPm)
        {
            // clock the feedback through
            _feedbackMemory[0] = _feedbackMemory[1];
            _feedbackMemory[1] = _feedbackInput;

            foreach (var t in _operators)
            {
                t?.Clock(envCounter, lfoRawPm);
            }
        }

        //-------------------------------------------------
        //  output_2op - combine 4 operators according to
        //  the specified algorithm, returning a sum
        //  according to the rshift and clipmax parameters,
        //  which vary between different implementations
        //-------------------------------------------------
        public void Output2Op(ref TOutputType output, uint rshift, int clipMax)
        {
            // The first 2 operators should be populated
            Assert.IsNotNull(_operators[0]);
            Assert.IsNotNull(_operators[1]);

            // AM amount is the same across all operators; compute it once
            var amOffset = Registers.LfoAmOffset(ChannelOffset);

            // operator 1 has optional self-feedback
            var opMod = 0;
            var feedback = Registers.ChannelFeedback(ChannelOffset);
            if (feedback != 0)
            {
                opMod = (_feedbackMemory[0] + _feedbackMemory[1]) >> (int)(10 - feedback);
            }

            // compute the 14-bit volume/value of operator 1 and update the feedback
            int op1Value = _feedbackInput =
                (short)_operators[0].ComputeVolume((uint)(_operators[0].Phase + opMod), amOffset);

            // now that the feedback has been computed, skip the rest if all volumes
            // are clear; no need to do all this work for nothing
            if (Registers.ChannelOutputAny(ChannelOffset) == 0)
            {
                return;
            }

            // Algorithms for two-operator case:
            //    0: O1 -> O2 -> out
            //    1: (O1 + O2) -> out
            int result;
            if (Utils.Bit(Registers.ChannelAlgorithm(ChannelOffset), 0))
            {
                // some OPL chips use the previous sample for modulation instead of
                // the current sample
                opMod = (Registers.ModulatorDelay ? _feedbackMemory[1] : op1Value) >> 1;
                result = _operators[1].ComputeVolume((uint)(_operators[1].Phase + opMod), amOffset) >> (int)rshift;
            }
            else
            {
                result = op1Value + (_operators[1].ComputeVolume(_operators[1].Phase, amOffset) >> (int)rshift);
                var clipMin = -clipMax - 1;
                result = math.clamp(result, clipMin, clipMax);
            }

            // add to the output
            AddToOutput(ChannelOffset, ref output, result);
        }

        //-------------------------------------------------
        //  output_4op - combine 4 operators according to
        //  the specified algorithm, returning a sum
        //  according to the rshift and clipmax parameters,
        //  which vary between different implementations
        //-------------------------------------------------
        public void Output4Op(ref TOutputType output, uint rshift, int clipMax)
        {
            // all 4 operators should be populated
            Assert.IsNotNull(_operators[0]);
            Assert.IsNotNull(_operators[1]);
            Assert.IsNotNull(_operators[2]);
            Assert.IsNotNull(_operators[3]);

            // AM amount is the same across all operators; compute it once
            var amOffset = Registers.LfoAmOffset(ChannelOffset);

            // operator 1 has optional self-feedback
            var opMod = 0;
            var feedback = Registers.ChannelFeedback(ChannelOffset);
            if (feedback != 0)
            {
                opMod = (_feedbackMemory[0] + _feedbackMemory[1]) >> (int)(10 - feedback);
            }

            // compute the 14-bit volume/value of operator 1 and update the feedback
            int op1Value = _feedbackInput =
                (short)_operators[0].ComputeVolume((uint)(_operators[0].Phase + opMod), amOffset);

            // now that the feedback has been computed, skip the rest if all volumes
            // are clear; no need to do all this work for nothing
            if (Registers.ChannelOutputAny(ChannelOffset) == 0)
                return;


            uint algorithmOps = Tables.AlgorithmOps[Registers.ChannelAlgorithm(ChannelOffset)];

            // populate the opout table
            Span<short> opOut = stackalloc short[8];
            opOut[0] = 0;
            opOut[1] = (short)op1Value;

            // compute the 14-bit volume/value of operator 2
            opMod = opOut[(int)Utils.Bitfield(algorithmOps, 0, 1)] >> 1;
            opOut[2] = (short)_operators[1].ComputeVolume((uint)(_operators[1].Phase + opMod), amOffset);
            opOut[5] = (short)(opOut[1] + opOut[2]);

            // compute the 14-bit volume/value of operator 3
            opMod = opOut[(int)Utils.Bitfield(algorithmOps, 1, 3)] >> 1;
            opOut[3] = (short)_operators[2].ComputeVolume((uint)(_operators[2].Phase + opMod), amOffset);
            opOut[6] = (short)(opOut[1] + opOut[3]);
            opOut[7] = (short)(opOut[2] + opOut[3]);

            // compute the 14-bit volume/value of operator 4; this could be a noise
            // value on the OPM; all algorithms consume OP4 output at a minimum
            int result;
            if (Registers.NoiseEnable != 0 && ChannelOffset == 7)
            {
                result = _operators[3].ComputeNoiseVolume(amOffset);
            }
            else
            {
                opMod = opOut[(int)Utils.Bitfield(algorithmOps, 4, 3)] >> 1;
                result = _operators[3].ComputeVolume((uint)(_operators[3].Phase + opMod), amOffset);
            }

            result >>= (int)rshift;

            // optionally add OP1, OP2, OP3
            var clipMin = -clipMax - 1;
            if (Utils.Bit(algorithmOps, 7))
            {
                result = math.clamp(result + (opOut[1] >> (int)rshift), clipMin, clipMax);
            }

            if (Utils.Bit(algorithmOps, 8))
            {
                result = math.clamp(result + (opOut[2] >> (int)rshift), clipMin, clipMax);
            }

            if (Utils.Bit(algorithmOps, 9))
            {
                result = math.clamp(result + (opOut[3] >> (int)rshift), clipMin, clipMax);
            }

            // add to the output
            AddToOutput(ChannelOffset, ref output, result);
        }

        //-------------------------------------------------
        //  output_rhythm_ch6 - special case output
        //  computation for OPL channel 6 in rhythm mode,
        //  which outputs a Bass Drum instrument
        //-------------------------------------------------
        public void OutputRhythmChannel6(ref TOutputType output, uint rshift, int clipMax)
        {
            // AM amount is the same across all operators; compute it once
            var amOffset = Registers.LfoAmOffset(ChannelOffset);

            // Bass Drum: this uses operators 12 and 15 (i.e., channel 6)
            // in an almost-normal way, except that if the algorithm is 1,
            // the first operator is ignored instead of added in

            // operator 1 has optional self-feedback
            var opMod = 0;
            var feedback = Registers.ChannelFeedback(ChannelOffset);
            if (feedback != 0)
            {
                opMod = (_feedbackMemory[0] + _feedbackMemory[1]) >> (int)(10 - feedback);
            }

            // compute the 14-bit volume/value of operator 1 and update the feedback
            int opOut1 = _feedbackInput =
                (short)_operators[0].ComputeVolume((uint)(_operators[0].Phase + opMod), amOffset);

            // compute the 14-bit volume/value of operator 2, which is the result
            opMod = Utils.Bit(Registers.ChannelAlgorithm(ChannelOffset), 0) ? 0 : (opOut1 >> 1);
            var result = _operators[1].ComputeVolume((uint)(_operators[1].Phase + opMod), amOffset) >> (int)rshift;

            // add to the output
            AddToOutput(ChannelOffset, ref output, result * 2);
        }

        //-------------------------------------------------
        //  output_rhythm_ch7 - special case output
        //  computation for OPL channel 7 in rhythm mode,
        //  which outputs High Hat and Snare Drum
        //  instruments
        //-------------------------------------------------
        public void OutputRhythmChannel7(uint phaseSelect, ref TOutputType output, uint rshift, int clipMax)
        {
            // AM amount is the same across all operators; compute it once
            var amOffset = Registers.LfoAmOffset(ChannelOffset);
            var noiseState = Utils.Bitfield(Registers.NoiseState, 0);

            // High Hat: this uses the envelope from operator 13 (channel 7),
            // and a combination of noise and the operator 13/17 phase select
            // to compute the phase
            var phase = (phaseSelect << 9) | 0xd0u >> (int)(2 * (noiseState ^ phaseSelect));
            var result = _operators[0].ComputeVolume(phase, amOffset) >> (int)rshift;

            // Snare Drum: this uses the envelope from operator 16 (channel 7),
            // and a combination of noise and operator 13 phase to pick a phase
            var op13Phase = _operators[0].Phase;
            phase = (0x100u << (int)Utils.Bitfield(op13Phase, 8)) ^ (noiseState << 8);
            result += _operators[1].ComputeVolume(phase, amOffset) >> (int)rshift;
            result = math.clamp(result, -clipMax - 1, clipMax);

            // add to the output
            AddToOutput(ChannelOffset, ref output, result * 2);
        }

        //-------------------------------------------------
        //  output_rhythm_ch8 - special case output
        //  computation for OPL channel 8 in rhythm mode,
        //  which outputs Tom Tom and Top Cymbal instruments
        //-------------------------------------------------
        public void OutputRhythmChannel8(uint phaseSelect, ref TOutputType output, uint rshift, int clipMax)
        {
            // AM amount is the same across all operators; compute it once
            var amOffset = Registers.LfoAmOffset(ChannelOffset);

            // Tom Tom: this is just a single operator processed normally
            var result = _operators[0].ComputeVolume(_operators[0].Phase, amOffset) >> (int)rshift;

            // Top Cymbal: this uses the envelope from operator 17 (channel 8),
            // and the operator 13/17 phase select to compute the phase
            var phase = 0x100 | (phaseSelect << 9);
            result += _operators[1].ComputeVolume(phase, amOffset) >> (int)rshift;
            result = math.clamp(result, -clipMax - 1, clipMax);

            // add to the output
            AddToOutput(ChannelOffset, ref output, result * 2);
        }

        // are we a 4-operator channel or a 2-operator one?
        public bool Is4Op => Registers.DynamicOps ? _operators[2] != null : Registers.Operators / Registers.Channels == 4;

        // return a reference to our registers
        [NotNull]
        public TRegisterType Registers { get; }


        // simple getters for debugging
        public FmOperator<TRegisterType, TOutputType, TOperatorMapping> DebugOperator(uint index)
        {
            return _operators[index];
        }

        // helper to add values to the outputs based on channel enables
        private void AddToOutput(uint channelOffset, ref TOutputType output, int value)
        {
            // create these constants to appease overzealous compilers checking array
            // bounds in unreachable code (looking at you, clang)
            const int out0Index = 0;
            var out1Index = 1 % Registers.Outputs;
            var out2Index = 2 % Registers.Outputs;
            var out3Index = 3 % Registers.Outputs;

            if (Registers.Outputs == 1 || Registers.ChannelOutput0(channelOffset) != 0)
            {
                output.Data[out0Index] += value;
            }

            if (Registers.Outputs >= 2 && Registers.ChannelOutput1(channelOffset) != 0)
            {
                output.Data[(int)out1Index] += value;
            }

            if (Registers.Outputs >= 3 && Registers.ChannelOutput2(channelOffset) != 0)
            {
                output.Data[(int)out2Index] += value;
            }

            if (Registers.Outputs >= 4 && Registers.ChannelOutput3(channelOffset) != 0)
            {
                output.Data[(int)out3Index] += value;
            }
        }

        // internal state
        private readonly short[] _feedbackMemory; // feedback memory for operator 1
        private short _feedbackInput; // next input value for op 1 feedback (set in output)
        private readonly FmOperator<TRegisterType, TOutputType, TOperatorMapping>[] _operators; // up to 4 operators
    }

    internal static partial class Tables
    {
        // OPM/OPN offer 8 different connection algorithms for 4 operators,
        // and OPL3 offers 4 more, which we designate here as 8-11.
        //
        // The operators are computed in order, with the inputs pulled from
        // an array of values (opout) that is populated as we go:
        //    0 = 0
        //    1 = O1
        //    2 = O2
        //    3 = O3
        //    4 = (O4)
        //    5 = O1+O2
        //    6 = O1+O3
        //    7 = O2+O3
        //
        // The s_algorithm_ops table describes the inputs and outputs of each
        // algorithm as follows:
        //
        //      ---------x use opout[x] as operator 2 input
        //      ------xxx- use opout[x] as operator 3 input
        //      ---xxx---- use opout[x] as operator 4 input
        //      --x------- include opout[1] in final sum
        //      -x-------- include opout[2] in final sum
        //      x--------- include opout[3] in final sum
        private static ushort ALGORITHM(
            ushort op2In,
            ushort op3In,
            ushort op4In,
            ushort op1Out,
            ushort op2Out,
            ushort op3Out
        )
        {
            return (ushort)((op2In) | ((op3In) << 1) | ((op4In) << 4) | ((op1Out) << 7) | ((op2Out) << 8) |
                            ((op3Out) << 9));
        }

        public static readonly ushort[] AlgorithmOps =
        {
            ALGORITHM(1, 2, 3, 0, 0, 0), //  0: O1 -> O2 -> O3 -> O4 -> out (O4)
            ALGORITHM(0, 5, 3, 0, 0, 0), //  1: (O1 + O2) -> O3 -> O4 -> out (O4)
            ALGORITHM(0, 2, 6, 0, 0, 0), //  2: (O1 + (O2 -> O3)) -> O4 -> out (O4)
            ALGORITHM(1, 0, 7, 0, 0, 0), //  3: ((O1 -> O2) + O3) -> O4 -> out (O4)
            ALGORITHM(1, 0, 3, 0, 1, 0), //  4: ((O1 -> O2) + (O3 -> O4)) -> out (O2+O4)
            ALGORITHM(1, 1, 1, 0, 1, 1), //  5: ((O1 -> O2) + (O1 -> O3) + (O1 -> O4)) -> out (O2+O3+O4)
            ALGORITHM(1, 0, 0, 0, 1, 1), //  6: ((O1 -> O2) + O3 + O4) -> out (O2+O3+O4)
            ALGORITHM(0, 0, 0, 1, 1, 1), //  7: (O1 + O2 + O3 + O4) -> out (O1+O2+O3+O4)
            ALGORITHM(1, 2, 3, 0, 0, 0), //  8: O1 -> O2 -> O3 -> O4 -> out (O4)         [same as 0]
            ALGORITHM(0, 2, 3, 1, 0, 0), //  9: (O1 + (O2 -> O3 -> O4)) -> out (O1+O4)   [unique]
            ALGORITHM(1, 0, 3, 0, 1, 0), // 10: ((O1 -> O2) + (O3 -> O4)) -> out (O2+O4) [same as 4]
            ALGORITHM(0, 2, 0, 1, 0, 1), // 11: (O1 + (O2 -> O3) + O4) -> out (O1+O3+O4) [unique]
        };
    }
}
