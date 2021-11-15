using System;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics.FixedPoint;
using UnityEngine;
using Ymfm.Opm;
using Object = UnityEngine.Object;

namespace Ymfm.Vgm
{
    public sealed class VgmPlayer : IDisposable
    {
        public const int Frequency = 44100;

        private readonly List<BaseVgmChip> _chips;
        private readonly uint _frequency;

        private VgmFile _file;
        private bool _disposed;
        private AudioClip _clip;
        private BinaryReader _reader;
        private MemoryStream _stream;
        private int _delay;
        private long _trackPosition;
        private readonly long _outputStep;

        internal VgmPlayer(VgmFile file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            _file = file;
            _chips = new List<BaseVgmChip>(2);
            _stream = new MemoryStream(_file.DataArray, false);
            _reader = new BinaryReader(_stream);
            _frequency = Frequency;
            _trackPosition = 0;
            _outputStep = (long)(0x100000000ul / _frequency);
            CreateChips();
        }

        public AudioClip Clip
        {
            get
            {
                if (!_disposed && _clip == null)
                {
                    ref var header = ref _file.Header;
                    var name = _file.Tags?.TrackName ?? _file.name;
                    var loops = header.LoopBase;
                    var length = header.TotalNumSamples + (header.LoopNumSamples * loops);
                    // total samples + (loop samples * [number of loops - 1])
                    if (length == 0)
                    { // Default to 1 second if we couldn't compute a length
                        length = _frequency;
                        // No need to account for the channels, Unity does that
                    }

                    _clip = AudioClip.Create(name, (int)length, 2, Frequency, true, OnReadData, OnPositionSet);
                }

                return _clip;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_clip != null)
                {
                    if (Application.isPlaying)
                    {
                        Object.Destroy(_clip);
                    }
                    else
                    {
                        Object.DestroyImmediate(_clip);
                    }
                }

                _file = null;
                _stream?.Dispose();
                _reader?.Dispose();
                _stream = null;
                _reader = null;
                _clip = null;
                _disposed = true;
            }
        }

        private void CreateChips()
        {
            var header = _file.Header;

            if (header.Version >= 0x110 && header.Ym2151Clock != 0)
            {
                AddChips<Ym2151>(header.Ym2151Clock, ChipType.Ym2151, "YM2151");
            }
        }

        private void AddChips<TChip>(uint clock, ChipType type, string chipName) where TChip : IChip
        {
            var clockValue = clock & 0x3fffffff;
            var numberOfChips = (clock & 0x40000000) != 0 ? 2 : 1;
            for (var index = 0; index < numberOfChips; index++)
            {
                _chips.Add(
                    new VgmChip<TChip>(
                        clockValue,
                        type,
                        (numberOfChips == 1) ? chipName : $"{chipName} #{index}"
                    )
                );
            }

            // YM2608 not supported (it needs a ROM that I can't redistribute)
        }


        private void OnReadData(float[] outputData)
        {
            // the offset was set in OnPositionSet
            var buffer = _file.Data.Span;
            var done = false;
            Span<int> outputs = stackalloc int[2];
            var outputOffset = 0;
            while (!done && _stream.Position < _stream.Length && outputOffset < outputData.Length)
            {
                var cmd = _reader.ReadByte();
                ParseInstruction(cmd, buffer, ref done);

                // handle delays
                foreach (var chip in _chips)
                {
                    chip.Generate(_trackPosition, _outputStep, outputs);
                }

                outputData[outputOffset] = outputs[0] / (float)short.MaxValue;
                outputData[outputOffset + 1] = outputs[1] / (float)short.MaxValue;
                outputOffset += 2;
                _trackPosition += 2;
            }
            // while (!done && _stream.Position < buffer.Length && outputDataIndex < outputData.Length)
            // {
            //     if (_delay > 0)
            //     {
            //         --_delay;
            //         continue;
            //     }
            //
            //     var cmd = _reader.ReadByte();
            //     ParseInstruction(cmd, buffer, ref done);
            //
            //     // handle delays
            //
            //     foreach (var chip in _chips)
            //     {
            //         chip.Generate(_position, outputStep, outputs);
            //     }
            //
            //     _position += outputStep;
            //     outputData[outputDataIndex] = outputs[0] / (float)short.MaxValue;
            //     outputData[outputDataIndex + 1] = outputs[1] / (float)short.MaxValue;
            //     outputDataIndex += 2;
            // }

            // TODO: The hard part will be looping (although if onpositionset is called a lot, i could use modulo or something)
        }

        private void ParseInstruction(byte cmd, ReadOnlySpan<byte> buffer, ref bool done)
        {
            switch (cmd)
            {
                // YM2413, write value dd to register aa
                case 0x51 or 0xa1:
                {
                    var register = _reader.ReadByte();
                    var data = _reader.ReadByte();
                    WriteChip(ChipType.Ym2413, (byte)(cmd >> 7), register, data);
                    // (cmd >> 7) gets the index (the higher operand opcode is used to write to the second chip)
                    break;
                }

                // YM2612 port 0, write value dd to register aa
                case 0x52 or 0xa2:
                {
                    var register = _reader.ReadByte();
                    var data = _reader.ReadByte();
                    WriteChip(ChipType.Ym2612, (byte)(cmd >> 7), register, data);
                    break;
                }

                // YM2612 port 1, write value dd to register aa
                case 0x53 or 0xa3:
                {
                    var register = _reader.ReadByte();
                    var data = _reader.ReadByte();
                    WriteChip(ChipType.Ym2612, (byte)(cmd >> 7), register | 0x100u, data);
                    break;
                }

                // YM2151, write value dd to register aa
                case 0x54 or 0xa4:
                {
                    var register = _reader.ReadByte();
                    var data = _reader.ReadByte();
                    WriteChip(ChipType.Ym2151, (byte)(cmd >> 7), register, data);
                    break;
                }

                // YM2203, write value dd to register aa
                case 0x55 or 0xa5:
                {
                    var register = _reader.ReadByte();
                    var data = _reader.ReadByte();
                    WriteChip(ChipType.Ym2203, (byte)(cmd >> 7), register, data);
                    break;
                }

                // YM2608 port 0, write value dd to register aa
                case 0x56 or 0xa6:
                {
                    var register = _reader.ReadByte();
                    var data = _reader.ReadByte();
                    WriteChip(ChipType.Ym2608, (byte)(cmd >> 7), register, data);
                    break;
                }

                // YM2608 port 1, write value dd to register aa
                case 0x57 or 0xa7:
                {
                    var register = _reader.ReadByte();
                    var data = _reader.ReadByte();
                    WriteChip(ChipType.Ym2608, (byte)(cmd >> 7), register | 0x100u, data);
                    break;
                }

                // YM2610 port 0, write value dd to register aa
                case 0x58 or 0xa8:
                {
                    var register = _reader.ReadByte();
                    var data = _reader.ReadByte();
                    WriteChip(ChipType.Ym2610, (byte)(cmd >> 7), register, data);
                    break;
                }

                // YM2610 port 1, write value dd to register aa
                case 0x59 or 0xa9:
                {
                    var register = _reader.ReadByte();
                    var data = _reader.ReadByte();
                    WriteChip(ChipType.Ym2610, (byte)(cmd >> 7), (uint)(register | 0x100u), data);
                    break;
                }

                // YM3812, write value dd to register aa
                case 0x5a or 0xaa:
                {
                    var register = _reader.ReadByte();
                    var data = _reader.ReadByte();
                    WriteChip(ChipType.Ym3812, (byte)(cmd >> 7), register, data);
                    break;
                }

                // YM3526, write value dd to register aa
                case 0x5b or 0xab:
                {
                    var register = _reader.ReadByte();
                    var data = _reader.ReadByte();
                    WriteChip(ChipType.Ym3526, (byte)(cmd >> 7), register, data);
                    break;
                }

                // Y8950, write value dd to register aa
                case 0x5c or 0xac:
                {
                    var register = _reader.ReadByte();
                    var data = _reader.ReadByte();
                    WriteChip(ChipType.Y8950, (byte)(cmd >> 7), register, data);
                    break;
                }

                // YMF262 port 0, write value dd to register aa
                case 0x5e or 0xae:
                {
                    var register = _reader.ReadByte();
                    var data = _reader.ReadByte();
                    WriteChip(ChipType.Ymf262, (byte)(cmd >> 7), register, data);
                    break;
                }

                // YMF262 port 1, write value dd to register aa
                case 0x5f or 0xaf:
                {
                    var register = _reader.ReadByte();
                    var data = _reader.ReadByte();
                    WriteChip(ChipType.Ymf262, (byte)(cmd >> 7), register | 0x100u, data);
                    break;
                }

                // Wait n samples, n can range from 0 to 65535 (approx 1.49 seconds)
                case 0x61:
                {
                    _delay = _reader.ReadUInt16();
                    break;
                }

                // wait 735 samples (60th of a second), a shortcut for 0x61 0xdf 0x02
                case 0x62:
                {
                    _delay = 735;
                    break;
                }

                // wait 882 samples (50th of a second), a shortcut for 0x61 0x72 0x03
                case 0x63:
                {
                    _delay = 882;
                    break;
                }

                // end of sound data
                case 0x66:
                {
                    done = true;
                    break;
                }

                // data block
                case 0x67:
                {
                    ReadDataBlock(buffer, _reader);
                    break;
                }

                // PCM RAM write
                case 0x68:
                {
                    ParsePcmRam(buffer, _reader);
                    break;
                }

                // AY8910, write value dd to register aa
                case 0xa0:
                {
                    var register = _reader.ReadByte();
                    var data = _reader.ReadByte();
                    WriteChip(ChipType.Ym2149, (byte)(register >> 7), (byte)(register & 0x7f), data);
                    break;
                }

                // pp aa dd: YMF278B, port pp, write value dd to register aa
                case 0xd0:
                {
                    var port = _reader.ReadByte();
                    var register = _reader.ReadByte();
                    var data = _reader.ReadByte();
                    WriteChip(ChipType.Ymf278B, (byte)(port >> 7), ((port & 0x7fu) << 8) | register, data);
                    break;
                }

                // wait n+1 samples, n can range from 0 to 15.
                case >= 0x70 and <= 0x7f:
                {
                    _delay = (cmd & 15) + 1;
                    break;
                }

                // YM2612 port 0 address 2A write from the data bank, then wait n samples; n can range from 0 to 15.
                // Note that the wait is n, NOT n+1. See also command 0xE0.
                case >= 0x80 and <= 0x8f:
                {
                    var chip = FindChip(ChipType.Ym2612, 0);
                    chip?.Write(0x2a, chip.ReadPcm());
                    _delay = cmd & 15;
                    break;
                }

                // reserved or unsupported commands, consume one byte
                case (>= 0x30 and <= 0x3f) or 0x4f or 0x50:
                {
                    _reader.ReadByte();
                    break;
                }

                // reserved or unsupported commands, consume two bytes
                case (>= 0x40 and <= 0x4e) or 0x5d or (>= 0xb0 and <= 0xbf):
                {
                    _reader.ReadUInt16();
                    break;
                }

                // reserved or unsupported commands, consume three bytes
                case (>= 0xc0 and <= 0xcf) or (>= 0xd1 and <= 0xdf):
                {
                    _reader.ReadByte();
                    _reader.ReadByte();
                    _reader.ReadByte();
                    break;
                }

                // dddddddd: Seek to offset dddddddd (Intel byte order) in PCM data bank of data block type 0 (YM2612).
                case 0xe0:
                {
                    var chip = FindChip(ChipType.Ym2612, 0);
                    var pos = _reader.ReadUInt32();
                    chip?.SeekPcm(pos);
                    break;
                }

                // reserved or unsupported commands, consume four bytes
                case (>= 0xe1 and <= 0xff):
                {
                    _reader.ReadUInt32();
                    break;
                }
            }
        }

        private void OnPositionSet(int pos)
        {
            _trackPosition = pos;
            // TODO: Handle looping
        }

        private static void ParsePcmRam(ReadOnlySpan<byte> buffer, BinaryReader reader)
        {
            if (reader.ReadByte() != 0x66)
            { // If this isn't a valid sound block...
                return;
            }

            // TODO: Get the size of the PCM RAM write, then skip it
            var chipType = reader.ReadByte();
            Span<byte> size = stackalloc byte[3];
            reader.Read(size);


            Debug.Log("0x68: PCM RAM write");
            return;
        }


        private unsafe void ReadDataBlock(ReadOnlySpan<byte> buffer, BinaryReader reader)
        {
            if (reader.ReadByte() != 0x66)
            { // If this isn't a valid sound block...
                return;
            }

            var type = reader.ReadByte();
            var size = reader.ReadUInt32();
            var localOffset = reader.BaseStream.Position;

            fixed (byte* ptr = buffer)
            {
                using var stream = new UnmanagedMemoryStream(ptr, buffer.Length);
                stream.Position = localOffset;

                using var localReader = new BinaryReader(stream);


                switch (type)
                {
                    // PCM data for use with associated commands (unsupported)
                    case >= 0x01 and <= 0x07:
                        break;

                    case 0x00: // YM2612 PCM data for use with associated commands
                    {
                        var chip = FindChip(ChipType.Ym2612, 0);
                        chip?.WriteData(AccessClass.Pcm, 0, buffer[(int)localOffset..]);

                        break;
                    }

                    case 0x82: // YM2610 ADPCM ROM data
                    {
                        AddRomData(ChipType.Ym2610, AccessClass.AdpcmA, localReader, buffer, size - 8);
                        break;
                    }

                    case 0x81: // YM2608 DELTA-T ROM data
                    {
                        AddRomData(ChipType.Ym2608, AccessClass.AdpcmB, localReader, buffer, size - 8);
                        break;
                    }

                    case 0x83: // YM2610 DELTA-T ROM data
                    {
                        AddRomData(ChipType.Ym2610, AccessClass.AdpcmB, localReader, buffer, size - 8);
                        break;
                    }

                    case 0x84: // YMF278B ROM data
                    case 0x87: // YMF278B RAM data
                    {
                        AddRomData(ChipType.Ymf278B, AccessClass.Pcm, localReader, buffer, size - 8);
                        break;
                    }

                    case 0x88: // Y8950 DELTA-T ROM data
                    {
                        AddRomData(ChipType.Y8950, AccessClass.AdpcmB, localReader, buffer, size - 8);
                        break;
                    }

                    // ROM data, unsupported
                    case 0x80 or 0x85 or 0x86 or (>= 0x89 and <= 0x93):
                        break;

                    // RAM write, unsupported
                    case (>= 0xC0 and <= 0xC2) or 0xE0 or 0xE1:
                        break;

                    // Compressed data block, unsupported
                    case (>= 0x40 and <= 0x7E): break;

                    // Unknown data block type
                    default:
                        break;
                }
            }

            Span<byte> skipped = stackalloc byte[(int)size];
            reader.Read(skipped);
        }

        //-------------------------------------------------
        //  write_chip - handle a write to the given chip
        //  and index
        //-------------------------------------------------
        private void WriteChip(ChipType type, byte index, uint reg, byte data)
        {
            FindChip(type, index)?.Write(reg, data);
        }


        //-------------------------------------------------
        //  find_chip - find the given chip and index
        //-------------------------------------------------
        private BaseVgmChip FindChip(ChipType type, byte index)
        {
            foreach (var chip in _chips)
            {
                if (chip.Type == type && index-- == 0)
                {
                    return chip;
                }
            }

            return null;
        }

        //-------------------------------------------------
        //  add_rom_data - add data to the given chip
        //  type in the given access class
        //-------------------------------------------------
        private void AddRomData(
            ChipType type,
            AccessClass access,
            BinaryReader reader,
            ReadOnlySpan<byte> buffer,
            uint size
        )
        {
            var length = reader.ReadUInt32();
            var start = reader.ReadUInt32();
            var slice = buffer.Slice((int)reader.BaseStream.Position, (int)size);
            for (var index = 0; index < 2; index++)
            {
                var chip = FindChip(type, (byte)index);
                chip?.WriteData(access, start, slice);
            }
        }
    }
}
