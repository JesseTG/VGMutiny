using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Ymfm.Opm;

namespace Ymfm.Vgm
{
    using uint32_t = UInt32;
    using uint8_t = System.Byte;
    using emulated_time = System.Int64;
    using int32_t = System.Int32;

    public class VgmPlayer
    {
        private const uint MagicNumber = 0x56676d20;

        private readonly List<BaseVgmChip> _chips;
        private readonly VgmHeader _header;
        public VgmHeader Header => _header;

        public VgmPlayer(ReadOnlySpan<byte> input)
        {
            _chips = new List<BaseVgmChip>(8);
            ParseHeader(input, out _header, out var dataStart);
            CreateChips();
            // TODO: Log warnings in the editor
        }

        private static unsafe bool ParseHeader(ReadOnlySpan<byte> input, out VgmHeader header, out uint dataStart)
        {
            switch (input.Length)
            {
                case 0:
                    throw new ArgumentException("Input is empty");
                case < 4:
                    throw new ArgumentException("No magic number");
            }

            fixed (byte* ptr = input)
            {
                using var stream = new UnmanagedMemoryStream(ptr, input.Length);
                using var reader = new BinaryReader(stream);

                header = default;
                dataStart = 0;

                // +00: check the magic ID 
                var magic = reader.ReadUInt32();

                if (magic != MagicNumber)
                { // Not a valid VGM file

                    return false;
                }

                // TODO: Put these warnings in the Unity editor


                // +04: parse the size
                header.EndOfFileOffset = reader.ReadUInt32();
                if (stream.Position - 4 + header.EndOfFileOffset > input.Length)
                { // File is smaller than specified size, it may have been truncated
                    Debug.LogWarning("Total size for file is too small; file may be truncated");
                    header.EndOfFileOffset = (uint)(input.Length - 4u);
                }

                // +08: parse the version
                header.Version = reader.ReadUInt32();
                if (header.Version > 0x171)
                {
                    Debug.LogWarning("Warning: version > 1.71 detected, some things may not work");
                }

                // +0C: SN76489 clock
                header.Sn76489Clock = reader.ReadUInt32();
                if (header.Sn76489Clock != 0)
                {
                    Debug.LogWarning(
                        $"Warning: clock for SN76489 specified ({header.Sn76489Clock:N}Hz), but not supported"
                    );
                }

                // +10: YM2413 clock
                header.Ym2413Clock = reader.ReadUInt32();

                // +14: GD3 offset
                header.Gd3Offset = reader.ReadUInt32();

                // +18: Total # samples
                header.TotalNumSamples = reader.ReadUInt32();

                // +1C: Loop offset
                header.LoopOffset = reader.ReadUInt32();

                // +20: Loop # samples
                header.LoopNumSamples = reader.ReadUInt32();

                // +24: Rate
                header.Rate = reader.ReadUInt32();

                // +28: SN76489 feedback
                header.Sn76489Feedback = reader.ReadUInt16();

                // +2A: SN76489 shift register width
                header.Sn76489ShiftRegisterWidth = reader.ReadByte();

                // +2B: SN76489 Flags
                header.Sn76489Flags = reader.ReadByte();

                // +2C: YM2612 clock
                header.Ym2612Clock = reader.ReadUInt32();

                // +30: YM2151 clock
                header.Ym2151Clock = reader.ReadUInt32();


                // +34: VGM data offset
                dataStart = reader.ReadUInt32();
                dataStart += (uint)(stream.Position - 4);
                if (header.Version < 0x150)
                {
                    dataStart = 0x40;
                }

                // +38: Sega PCM clock
                header.SegaPcmClock = reader.ReadUInt32();
                if (header.Version >= 0x151 && header.SegaPcmClock != 0)
                {
                    Debug.LogWarning("Warning: clock for Sega PCM specified, but not supported");
                }

                // +3C: Sega PCM interface register
                header.SegaPcmInterfaceRegister = reader.ReadUInt32();

                // +40: RF5C68 clock
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.Rf5c68Clock = reader.ReadUInt32();
                if (header.Version >= 0x151 && header.Rf5c68Clock != 0)
                {
                    Debug.LogWarning("Warning: clock for RF5C68 specified, but not supported");
                }

                // +44: YM2203 clock
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.Ym2203Clock = reader.ReadUInt32();

                // +48: YM2608 clock
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.Ym2608Clock = reader.ReadUInt32();

                // +4C: YM2610/2610B clock
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.Ym2610Clock = reader.ReadUInt32();

                // +50: YM3812 clock
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.Ym3812Clock = reader.ReadUInt32();

                // +54: YM3526 clock
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.Ym3526Clock = reader.ReadUInt32();

                // +58: Y8950 clock
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.Y8950Clock = reader.ReadUInt32();

                // +5C: YMF262 clock
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.Ymf262Clock = reader.ReadUInt32();

                // +60: YMF278B clock
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.Ymf278bClock = reader.ReadUInt32();

                // +64: YMF271 clock
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.Ymf271Clock = reader.ReadUInt32();
                if (header.Version >= 0x151 && header.Ymf271Clock != 0)
                {
                    Debug.LogWarning("Warning: clock for YMF271 specified, but not supported");
                }

                // +68: YMZ280B clock
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.Ymz280bClock = reader.ReadUInt32();
                if (header.Version >= 0x151 && header.Ymz280bClock != 0)
                {
                    Debug.LogWarning("Warning: clock for YMZ280B specified, but not supported");
                }

                // +6C: RF5C164 clock
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.Rf5c164Clock = reader.ReadUInt32();
                if (header.Version >= 0x151 && header.Rf5c164Clock != 0)
                {
                    Debug.LogWarning("Warning: clock for RF5C164 specified, but not supported");
                }

                // +70: PWM clock
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.PwmClock = reader.ReadUInt32();
                if (header.Version >= 0x151 && header.PwmClock != 0)
                {
                    Debug.LogWarning("Warning: clock for PWM specified, but not supported");
                }

                // +74: AY8910 clock
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.Ay8910Clock = reader.ReadUInt32();

                // +78: AY8910 chip type
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.Ay8910ChipType = reader.ReadByte();

                // +79: AY8910 flags
                header.Ay8910Flags = reader.ReadByte();

                // +7A: YM2203/AY8910 flags
                header.Ym2203Ay8910Flags = reader.ReadByte();

                // +7B: YM2608/AY8910 flags
                header.Ym2608Ay8910Flags = reader.ReadByte();

                // +7C: Volume modifier
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.VolumeModifier = reader.ReadByte();
                if (header.VolumeModifier != 0)
                {
                    Debug.LogFormat(
                        "Volume modifier: %{0:X2} (={1})",
                        header.VolumeModifier,
                        (int)(Mathf.Pow(2, (float)(header.VolumeModifier) / 0x20))
                    );
                }

                // +7D: Reserved
                if (reader.ReadByte() != 0)
                {
                    Debug.LogWarning("Expected a zero at reserved offset 0x7D");
                }

                // +7E: Loop base
                header.LoopBase = reader.ReadByte();

                // +7F: Loop modifier
                header.LoopModifier = reader.ReadByte();

                // +80: GameBoy DMG clock
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.GameBoyDmgClock = reader.ReadUInt32();
                if (header.Version >= 0x161 && header.GameBoyDmgClock != 0)
                {
                    Debug.LogWarning("Warning: clock for GameBoy DMG specified, but not supported");
                }

                // +84: NES APU clock
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.NesApuClock = reader.ReadUInt32();
                if (header.Version >= 0x161 && header.NesApuClock != 0)
                {
                    Debug.LogWarning("Warning: clock for NES APU specified, but not supported");
                }

                // +88: MultiPCM clock
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.MultiPcmClock = reader.ReadUInt32();
                if (header.Version >= 0x161 && header.MultiPcmClock != 0)
                {
                    Debug.LogWarning("Warning: clock for MultiPCM specified, but not supported");
                }

                // +8C: uPD7759 clock
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.Upd7759Clock = reader.ReadUInt32();
                if (header.Version >= 0x161 && header.Upd7759Clock != 0)
                {
                    Debug.LogWarning("Warning: clock for uPD7759 specified, but not supported");
                }

                // +90: OKIM6258 clock
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.Okim6258Clock = reader.ReadUInt32();
                if (header.Version >= 0x161 && header.Okim6258Clock != 0)
                {
                    Debug.LogWarning("Warning: clock for OKIM6258 specified, but not supported");
                }

                // +94: OKIM6258 Flags / K054539 Flags / C140 Chip Type / reserved
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.Okim6258Flags = reader.ReadByte();
                header.K054539Flags = reader.ReadByte();
                header.C140ChipType = reader.ReadByte();

                if (reader.ReadByte() != 0)
                {
                    Debug.LogWarning("Expected a zero at reserved offset 0x97");
                }

                // +98: OKIM6295 clock
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.Okim6295Clock = reader.ReadUInt32();
                if (header.Version >= 0x161 && header.Okim6295Clock != 0)
                {
                    Debug.LogWarning("Warning: clock for OKIM6295 specified, but not supported");
                }

                // +9C: K051649 clock
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.K051649Clock = reader.ReadUInt32();
                if (header.Version >= 0x161 && header.K051649Clock != 0)
                {
                    Debug.LogWarning("Warning: clock for K051649 specified, but not supported");
                }

                // +A0: K054539 clock
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.K054539Clock = reader.ReadUInt32();
                if (header.Version >= 0x161 && header.K054539Clock != 0)
                {
                    Debug.LogWarning("Warning: clock for K054539 specified, but not supported");
                }

                // +A4: HuC6280 clock
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.HuC6280Clock = reader.ReadUInt32();
                if (header.Version >= 0x161 && header.HuC6280Clock != 0)
                    Debug.LogWarning("Warning: clock for HuC6280 specified, but not supported");

                // +A8: C140 clock
                if (stream.Position + 4 > dataStart)
                {
                    return true;
                }

                header.C140Clock = reader.ReadUInt32();
                if (header.Version >= 0x161 && header.C140Clock != 0)
                    Debug.LogWarning("Warning: clock for C140 specified, but not supported");

                // +AC: K053260 clock
                if (stream.Position + 4 > dataStart)
                    return true;
                header.K053260Clock = reader.ReadUInt32();
                if (header.Version >= 0x161 && header.K053260Clock != 0)
                    Debug.LogWarning("Warning: clock for K053260 specified, but not supported");

                // +B0: Pokey clock
                if (stream.Position + 4 > dataStart)
                    return true;
                header.PokeyClock = reader.ReadUInt32();
                if (header.Version >= 0x161 && header.PokeyClock != 0)
                    Debug.LogWarning("Warning: clock for Pokey specified, but not supported");

                // +B4: QSound clock
                if (stream.Position + 4 > dataStart)
                    return true;
                header.QsoundClock = reader.ReadUInt32();
                if (header.Version >= 0x161 && header.QsoundClock != 0)
                    Debug.LogWarning("Warning: clock for QSound specified, but not supported");

                // +B8: SCSP clock
                if (stream.Position + 4 > dataStart)
                    return true;
                header.ScspClock = reader.ReadUInt32();
                if (header.Version >= 0x171 && header.ScspClock != 0)
                    Debug.LogWarning("Warning: clock for SCSP specified, but not supported");

                // +BC: extra header offset
                if (stream.Position + 4 > dataStart)
                    return true;
                header.ExtraHeaderOffset = reader.ReadUInt32();

                // +C0: WonderSwan clock
                if (stream.Position + 4 > dataStart)
                    return true;
                header.WonderSwanClock = reader.ReadUInt32();
                if (header.Version >= 0x171 && header.WonderSwanClock != 0)
                    Debug.LogWarning("Warning: clock for WonderSwan specified, but not supported");

                // +C4: VSU clock
                if (stream.Position + 4 > dataStart)
                    return true;
                header.VsuClock = reader.ReadUInt32();
                if (header.Version >= 0x171 && header.VsuClock != 0)
                    Debug.LogWarning("Warning: clock for VSU specified, but not supported");

                // +C8: SAA1099 clock
                if (stream.Position + 4 > dataStart)
                    return true;
                header.Saa1099Clock = reader.ReadUInt32();
                if (header.Version >= 0x171 && header.Saa1099Clock != 0)
                    Debug.LogWarning("Warning: clock for SAA1099 specified, but not supported");

                // +CC: ES5503 clock
                if (stream.Position + 4 > dataStart)
                    return true;
                header.Es5503Clock = reader.ReadUInt32();
                if (header.Version >= 0x171 && header.Es5503Clock != 0)
                    Debug.LogWarning("Warning: clock for ES5503 specified, but not supported");

                // +D0: ES5505/ES5506 clock
                if (stream.Position + 4 > dataStart)
                    return true;
                header.Es5505Es5506Clock = reader.ReadUInt32();
                if (header.Version >= 0x171 && header.Es5505Es5506Clock != 0)
                    Debug.LogWarning("Warning: clock for ES5505/ES5506 specified, but not supported");

                // +D4: ES5503 output channels / ES5505/ES5506 amount of output channels / C352 clock divider
                if (stream.Position + 4 > dataStart)
                    return true;

                header.Es5503Channels = reader.ReadByte();
                header.Es5505Es5506Channels = reader.ReadByte();
                header.C352ClockDivider = reader.ReadByte();

                // +D8: X1-010 clock
                if (stream.Position + 4 > dataStart)
                    return true;
                header.X1010Clock = reader.ReadUInt32();
                if (header.Version >= 0x171 && header.X1010Clock != 0)
                    Debug.LogWarning("Warning: clock for X1-010 specified, but not supported");

                // +DC: C352 clock
                if (stream.Position + 4 > dataStart)
                    return true;
                header.C352Clock = reader.ReadUInt32();
                if (header.Version >= 0x171 && header.C352Clock != 0)
                    Debug.LogWarning("Warning: clock for C352 specified, but not supported");

                // +E0: GA20 clock
                if (stream.Position + 4 > dataStart)
                    return true;
                header.Ga20Clock = reader.ReadUInt32();
                if (header.Version >= 0x171 && header.Ga20Clock != 0)
                    Debug.LogWarning("Warning: clock for GA20 specified, but not supported");

                return true;
            }
        }


        private void CreateChips()
        {
            // if (_header.Ym2413Clock != 0)
            // {
            //     add_chips<ymfm::ym2413>(_header.Ym2413Clock, ChipType.Ym2413, "YM2413");
            // }
            //
            // if (_header.Version >= 0x110 && _header.Ym2612Clock != 0)
            // {
            //     add_chips<ymfm::ym2612>(_header.Ym2612Clock, ChipType.Ym2612, "YM2612");
            // }

            if (_header.Version >= 0x110 && _header.Ym2151Clock != 0)
            {
                AddChips<Ym2151, StereoOutput>(_header.Ym2151Clock, ChipType.Ym2151, "YM2151");
            }

            // if (_header.Version >= 0x151 && _header.Ym2203Clock != 0)
            // {
            //     add_chips<ymfm::ym2203>(_header.Ym2203Clock, ChipType.Ym2203, "YM2203");
            // }
            //
            // if (_header.Version >= 0x151 && _header.Ym2608Clock != 0)
            // {
            //     add_chips<ymfm::ym2608>(_header.Ym2608Clock, ChipType.Ym2608, "YM2608");
            // }
            //
            // if (_header.Version >= 0x151 && _header.Ym2610Clock != 0)
            // {
            //     if (_header.IsYm2610B)
            //     {
            //         add_chips<ymfm::ym2610b>(_header.Ym2610Clock, ChipType.Ym2610, "YM2610B");
            //     }
            //     else
            //     {
            //         add_chips<ymfm::ym2610>(_header.Ym2610Clock, ChipType.Ym2610, "YM2610");
            //     }
            // }
            //
            // if (_header.Version >= 0x151 && _header.Ym3812Clock != 0)
            // {
            //     add_chips<ymfm::ym3812>(_header.Ym3812Clock, ChipType.Ym3812, "YM3812");
            // }
            //
            // if (_header.Version >= 0x151 && _header.Ym3526Clock != 0)
            // {
            //     add_chips<ymfm::ym3526>(_header.Ym3526Clock, ChipType.Ym3526, "YM3526");
            // }
            //
            // if (_header.Version >= 0x151 && _header.Y8950Clock != 0)
            // {
            //     add_chips<ymfm::y8950>(_header.Y8950Clock, ChipType.Y8950, "Y8950");
            // }
            //
            // if (_header.Version >= 0x151 && _header.Ymf262Clock != 0)
            // {
            //     add_chips<ymfm::ymf262>(_header.Ymf262Clock, ChipType.Ymf262, "YMF262");
            // }
            //
            // if (_header.Version >= 0x151 && _header.Ymf278bClock != 0)
            // {
            //     add_chips<ymfm::ymf278b>(_header.Ymf278bClock, ChipType.Ymf278B, "YMF278B");
            // }
            //
            // if (_header.Version >= 0x151 && _header.Ay8910Clock != 0)
            // {
            //     Debug.LogWarning("Warning: clock for AY8910 specified, substituting YM2149\n");
            //     add_chips<ymfm::ym2149>(_header.Ay8910Clock, ChipType.Ym2149, "YM2149");
            // }
        }

        private void AddChips<TChip, TOutput>(uint clock, ChipType type, string chipName)
            where TChip : IChip<TOutput>
            where TOutput : unmanaged, IOutput
        {
            var clockValue = clock & 0x3fffffff;
            var numberOfChips = (clock & 0x40000000) != 0 ? 2 : 1;
            for (var index = 0; index < numberOfChips; index++)
            {
                _chips.Add(
                    new VgmChip<TChip, TOutput>(
                        clockValue,
                        type,
                        (numberOfChips == 1) ? chipName : $"{chipName} #{index}"
                    )
                );
            }

            // YM2608 not supported (it needs a ROM that I can't redistribute)
        }

        //-------------------------------------------------
        //  generate_all - generate everything described
        //  in the vgmplay file
        //-------------------------------------------------
        private unsafe void GenerateAll(
            ReadOnlySpan<byte> buffer,
            uint dataStart,
            uint outputRate,
            List<int> wavBuffer
        )
        {
            // set the offset to the data start and go 
            var done = false;
            var outputStep = (long)(0x100000000ul / outputRate);
            fixed (byte* ptr = buffer)
            {
                using var stream = new UnmanagedMemoryStream(ptr, buffer.Length);
                using var reader = new BinaryReader(stream);
                stream.Position = dataStart;

                while (!done && stream.Position < buffer.Length)
                {
                    var delay = 0;
                    var cmd = reader.ReadByte();
                    switch (cmd)
                    {
                        // YM2413, write value dd to register aa
                        case 0x51 or 0xa1:
                        {
                            var register = reader.ReadByte();
                            var data = reader.ReadByte();
                            WriteChip(ChipType.Ym2413, (byte)(cmd >> 7), register, data);
                            // (cmd >> 7) gets the index (the higher operand opcode is used to write to the second chip)
                            break;
                        }

                        // YM2612 port 0, write value dd to register aa
                        case 0x52 or 0xa2:
                        {
                            var register = reader.ReadByte();
                            var data = reader.ReadByte();
                            WriteChip(ChipType.Ym2612, (byte)(cmd >> 7), register, data);
                            break;
                        }

                        // YM2612 port 1, write value dd to register aa
                        case 0x53 or 0xa3:
                        {
                            var register = reader.ReadByte();
                            var data = reader.ReadByte();
                            WriteChip(ChipType.Ym2612, (byte)(cmd >> 7), register | 0x100u, data);
                            break;
                        }

                        // YM2151, write value dd to register aa
                        case 0x54 or 0xa4:
                        {
                            var register = reader.ReadByte();
                            var data = reader.ReadByte();
                            WriteChip(ChipType.Ym2151, (byte)(cmd >> 7), register, data);
                            break;
                        }

                        // YM2203, write value dd to register aa
                        case 0x55 or 0xa5:
                        {
                            var register = reader.ReadByte();
                            var data = reader.ReadByte();
                            WriteChip(ChipType.Ym2203, (byte)(cmd >> 7), register, data);
                            break;
                        }

                        // YM2608 port 0, write value dd to register aa
                        case 0x56 or 0xa6:
                        {
                            var register = reader.ReadByte();
                            var data = reader.ReadByte();
                            WriteChip(ChipType.Ym2608, (byte)(cmd >> 7), register, data);
                            break;
                        }

                        // YM2608 port 1, write value dd to register aa
                        case 0x57 or 0xa7:
                        {
                            var register = reader.ReadByte();
                            var data = reader.ReadByte();
                            WriteChip(ChipType.Ym2608, (byte)(cmd >> 7), register | 0x100u, data);
                            break;
                        }

                        // YM2610 port 0, write value dd to register aa
                        case 0x58 or 0xa8:
                        {
                            var register = reader.ReadByte();
                            var data = reader.ReadByte();
                            WriteChip(ChipType.Ym2610, (byte)(cmd >> 7), register, data);
                            break;
                        }

                        // YM2610 port 1, write value dd to register aa
                        case 0x59 or 0xa9:
                        {
                            var register = reader.ReadByte();
                            var data = reader.ReadByte();
                            WriteChip(ChipType.Ym2610, (byte)(cmd >> 7), (uint)(register | 0x100u), data);
                            break;
                        }

                        // YM3812, write value dd to register aa
                        case 0x5a or 0xaa:
                        {
                            var register = reader.ReadByte();
                            var data = reader.ReadByte();
                            WriteChip(ChipType.Ym3812, (byte)(cmd >> 7), register, data);
                            break;
                        }

                        // YM3526, write value dd to register aa
                        case 0x5b or 0xab:
                        {
                            var register = reader.ReadByte();
                            var data = reader.ReadByte();
                            WriteChip(ChipType.Ym3526, (byte)(cmd >> 7), register, data);
                            break;
                        }

                        // Y8950, write value dd to register aa
                        case 0x5c or 0xac:
                        {
                            var register = reader.ReadByte();
                            var data = reader.ReadByte();
                            WriteChip(ChipType.Y8950, (byte)(cmd >> 7), register, data);
                            break;
                        }

                        // YMF262 port 0, write value dd to register aa
                        case 0x5e or 0xae:
                        {
                            var register = reader.ReadByte();
                            var data = reader.ReadByte();
                            WriteChip(ChipType.Ymf262, (byte)(cmd >> 7), register, data);
                            break;
                        }

                        // YMF262 port 1, write value dd to register aa
                        case 0x5f or 0xaf:
                        {
                            var register = reader.ReadByte();
                            var data = reader.ReadByte();
                            WriteChip(ChipType.Ymf262, (byte)(cmd >> 7), register | 0x100u, data);
                            break;
                        }

                        // Wait n samples, n can range from 0 to 65535 (approx 1.49 seconds)
                        case 0x61:
                        {
                            delay = reader.ReadUInt16();
                            break;
                        }

                        // wait 735 samples (60th of a second), a shortcut for 0x61 0xdf 0x02
                        case 0x62:
                        {
                            delay = 735;
                            break;
                        }

                        // wait 882 samples (50th of a second), a shortcut for 0x61 0x72 0x03
                        case 0x63:
                        {
                            delay = 882;
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
                            ReadDataBlock(buffer, reader);
                            break;
                        }

                        // PCM RAM write
                        case 0x68:
                        {
                            ParsePcmRam(buffer, reader);
                            break;
                        }

                        // AY8910, write value dd to register aa
                        case 0xa0:
                        {
                            var register = reader.ReadByte();
                            var data = reader.ReadByte();
                            WriteChip(ChipType.Ym2149, (byte)(register >> 7), (byte)(register & 0x7f), data);
                            break;
                        }

                        // pp aa dd: YMF278B, port pp, write value dd to register aa
                        case 0xd0:
                        {
                            var port = reader.ReadByte();
                            var register = reader.ReadByte();
                            var data = reader.ReadByte();
                            WriteChip(ChipType.Ymf278B, (byte)(port >> 7), ((port & 0x7fu) << 8) | register, data);
                            break;
                        }

                        // wait n+1 samples, n can range from 0 to 15.
                        case >= 0x70 and <= 0x7f:
                        {
                            delay = (cmd & 15) + 1;
                            break;
                        }

                        // YM2612 port 0 address 2A write from the data bank, then wait n samples; n can range from 0 to 15.
                        // Note that the wait is n, NOT n+1. See also command 0xE0.
                        case >= 0x80 and <= 0x8f:
                        {
                            var chip = FindChip(ChipType.Ym2612, 0);
                            chip?.Write(0x2a, chip.ReadPcm());
                            delay = cmd & 15;
                            break;
                        }

                        // reserved or unsupported commands, consume one byte
                        case (>= 0x30 and <= 0x3f) or 0x4f or 0x50:
                        {
                            reader.ReadByte();
                            break;
                        }

                        // reserved or unsupported commands, consume two bytes
                        case (>= 0x40 and <= 0x4e) or 0x5d or (>= 0xb0 and <= 0xbf):
                        {
                            reader.ReadUInt16();
                            break;
                        }

                        // reserved or unsupported commands, consume three bytes
                        case (>= 0xc0 and <= 0xcf) or (>= 0xd1 and <= 0xdf):
                        {
                            reader.ReadByte();
                            reader.ReadByte();
                            reader.ReadByte();
                            break;
                        }

                        // dddddddd: Seek to offset dddddddd (Intel byte order) in PCM data bank of data block type 0 (YM2612).
                        case 0xe0:
                        {
                            var chip = FindChip(ChipType.Ym2612, 0);
                            var pos = reader.ReadUInt32();
                            chip?.SeekPcm(pos);
                            break;
                        }

                        // reserved or unsupported commands, consume four bytes
                        case (>= 0xe1 and <= 0xff):
                        {
                            reader.ReadUInt32();
                            break;
                        }
                    }

                    HandleDelays(wavBuffer, delay, outputStep);
                }
            }
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

        private void HandleDelays(List<int> wavBuffer, int delay, long outputStep)
        {
            Span<int> outputs = stackalloc int[2];
            var outputPos = 0L;

            // handle delays
            while (delay-- != 0)
            {
                foreach (var chip in _chips)
                {
                    chip.Generate(outputPos, outputStep, outputs);
                }

                outputPos += outputStep;
                wavBuffer.Add(outputs[0]);
                wavBuffer.Add(outputs[1]);
            }
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


        private void OnReadData(float[] data)
        {
        }

        private void OnPositionSet(int position)
        {
        }
    }
}
