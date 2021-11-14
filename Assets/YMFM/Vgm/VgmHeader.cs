using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace Ymfm.Vgm
{
    /// <seealso href="https://vgmrips.net/wiki/VGM_Specification"/>
    [Serializable]
    public struct VgmHeader
    {
        public const uint MagicNumber = 0x20_6d_67_56;
        public const uint Version101 = 0x101;
        public const uint Version110 = 0x110;
        public const uint Version150 = 0x150;
        public const uint Version151 = 0x151;
        public const uint Version160 = 0x160;
        public const uint Version161 = 0x161;
        public const uint Version170 = 0x170;
        public const uint Version171 = 0x171;

        [SerializeField]
        [CustomValueDrawer("HexLabelDrawer")]
        private uint _magic;

        public uint Magic => _magic;

        /// <summary>
        /// Relative offset to end of file (i.e. file length - 4). This is mainly used to find the next track when
        /// concatenating player stubs and multiple files. 
        /// </summary>
        //[FieldOffset(0x04)]
        [SerializeField]
        [CustomValueDrawer("LengthDrawer")]
        private uint _endOfFileOffset;

        public uint EndOfFileOffset => _endOfFileOffset;

        /// <summary>
        /// Version number in BCD-Code. e.g. Version 1.70 is stored as <c>0x00000171</c>. This is used for backwards
        /// compatibility in players, and defines which header values are valid. 
        /// </summary>
        //[FieldOffset(0x08)]
        [CustomValueDrawer("HexLabelDrawer")]
        [SerializeField]
        private uint _version;

        public uint Version => _version;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the SN76489 PSG chip. A typical value is 3579545.
        /// </para>
        /// <para>
        /// It should be 0 if there is no PSG chip used.
        /// </para>
        /// <para>
        /// Note: Bit 31 (0x80000000) is used on combination with the dual-chip-bit to indicate that this is a T6W28.
        /// (PSG variant used in Neo Geo Pocket)
        /// </para>
        /// </summary>
        //[FieldOffset(0x0C)]
        public uint Sn76489Clock;

        /// <summary>
        /// Input clock rate in Hz for the YM2413 chip. A typical value is <c>3579545</c>.
        /// It should be 0 if there is no YM2413 chip used. 
        /// </summary>
        //[FieldOffset(0x10)]
        public uint Ym2413Clock;

        /// <summary>
        /// Relative offset to GD3 tag. 0 if no GD3 tag. GD3 tags are descriptive tags similar in use to ID3 tags in MP3
        /// files. See the GD3 specification for more details. The GD3 tag is usually stored immediately after the VGM data. 
        /// </summary>
        //[FieldOffset(0x14)]
        [SerializeField]
        [CustomValueDrawer("LengthDrawer")]
        private uint _gd3Offset;

        public uint Gd3Offset => _gd3Offset;

        /// <summary>
        /// Total of all wait values in the file. 
        /// </summary>
        //[FieldOffset(0x18)]
        public uint TotalNumSamples;

        /// <summary>
        /// Relative offset to loop point, or 0 if no loop. For example, if the data for the one-off intro to a song was
        /// in bytes <c>0x0040 - 0x3FFF</c> of the file, but the main looping section started at <c>0x4000</c>, this
        /// would contain the value <c>0x4000 - 0x1C = 0x00003FE4</c>. 
        /// </summary>
        //[FieldOffset(0x1C)]
        [SerializeField]
        [CustomValueDrawer("LengthDrawer")]
        private uint _loopOffset;

        public uint LoopOffset => _loopOffset;


        /// <summary>
        /// Number of samples in one loop, or 0 if there is no loop. Total of all wait values between the loop point and
        /// the end of the file. 
        /// </summary>
        //[FieldOffset(0x20)]
        public uint LoopNumSamples;

        #region VGM 1.01 Additions

        /// <summary>
        /// "Rate" of recording in Hz, used for rate scaling on playback. It is typically 50 for PAL systems and 60 for
        /// NTSC systems. It should be set to zero if rate scaling is not appropriate - for example, if the game adjusts
        /// its music engine for the system's speed. VGM 1.00 files will have a value of 0. 
        /// </summary>
        //[FieldOffset(0x24)]
        public uint Rate;

        #endregion

        #region VGM 1.10 Additions

        /// <summary>
        /// The white noise feedback pattern for the SN76489 PSG. Known values are:
        /// <list type="table">
        /// <listheader>
        ///     <term>Value</term>
        ///     <description>Device</description>
        /// </listheader>
        /// <item>
        ///     <term>0x0009</term>
        ///     <description>
        ///         Sega Master System 2/Game Gear/Mega Drive (SN76489/SN76496 integrated into Sega VDP chip)
        ///     </description>
        /// </item>
        /// <item>
        ///     <term>0x0003</term>
        ///     <description>Sega Computer 3000H, BBC Micro (SN76489AN)</description>
        /// </item>
        /// <item>
        ///     <term>0x0006</term>
        ///     <description>SN76494, SN76496</description>
        /// </item>
        /// </list> 
        /// For version 1.01 and earlier files, the feedback pattern should be assumed to be 0x0009. If the PSG is not used then this may be omitted (left at zero). 
        /// </summary>
        //[FieldOffset(0x28)]
        public ushort Sn76489Feedback;

        /// <summary>
        /// The noise feedback shift register width, in bits. Known values are:
        /// <list type="table">
        /// <listheader>
        ///     <term>Width (bits)</term>
        ///     <description>Device</description>
        /// </listheader>
        /// <item>
        ///     <term>16</term>
        ///     <description>
        ///         Sega Master System 2/Game Gear/Mega Drive (SN76489/SN76496 integrated into Sega VDP chip)
        ///     </description>
        /// </item>
        /// <item>
        ///     <term>15</term>
        ///     <description>
        ///         Sega Computer 3000H, BBC Micro (SN76489AN)
        ///     </description>
        /// </item>
        /// </list>
        /// For version 1.01 and earlier files, the shift register width should be assumed to be 16. If the PSG is not used then this may be omitted (left at zero). 
        /// </summary>
        //[FieldOffset(0x2A)]
        public byte Sn76489ShiftRegisterWidth;

        #endregion

        #region VGM 1.51 Additions

        /// <summary>
        /// Misc flags for the SN76489. Most of them don't make audible changes and can be ignored, if the SN76489 emulator lacks the features.
        /// <list type="table">
        /// <listheader>
        ///     <term>Bits</term>
        ///     <description>Description</description>
        /// </listheader>
        /// <item>
        ///     <term>bit 0</term>
        ///     <description>frequency 0 is 0x400</description>
        /// </item>
        /// <item>
        ///     <term>bit 1</term>
        ///     <description>output negate flag</description>
        /// </item>
        /// <item>
        ///     <term>bit 2</term>
        ///     <description>stereo on/off (on when bit clear)</description>
        /// </item>
        /// <item>
        ///     <term>bit 3</term>
        ///     <description>/8 Clock Divider on/off (on when bit clear)</description>
        /// </item>
        /// <item>
        ///     <term>bits 4-7</term>
        ///     <description>reserved (must be zero)</description>
        /// </item>
        /// </list>
        /// For version 1.51 and earlier files, all the flags should not be set. If the PSG is not used then this may be omitted (left at zero). 
        /// </summary>
        //[FieldOffset(0x2B)]
        public byte Sn76489Flags;

        private const byte OutputNegateFlag = 0b00000010;
        private const byte StereoOnOffFlag = 0b00000100;

        public bool Sn76489OutputNegate
        {
            [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => (Sn76489Flags & OutputNegateFlag) != 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Sn76489Flags = (byte)(value ? Sn76489Flags | OutputNegateFlag : Sn76489Flags & ~OutputNegateFlag);
        }

        public bool Sn76489NotStereo
        {
            [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => (Sn76489Flags & StereoOnOffFlag) != 0;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Sn76489Flags = (byte)(value ? Sn76489Flags | StereoOnOffFlag : Sn76489Flags & ~StereoOnOffFlag);
        }

        #endregion

        #region VGM 1.10 Additions

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the YM2612 chip. A typical value is 7670454.
        /// </para>
        /// <para>
        /// It should be 0 if there us no YM2612 chip used.
        /// </para>
        /// <para>
        /// For version 1.01 and earlier files, the YM2413 clock rate should be used for the clock rate of the YM2612.
        /// </para>
        /// </summary>
        //[FieldOffset(0x2C)]
        public uint Ym2612Clock;


        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the YM2151 chip. A typical value is 3579545.
        /// </para>
        /// <para>
        /// It should be 0 if there us no YM2151 chip used.
        /// </para>
        /// <para>
        /// For version 1.01 and earlier files, the YM2413 clock rate should be used for the clock rate of the YM2151.
        /// </para>
        /// </summary>
        //[FieldOffset(0x30)]
        public uint Ym2151Clock;

        #endregion

        #region VGM 1.50 Additions

        /// <summary>
        /// <para>
        /// Relative offset to VGM data stream.
        /// </para>
        /// <para>
        /// If the VGM data starts at absolute offset 0x40, this will contain value 0x0000000C. For versions prior to 1.50,
        /// it should be 0 and the VGM data must start at offset 0x40.
        /// </para>
        /// </summary>
        //[FieldOffset(0x34)]
        [SerializeField]
        [CustomValueDrawer("LengthDrawer")]
        private uint _vgmDataOffset;

        public uint VgmDataOffset => _vgmDataOffset;

        #endregion

        #region VGM 1.51 Additions

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the Sega PCM chip. A typical value is 4000000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no Sega PCM chip used. 
        /// </para>
        /// </summary>
        //[FieldOffset(0x38)]
        public uint SegaPcmClock;

        /// <summary>
        /// <para>
        /// The interface register for the Sega PCM chip.
        /// </para>
        /// <para>
        /// It should be 0 if there is no Sega PCM chip used. 
        /// </para>
        /// </summary>
        //[FieldOffset(0x3C)]
        public uint SegaPcmInterfaceRegister;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the RF5C68 PCM chip. A typical value is 12500000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no RF5C68 chip used. 
        /// </para>
        /// </summary>
        //[FieldOffset(0x40)]
        public uint Rf5c68Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the YM2203 chip. A typical value is 3000000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no YM2203 chip used. 
        /// </para>
        /// </summary>
        //[FieldOffset(0x44)]
        public uint Ym2203Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the YM2608 chip. A typical value is 8000000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no YM2608 chip used. 
        /// </para>
        /// </summary>
        //[FieldOffset(0x48)]
        public uint Ym2608Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the YM2610/B chip. A typical value is 8000000. It should be 0 if there is no
        /// YM2610/B chip used.
        /// </para>
        /// <para>
        /// Note: Bit 31 is used to set whether it is an YM2610 or an YM2610B chip. If bit 31 is set it is an YM2610B,
        /// if bit 31 is clear it is an YM2610.
        /// </para>
        /// </summary>
        //[FieldOffset(0x4C)]
        public uint Ym2610Clock;

        private const uint IsYm2610BFlag = (1u << 31);

        public bool IsYm2610B
        {
            [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => (Ym2610Clock & IsYm2610BFlag) != 0;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => Ym2610Clock = (value ? Ym2610Clock | IsYm2610BFlag : Ym2610Clock & ~IsYm2610BFlag);
        }

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the YM3812 chip. A typical value is 3579545.
        /// </para>
        /// <para>
        /// It should be 0 if there is no YM3812 chip used. 
        /// </para>
        /// </summary>
        //[FieldOffset(0x50)]
        public uint Ym3812Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the YM3526 chip. A typical value is 3579545.
        /// </para>
        /// <para>
        /// It should be 0 if there is no YM3526 chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0x54)]
        public uint Ym3526Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the Y8950 chip. A typical value is 3579545.
        /// </para>
        /// <para>
        /// It should be 0 if there is no Y8950 chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0x58)]
        public uint Y8950Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the YMF262 chip. A typical value is 14318180.
        /// </para>
        /// <para>
        /// It should be 0 if there is no YMF262 chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0x5C)]
        public uint Ymf262Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the YMF278B chip. A typical value is 33868800.
        /// </para>
        /// <para>
        /// It should be 0 if there is no YMF278B chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0x60)]
        public uint Ymf278bClock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the YMF271 chip. A typical value is 16934400.
        /// </para>
        /// <para>
        /// It should be 0 if there is no YMF271 chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0x64)]
        public uint Ymf271Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the YMZ280B chip. A typical value is 16934400.
        /// </para>
        /// <para>
        /// It should be 0 if there is no YMZ280B chip used. 
        /// </para>
        /// </summary>
        //[FieldOffset(0x68)]
        public uint Ymz280bClock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the RF5C164 PCM chip. A typical value is 12500000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no RF5C164 chip used. 
        /// </para>
        /// </summary>
        //[FieldOffset(0x6C)]
        public uint Rf5c164Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the PWM chip. A typical value is 23011361.
        /// </para>
        /// <para>
        /// It should be 0 if there is no PWM chip used. 
        /// </para>
        /// </summary>
        //[FieldOffset(0x70)]
        public uint PwmClock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the AY8910 chip. A typical value is 1789750.
        /// </para>
        /// <para>
        /// It should be 0 if there is no AY8910 chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0x74)]
        public uint Ay8910Clock;

        /// <summary>
        /// Defines the exact type of AY8910. The values are:
        /// <list type="table">
        /// <listheader>
        ///     <term>Value</term>
        ///     <description>Chip Type</description>
        /// </listheader>
        /// <item>
        ///     <term>0x00</term>
        ///     <description>AY8910</description>
        /// </item>
        /// <item>
        ///     <term>0x01</term>
        ///     <description>AY8912</description>
        /// </item>
        /// <item>
        ///     <term>0x02</term>
        ///     <description>AY8913</description>
        /// </item>
        /// <item>
        ///     <term>0x03</term>
        ///     <description>AY8930</description>
        /// </item>
        /// <item>
        ///     <term>0x10</term>
        ///     <description>YM2149</description>
        /// </item>
        /// <item>
        ///     <term>0x11</term>
        ///     <description>YM3439</description>
        /// </item>
        /// <item>
        ///     <term>0x12</term>
        ///     <description>YMZ284</description>
        /// </item>
        /// <item>
        ///     <term>0x13</term>
        ///     <description>YMZ294</description>
        /// </item>
        /// </list>
        /// If the AY8910 is not used then this may be omitted (left at zero). 
        /// </summary>
        //[FieldOffset(0x78)]
        public byte Ay8910ChipType;

        /// <summary>
        /// Misc flags for the AY8910. Default is 0x01. For additional description see ay8910.h in MAME source code.
        /// <list type="table">
        /// <listheader>
        ///     <term>Bits</term>
        ///     <description>Description</description>
        /// </listheader>
        /// <item>
        ///     <term>bit 0</term>
        ///     <description>Legacy Output</description>
        /// </item>
        /// <item>
        ///     <term>bit 1</term>
        ///     <description>Single Output</description>
        /// </item>
        /// <item>
        ///     <term>bit 2</term>
        ///     <description>Discrete Output</description>
        /// </item>
        /// <item>
        ///     <term>bit 3</term>
        ///     <description>RAW Output</description>
        /// </item>
        /// <item>
        ///     <term>bit 4-7</term>
        ///     <description>reserved (must be zero)</description>
        /// </item>
        /// </list>
        /// If the AY8910 is not used then this may be omitted (left at zero). 
        /// </summary>
        //[FieldOffset(0x79)]
        public byte Ay8910Flags;

        /// <summary>
        /// Misc flags for the AY8910. This one is specific for the AY8910 that's connected with/part of the YM2203.
        /// </summary>
        //[FieldOffset(0x7A)]
        public byte Ym2203Ay8910Flags;

        /// <summary>
        /// Misc flags for the AY8910. This one is specific for the AY8910 that's connected with/part of the YM2608.
        /// </summary>
        //[FieldOffset(0x7B)]
        public byte Ym2608Ay8910Flags;

        #endregion

        #region VGM 1.60 Additions

        /// <summary>
        /// <para>
        /// Volume = 2 ^ (VolumeModifier / 0x20) where VolumeModifier is a number from -63 to 192 (-63 = 0xC1, 0 = 0x00,
        /// 192 = 0xC0). Also the value -63 gets replaced with -64 in order to make factor of 0.25 possible. Therefore
        /// the volume can reach levels between 0.25 and 64. Default is 0, which is equal to a factor of 1 or 100%.
        /// </para>
        /// <para>
        /// Note: Players should support the Volume Modifier in v1.50 files and higher. This way Mega Drive VGMs can use
        /// the Volume Modifier without breaking compatibility with old players.
        /// </para>
        /// </summary>
        //[FieldOffset(0x7C)]
        public byte VolumeModifier;

        /// <summary>
        /// Modifies the number of loops that are played before the playback ends. Set this value to eg. 1 to reduce the
        /// number of played loops by one. This is useful, if the song is looped twice in the vgm, because there are
        /// minor differences between the first and second loop and the song repeats just the second loop. The resulting
        /// number of loops that are played is calculated as following: NumLoops = NumLoopsModified - LoopBase Default
        /// is 0. Negative numbers are possible (80h...FFh = -128...-1)
        /// </summary>
        //[FieldOffset(0x7E)]
        public byte LoopBase;

        #endregion

        #region VGM 1.51 Additions

        /// <summary>
        /// Modifies the number of loops that are played before the playback ends. You may want to use this, e.g. if a
        /// tune has a very short, but non- repetitive loop (then set it to 0x20 double the loop number). The resulting
        /// number of loops that are played is calculated as following:
        ///
        /// <code>
        /// NumLoops = ProgramNumLoops * LoopModifier / 0x10
        /// </code>
        /// Default is 0, which is equal to 0x10. 
        /// </summary>
        //[FieldOffset(0x7F)]
        public byte LoopModifier;

        #endregion

        #region VGM 1.61 Additions

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the GameBoy DMG chip, LR35902. A typical value is 4194304.
        /// </para>
        /// <para>
        /// It should be 0 if there is no GB DMG chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0x80)]
        public uint GameBoyDmgClock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the NES APU chip, N2A03. A typical value is 1789772.
        /// </para>
        /// <para>
        /// It should be 0 if there is no NES APU chip used.
        /// </para>
        /// <para>
        /// Note: Bit 31 (0x80000000) is used to enable the FDS sound addon. Set to enable, clear to disable.
        /// </para>
        /// </summary>
        //[FieldOffset(0x84)]
        public uint NesApuClock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the MultiPCM chip. A typical value is 8053975.
        /// </para>
        /// <para>
        /// It should be 0 if there is no MultiPCM chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0x88)]
        public uint MultiPcmClock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the uPD7759 chip. A typical value is 640000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no uPD7759 chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0x8C)]
        public uint Upd7759Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the OKIM6258 chip. A typical value is 4000000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no OKIM6258 chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0x90)]
        public uint Okim6258Clock;

        /// <summary>
        /// Misc flags for the OKIM6258. Default is 0x00.
        /// <list type="table">
        /// <listheader>
        ///     <term>Bits</term>
        ///     <description>Description</description>
        /// </listheader>
        /// <item>
        ///     <term>bit 0-1</term>
        ///     <description>Clock Divider (clock dividers are 1024, 768, 512, 512)</description>
        /// </item>
        /// <item>
        ///     <term>bit 2</term>
        ///     <description>3/4-bit ADPCM select (default is 4-bit, doesn't work currently)</description>
        /// </item>
        /// <item>
        ///     <term>bit 3</term>
        ///     <description>10/12-bit Output (default is 10-bit)</description>
        /// </item>
        /// <item>
        ///     <term>bit 4-7</term>
        ///     <description>reserved (must be zero)</description>
        /// </item>
        /// </list>
        /// If the OKIM6258 is not used then this may be omitted (left at zero).
        /// </summary>
        //[FieldOffset(0x94)]
        public byte Okim6258Flags;

        /// <summary>
        /// Misc flags for the K054539. Default is 0x01. See also k054539.h in MAME source code.
        /// <list type="table">
        /// <listheader>
        ///     <term>Bits</term>
        ///     <description>Description</description>
        /// </listheader>
        /// <item>
        ///     <term>bit 0</term>
        ///     <description>Reverse Stereo</description>
        /// </item>
        /// <item>
        ///     <term>bit 1</term>
        ///     <description>Disable Reverb</description>
        /// </item>
        /// <item>
        ///     <term>bit 2</term>
        ///     <description>Update at KeyOn</description>
        /// </item>
        /// <item>
        ///     <term>bit 3-7</term>
        ///     <description>reserved (must be zero)</description>
        /// </item>
        /// </list>
        /// If the K054539 is not used then this may be omitted (left at zero).
        /// </summary>
        //[FieldOffset(0x95)]
        public byte K054539Flags;

        /// <summary>
        /// Defines the exact type of C140 and its banking method. The values are:
        /// <list type="table">
        /// <listheader>
        ///     <term>Value</term>
        ///     <description>Chip Type</description>
        /// </listheader>
        /// <item>
        ///     <term>0x00</term>
        ///     <description>C140, Namco System 2</description>
        /// </item>
        /// <item>
        ///     <term>0x01</term>
        ///     <description>C140, Namco System 21</description>
        /// </item>
        /// <item>
        ///     <term>0x02</term>
        ///     <description>219 ASIC, Namco NA-1/2 </description>
        /// </item>
        /// </list>
        /// If the C140 is not used then this may be omitted (left at zero).
        /// </summary>
        //[FieldOffset(0x96)]
        public byte C140ChipType;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the OKIM6295 chip. A typical value is 8000000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no OKIM6295 chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0x98)]
        public uint Okim6295Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the K051649 chip. A typical value is 1500000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no K051649 chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0x9C)]
        public uint K051649Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the K054539 chip. A typical value is 18432000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no K054539 chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0xA0)]
        public uint K054539Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the HuC6280 chip. A typical value is 3579545.
        /// </para>
        /// <para>
        /// It should be 0 if there is no HuC6280 chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0xA4)]
        public uint HuC6280Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the C140 chip. A typical value is 21390.
        /// </para>
        /// <para>
        /// It should be 0 if there is no C140 chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0xA8)]
        public uint C140Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the K053260 chip. A typical value is 3579545.
        /// </para>
        /// <para>
        /// It should be 0 if there is no K053260 chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0xAC)]
        public uint K053260Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the Pokey chip. A typical value is 1789772.
        /// </para>
        /// <para>
        /// It should be 0 if there is no Pokey chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0xB0)]
        public uint PokeyClock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the QSound chip. A typical value is 4000000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no QSound chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0xB4)]
        public uint QsoundClock;

        #endregion

        #region VGM 1.71 Additions

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the SCSP chip. A typical value is 22579200.
        /// </para>
        /// <para>
        /// It should be 0 if there is no SCSP chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0xB8)]
        public uint ScspClock;

        #endregion

        #region VGM 1.70 Additions

        /// <summary>
        /// Relative offset to the extra header or 0 if no extra header is present.
        /// </summary>
        //[FieldOffset(0xBC)]
        public uint ExtraHeaderOffset;

        #endregion

        #region VGM 1.71 Additions

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the WonderSwan chip. A typical value is 3072000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no WonderSwan chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0xC0)]
        public uint WonderSwanClock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the VSU chip. A typical value is 5000000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no VSU chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0xC4)]
        public uint VsuClock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the SAA1099 chip. A typical value is 8000000 (or 7159000/7159090).
        /// </para>
        /// <para>
        /// It should be 0 if there is no SAA1099 chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0xC8)]
        public uint Saa1099Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the ES5503 chip. A typical value is 7159090.
        /// </para>
        /// <para>
        /// It should be 0 if there is no ES5503 chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0xCC)]
        public uint Es5503Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the ES5505/ES5506 chip.
        /// </para>
        /// <para>
        /// It should be 0 if there is no ES5505/ES5506 chip used.
        /// </para>
        /// <para>
        /// Note: Bit 31 is used to set whether it is an ES5505 or an ES5506 chip. If bit 31 is set it is an ES5506, if
        /// bit 31 is clear it is an ES5505.
        /// </para>
        /// </summary>
        //[FieldOffset(0xD0)]
        public uint Es5505Es5506Clock;

        /// <summary>
        /// <para>
        /// Defines the internal number of output channels for the ES5503.
        /// </para>
        /// <para>
        /// Possible values are 1 to 8. A typical value is 2.
        /// </para>
        /// <para>
        /// If the ES5503 is not used then this may be omitted (left at zero).
        /// </para>
        /// </summary>
        //[FieldOffset(0xD4)]
        public byte Es5503Channels;

        /// <summary>
        /// <para>
        /// Defines the internal number of output channels for the ES5506.
        /// </para>
        /// <para>
        /// Possible values are 1 to 4 for the ES5505 and 1 to 8 for the ES5506. A typical value is 1.
        /// </para>
        /// <para>
        /// If the ES5506 is not used then this may be omitted (left at zero).
        /// </para>
        /// </summary>
        //[FieldOffset(0xD5)]
        public byte Es5505Es5506Channels;

        /// <summary>
        /// <para>
        /// Defines the clock divider for the C352 chip, divided by 4 in order to achieve a divider range of 0 to 1020.
        /// A typical value is 288.
        /// </para>
        /// <para>
        /// If the C352 is not used then this may be omitted (left at zero).
        /// </para>
        /// </summary>
        //[FieldOffset(0xD6)]
        public byte C352ClockDivider;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the X1-010 chip. A typical value is 16000000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no X1-010 chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0xD8)]
        public uint X1010Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the C352 chip. A typical value is 24192000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no C352 chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0xDC)]
        public uint C352Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the GA20 chip. A typical value is 3579545.
        /// </para>
        /// <para>
        /// It should be 0 if there is no GA20 chip used.
        /// </para>
        /// </summary>
        //[FieldOffset(0xE0)]
        public uint Ga20Clock;

        #endregion

        public VgmHeader(ReadOnlySpan<byte> input)
        {
            switch (input.Length)
            {
                case 0:
                    throw new ArgumentException("Input is empty");
                case < 4:
                    throw new ArgumentException("No magic number");
            }

            // +00: check the magic ID 
            _magic = 0;
            _endOfFileOffset = 0;
            _version = 0;
            Sn76489Clock = 0;
            Ym2413Clock = 0;
            _gd3Offset = 0;
            TotalNumSamples = 0;
            _loopOffset = 0;
            LoopNumSamples = 0;
            Rate = 0;
            Sn76489Feedback = 0;
            Sn76489ShiftRegisterWidth = 0;
            Sn76489Flags = 0;
            Ym2612Clock = 0;
            Ym2151Clock = 0;
            _vgmDataOffset = 0;
            SegaPcmClock = 0;
            SegaPcmInterfaceRegister = 0;
            Rf5c68Clock = 0;
            Ym2203Clock = 0;
            Ym2608Clock = 0;
            Ym2610Clock = 0;
            Ym3812Clock = 0;
            Ym3526Clock = 0;
            Y8950Clock = 0;
            Ymf262Clock = 0;
            Ymf278bClock = 0;
            Ymf271Clock = 0;
            Ymz280bClock = 0;
            Rf5c164Clock = 0;
            PwmClock = 0;
            Ay8910Clock = 0;
            Ay8910ChipType = 0;
            Ay8910Flags = 0;
            Ym2203Ay8910Flags = 0;
            Ym2608Ay8910Flags = 0;
            VolumeModifier = 0;
            LoopBase = 0;
            LoopModifier = 0;
            GameBoyDmgClock = 0;
            NesApuClock = 0;
            MultiPcmClock = 0;
            Upd7759Clock = 0;
            Okim6258Clock = 0;
            Okim6258Flags = 0;
            K054539Flags = 0;
            C140ChipType = 0;
            Okim6295Clock = 0;
            K051649Clock = 0;
            K054539Clock = 0;
            HuC6280Clock = 0;
            C140Clock = 0;
            K053260Clock = 0;
            PokeyClock = 0;
            QsoundClock = 0;
            ScspClock = 0;
            ExtraHeaderOffset = 0;
            WonderSwanClock = 0;
            VsuClock = 0;
            Saa1099Clock = 0;
            Es5503Clock = 0;
            Es5505Es5506Clock = 0;
            Es5503Channels = 0;
            Es5505Es5506Channels = 0;
            C352ClockDivider = 0;
            X1010Clock = 0;
            C352Clock = 0;
            Ga20Clock = 0;

            InitializeHeader(input);
        }

        // TODO: Reduce the branching here
        private void InitializeHeader(ReadOnlySpan<byte> input)
        {
            // +00: check the magic ID 
            _magic = input.ReadUInt32LittleEndian(0x00);

            if (Magic != MagicNumber)
            { // Not a valid VGM file

                return;
            }

            // +04: parse the size
            _endOfFileOffset = input.ReadUInt32LittleEndian(0x04);

            // +08: parse the version
            _version = input.ReadUInt32LittleEndian(0x08);

            // +0C: SN76489 clock
            Sn76489Clock = input.ReadUInt32LittleEndian(0x0C);

            // +10: YM2413 clock
            Ym2413Clock = input.ReadUInt32LittleEndian(0x10);

            // +14: GD3 offset
            _gd3Offset = input.ReadUInt32LittleEndian(0x14);

            // +18: Total # samples
            TotalNumSamples = input.ReadUInt32LittleEndian(0x18);

            // +1C: Loop offset
            _loopOffset = input.ReadUInt32LittleEndian(0x1C);

            // +20: Loop # samples
            LoopNumSamples = input.ReadUInt32LittleEndian(0x20);

            // +24: Rate
            Rate = input.ReadUInt32LittleEndian(0x24);

            // +28: SN76489 feedback
            Sn76489Feedback = input.ReadUInt16LittleEndian(0x28);

            // +2A: SN76489 shift register width
            Sn76489ShiftRegisterWidth = input[0x2A];

            // +2B: SN76489 Flags
            Sn76489Flags = input[0x2B];

            // +2C: YM2612 clock
            Ym2612Clock = input.ReadUInt32LittleEndian(0x2C);

            // +30: YM2151 clock
            Ym2151Clock = input.ReadUInt32LittleEndian(0x30);

            // +34: VGM data offset
            _vgmDataOffset = input.ReadUInt32LittleEndian(0x34);
            _vgmDataOffset += 0x34U;
            if (Version < 0x150)
            {
                _vgmDataOffset = 0x40;
            }

            // +38: Sega PCM clock
            SegaPcmClock = input.ReadUInt32LittleEndian(0x38);

            // +3C: Sega PCM interface register
            SegaPcmInterfaceRegister = input.ReadUInt32LittleEndian(0x3C);

            // +40: RF5C68 clock
            if (0x40 + 4 > VgmDataOffset) return;
            Rf5c68Clock = input.ReadUInt32LittleEndian(0x40);

            // +44: YM2203 clock
            if (0x44 + 4 > VgmDataOffset) return;
            Ym2203Clock = input.ReadUInt32LittleEndian(0x44);

            // +48: YM2608 clock
            if (0x48 + 4 > VgmDataOffset) return;
            Ym2608Clock = input.ReadUInt32LittleEndian(0x48);

            // +4C: YM2610/2610B clock
            if (0x4C + 4 > VgmDataOffset) return;
            Ym2610Clock = input.ReadUInt32LittleEndian(0x4C);

            // +50: YM3812 clock
            if (0x50 + 4 > VgmDataOffset) return;
            Ym3812Clock = input.ReadUInt32LittleEndian(0x50);

            // +54: YM3526 clock
            if (0x54 + 4 > VgmDataOffset) return;
            Ym3526Clock = input.ReadUInt32LittleEndian(0x54);

            // +58: Y8950 clock
            if (0x58 + 4 > VgmDataOffset) return;
            Y8950Clock = input.ReadUInt32LittleEndian(0x58);

            // +5C: YMF262 clock
            if (0x5C + 4 > VgmDataOffset) return;
            Ymf262Clock = input.ReadUInt32LittleEndian(0x5C);

            // +60: YMF278B clock
            if (0x60 + 4 > VgmDataOffset) return;
            Ymf278bClock = input.ReadUInt32LittleEndian(0x60);

            // +64: YMF271 clock
            if (0x64 + 4 > VgmDataOffset) return;
            Ymf271Clock = input.ReadUInt32LittleEndian(0x64);

            // +68: YMZ280B clock
            if (0x68 + 4 > VgmDataOffset) return;
            Ymz280bClock = input.ReadUInt32LittleEndian(0x68);

            // +6C: RF5C164 clock
            if (0x6C + 4 > VgmDataOffset) return;
            Rf5c164Clock = input.ReadUInt32LittleEndian(0x6C);

            // +70: PWM clock
            if (0x70 + 4 > VgmDataOffset) return;
            PwmClock = input.ReadUInt32LittleEndian(0x70);

            // +74: AY8910 clock
            if (0x74 + 4 > VgmDataOffset) return;
            Ay8910Clock = input.ReadUInt32LittleEndian(0x74);

            // +78: AY8910 chip type
            if (0x78 + 4 > VgmDataOffset) return;
            Ay8910ChipType = input[0x78];

            // +79: AY8910 flags
            Ay8910Flags = input[0x79];

            // +7A: YM2203/AY8910 flags
            Ym2203Ay8910Flags = input[0x7A];

            // +7B: YM2608/AY8910 flags
            Ym2608Ay8910Flags = input[0x7B];

            // +7C: Volume modifier
            if (0x7C + 4 > VgmDataOffset) return;
            VolumeModifier = input[0x7C];

            // +7D: Reserved
            // +7E: Loop base
            LoopBase = input[0x7E];

            // +7F: Loop modifier
            LoopModifier = input[0x7F];

            // +80: GameBoy DMG clock
            if (0x80 + 4 > VgmDataOffset) return;
            GameBoyDmgClock = input.ReadUInt32LittleEndian(0x80);

            // +84: NES APU clock
            if (0x84 + 4 > VgmDataOffset) return;
            NesApuClock = input.ReadUInt32LittleEndian(0x84);

            // +88: MultiPCM clock
            if (0x88 + 4 > VgmDataOffset) return;
            MultiPcmClock = input.ReadUInt32LittleEndian(0x88);

            // +8C: uPD7759 clock
            if (0x8C + 4 > VgmDataOffset) return;
            Upd7759Clock = input.ReadUInt32LittleEndian(0x8C);

            // +90: OKIM6258 clock
            if (0x90 + 4 > VgmDataOffset) return;
            Okim6258Clock = input.ReadUInt32LittleEndian(0x90);

            // +94: OKIM6258 Flags / K054539 Flags / C140 Chip Type / reserved
            if (0x94 + 4 > VgmDataOffset) return;
            Okim6258Flags = input[0x94];
            K054539Flags = input[0x95];
            C140ChipType = input[0x96];

            // +98: OKIM6295 clock
            if (0x98 + 4 > VgmDataOffset) return;
            Okim6295Clock = input.ReadUInt32LittleEndian(0x98);

            // +9C: K051649 clock
            if (0x9C + 4 > VgmDataOffset) return;
            K051649Clock = input.ReadUInt32LittleEndian(0x9C);

            // +A0: K054539 clock
            if (0xA0 + 4 > VgmDataOffset) return;
            K054539Clock = input.ReadUInt32LittleEndian(0xA0);

            // +A4: HuC6280 clock
            if (0xA4 + 4 > VgmDataOffset) return;
            HuC6280Clock = input.ReadUInt32LittleEndian(0xA4);

            // +A8: C140 clock
            if (0xA8 + 4 > VgmDataOffset) return;
            C140Clock = input.ReadUInt32LittleEndian(0xA8);

            // +AC: K053260 clock
            if (0xAC + 4 > VgmDataOffset) return;
            K053260Clock = input.ReadUInt32LittleEndian(0xAC);

            // +B0: Pokey clock
            if (0xB0 + 4 > VgmDataOffset) return;
            PokeyClock = input.ReadUInt32LittleEndian(0xB0);

            // +B4: QSound clock
            if (0xB4 + 4 > VgmDataOffset) return;
            QsoundClock = input.ReadUInt32LittleEndian(0xB4);

            // +B8: SCSP clock
            if (0xB8 + 4 > VgmDataOffset) return;
            ScspClock = input.ReadUInt32LittleEndian(0xB8);

            // +BC: extra header offset
            if (0xBC + 4 > VgmDataOffset) return;
            ExtraHeaderOffset = input.ReadUInt32LittleEndian(0xBC);

            // +C0: WonderSwan clock
            if (0xC0 + 4 > VgmDataOffset) return;
            WonderSwanClock = input.ReadUInt32LittleEndian(0xC0);

            // +C4: VSU clock
            if (0xC4 + 4 > VgmDataOffset) return;
            VsuClock = input.ReadUInt32LittleEndian(0xC4);

            // +C8: SAA1099 clock
            if (0xC8 + 4 > VgmDataOffset) return;
            Saa1099Clock = input.ReadUInt32LittleEndian(0xC8);

            // +CC: ES5503 clock
            if (0xCC + 4 > VgmDataOffset) return;
            Es5503Clock = input.ReadUInt32LittleEndian(0xCC);

            // +D0: ES5505/ES5506 clock
            if (0xD0 + 4 > VgmDataOffset) return;
            Es5505Es5506Clock = input.ReadUInt32LittleEndian(0xD0);

            // +D4: ES5503 output channels / ES5505/ES5506 amount of output channels / C352 clock divider
            if (0xD4 + 4 > VgmDataOffset) return;
            Es5503Channels = input[0xD4];
            Es5505Es5506Channels = input[0xD5];
            C352ClockDivider = input[0xD6];

            // +D8: X1-010 clock
            if (0xD8 + 4 > VgmDataOffset) return;
            X1010Clock = input.ReadUInt32LittleEndian(0xD8);

            // +DC: C352 clock
            if (0xDC + 4 > VgmDataOffset) return;
            C352Clock = input.ReadUInt32LittleEndian(0xDC);

            // +E0: GA20 clock
            if (0xE0 + 4 > VgmDataOffset) return;
            Ga20Clock = input.ReadUInt32LittleEndian(0xE0);
        }

#if UNITY_EDITOR
        private static uint HexLabelDrawer(uint value, GUIContent label)
        {
            EditorGUILayout.LabelField(label.ToString(), $"0x{value:x8}");
            return value;
        }

        private static uint LengthDrawer(uint value, GUIContent label)
        {
            EditorGUILayout.LabelField(
                label.ToString(),
                $"{value:N0} B == {StringUtilities.NicifyByteSize((int)value)} (0x{value:x8})"
            );
            return value;
        }
#endif
    }
}
