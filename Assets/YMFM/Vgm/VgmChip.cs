using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Ymfm.Vgm
{
    // actual chip-specific implementation class; includes implementation of the
    // ymfm_interface as needed for vgmplay purposes
    public class VgmChip<TChip, TOutput> : BaseVgmChip, IYmfmInterface
        where TChip : IChip<TOutput>
        where TOutput : unmanaged, IOutput
    {
        // construction
        public VgmChip(uint clock, ChipType type, string name)
        {
            _type = type;
            _name = name ?? throw new ArgumentNullException(nameof(name));
            _chip = (TChip)Activator.CreateInstance(typeof(TChip), this);
            // I don't want to use reflection, but C# doesn't let you construct generics (except with new())

            _data = new byte[(int)AccessClass.AccessClasses][];
            for (var i = 0; i < _data.Length; ++i)
            {
                _data[i] = Array.Empty<byte>();
            }

            _queue = new Queue<(uint, byte)>();


            _clock = clock;
            _clocks = 0;
            _step = (long)(0x100000000ul / _chip.SampleRate(clock));
            _pos = 0;
            _chip.Reset();
        }

        public override uint SampleRate
        {
            [Pure]
            get => _chip.SampleRate(_clock);
        }

        // handle a register write: just queue for now
        public override void Write(uint reg, byte data)
        {
            _queue.Enqueue((reg, data));
        }

        // generate one output sample of output
        public override unsafe void Generate(long outputStart, long outputStep, Span<int> buffer)
        {
            uint addr1 = 0xffff, addr2 = 0xffff;
            byte data1 = 0, data2 = 0;

            // see if there is data to be written; if so, extract it and dequeue
            if (_queue.Count > 0)
            {
                var (first, second) = _queue.Dequeue();
                addr1 = 0 + 2 * ((first >> 8) & 3);
                data1 = (byte)(first & 0xffu);
                addr2 = addr1 + (_type == ChipType.Ym2149 ? 2u : 1u);
                data2 = second;
            }

            // write to the chip
            if (addr1 != 0xffff)
            {
                // if (LOG_WRITES)
                //     Debug.LogFormat("%10.5f: %s %03X=%02X", (double)(m_clocks) / (double)(m_chip.SampleRate(m_clock)),
                //         m_name,
                //         data1, data2);
                _chip.Write(addr1, data1);
                _chip.Write(addr2, data2);
            }


            // generate at the appropriate sample rate

            fixed (TOutput* output = &_output)
            {
                var span = new Span<TOutput>(output, 1);
                for (; _pos <= outputStart; _pos += _step)
                {
                    _chip.Generate(span);
                }
            }

            var data = _output.Data;

            fixed (int* ptr = buffer)
            {
                // add the final result to the buffer
                var bufferPtr = ptr;
                switch (_type)
                {
                    case ChipType.Ym2203:
                    {
                        var out0 = _output.Data[0];
                        var out1 = _output.Data[(int)(1 % _chip.Outputs)];
                        var out2 = _output.Data[(int)(2 % _chip.Outputs)];
                        var out3 = _output.Data[(int)(3 % _chip.Outputs)];
                        *bufferPtr++ += out0 + out1 + out2 + out3;
                        *bufferPtr++ += out0 + out1 + out2 + out3;
                        break;
                    }
                    case ChipType.Ym2608 or ChipType.Ym2610:
                    {
                        var out0 = _output.Data[0];
                        var out1 = _output.Data[(int)(1 % _chip.Outputs)];
                        var out2 = _output.Data[(int)(2 % _chip.Outputs)];
                        *bufferPtr++ += out0 + out2;
                        *bufferPtr++ += out1 + out2;
                        break;
                    }
                    case ChipType.Ymf278B:
                        *bufferPtr++ += _output.Data[4];
                        *bufferPtr++ += _output.Data[5];
                        break;
                    case var _ when _chip.Outputs == 1:
                        *bufferPtr++ += _output.Data[0];
                        *bufferPtr++ += _output.Data[0];
                        break;
                    default:
                        *bufferPtr++ += _output.Data[0];
                        *bufferPtr++ += _output.Data[(int)(1 % _chip.Outputs)];

                        break;
                }
            }

            _clocks++;
        }

        // write data to the ADPCM-A buffer
        public override void WriteData(AccessClass type, uint @base, ReadOnlySpan<byte> src)
        {
            var end = @base + src.Length;
            if (end > _data[(int)type].Length)
            {
                Array.Resize(ref _data[(int)type], (int)end);
            }

            var span = _data[(int)type].AsSpan((int)@base);
            src.CopyTo(span);
        }

        // seek within the PCM stream
        public override void SeekPcm(uint pos)
        {
            _pcmOffset = pos;
        }

        public override byte ReadPcm()
        {
            var pcm = _data[(int)AccessClass.Pcm];
            return (byte)((_pcmOffset < pcm.Length) ? pcm[(int)_pcmOffset++] : 0);
        }


        // handle a read from the buffer
        public byte ExternalRead(AccessClass type, uint offset)
        {
            var data = _data[(int)type];
            return (byte)((offset < data.Length) ? data[offset] : 0);
        }

        IEngineCallbacks IYmfmInterface.Engine { get; set; }

        public override ChipType Type => _type;

        // internal state
        private readonly ChipType _type;
        private string _name;
        private readonly byte[][] _data;
        private uint _pcmOffset;
        private TChip _chip;
        private readonly uint _clock;
        private ulong _clocks;
        private TOutput _output;
        private readonly long _step;
        private long _pos;
        private readonly Queue<(uint, byte)> _queue;
    };
}
