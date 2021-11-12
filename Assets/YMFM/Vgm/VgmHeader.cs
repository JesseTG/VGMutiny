using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace Ymfm.Vgm
{
    /// <seealso href="https://vgmrips.net/wiki/VGM_Specification"/>
    [Serializable]
    public struct VgmHeader
    {
        /// <summary>
        /// Relative offset to end of file (i.e. file length - 4). This is mainly used to find the next track when
        /// concatenating player stubs and multiple files. 
        /// </summary>
        [FieldOffset(0x04)]
        public uint EndOfFileOffset;

        /// <summary>
        /// Version number in BCD-Code. e.g. Version 1.70 is stored as <c>0x00000171</c>. This is used for backwards
        /// compatibility in players, and defines which header values are valid. 
        /// </summary>
        [FieldOffset(0x08)]
        public uint Version;

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
        [FieldOffset(0x0C)]
        public uint Sn76489Clock;

        /// <summary>
        /// Input clock rate in Hz for the YM2413 chip. A typical value is <c>3579545</c>.
        /// It should be 0 if there is no YM2413 chip used. 
        /// </summary>
        [FieldOffset(0x10)]
        public uint Ym2413Clock;

        /// <summary>
        /// Relative offset to GD3 tag. 0 if no GD3 tag. GD3 tags are descriptive tags similar in use to ID3 tags in MP3
        /// files. See the GD3 specification for more details. The GD3 tag is usually stored immediately after the VGM data. 
        /// </summary>
        [FieldOffset(0x14)]
        public uint Gd3Offset;

        /// <summary>
        /// Total of all wait values in the file. 
        /// </summary>
        [FieldOffset(0x18)]
        public uint TotalNumSamples;

        /// <summary>
        /// Relative offset to loop point, or 0 if no loop. For example, if the data for the one-off intro to a song was
        /// in bytes <c>0x0040 - 0x3FFF</c> of the file, but the main looping section started at <c>0x4000</c>, this
        /// would contain the value <c>0x4000 - 0x1C = 0x00003FE4</c>. 
        /// </summary>
        [FieldOffset(0x1C)]
        public uint LoopOffset;


        /// <summary>
        /// Number of samples in one loop, or 0 if there is no loop. Total of all wait values between the loop point and
        /// the end of the file. 
        /// </summary>
        [FieldOffset(0x20)]
        public uint LoopNumSamples;

        #region VGM 1.01 Additions

        /// <summary>
        /// "Rate" of recording in Hz, used for rate scaling on playback. It is typically 50 for PAL systems and 60 for
        /// NTSC systems. It should be set to zero if rate scaling is not appropriate - for example, if the game adjusts
        /// its music engine for the system's speed. VGM 1.00 files will have a value of 0. 
        /// </summary>
        [FieldOffset(0x24)]
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
        [FieldOffset(0x28)]
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
        [FieldOffset(0x2A)]
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
        [FieldOffset(0x2B)]
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
        [FieldOffset(0x2C)]
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
        [FieldOffset(0x30)]
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
        [FieldOffset(0x34)]
        public uint VgmDataOffset;

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
        [FieldOffset(0x38)]
        public uint SegaPcmClock;

        /// <summary>
        /// <para>
        /// The interface register for the Sega PCM chip.
        /// </para>
        /// <para>
        /// It should be 0 if there is no Sega PCM chip used. 
        /// </para>
        /// </summary>
        [FieldOffset(0x3C)]
        public uint SegaPcmInterfaceRegister;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the RF5C68 PCM chip. A typical value is 12500000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no RF5C68 chip used. 
        /// </para>
        /// </summary>
        [FieldOffset(0x40)]
        public uint Rf5c68Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the YM2203 chip. A typical value is 3000000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no YM2203 chip used. 
        /// </para>
        /// </summary>
        [FieldOffset(0x44)]
        public uint Ym2203Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the YM2608 chip. A typical value is 8000000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no YM2608 chip used. 
        /// </para>
        /// </summary>
        [FieldOffset(0x48)]
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
        [FieldOffset(0x4C)]
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
        [FieldOffset(0x50)]
        public uint Ym3812Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the YM3526 chip. A typical value is 3579545.
        /// </para>
        /// <para>
        /// It should be 0 if there is no YM3526 chip used.
        /// </para>
        /// </summary>
        [FieldOffset(0x54)]
        public uint Ym3526Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the Y8950 chip. A typical value is 3579545.
        /// </para>
        /// <para>
        /// It should be 0 if there is no Y8950 chip used.
        /// </para>
        /// </summary>
        [FieldOffset(0x58)]
        public uint Y8950Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the YMF262 chip. A typical value is 14318180.
        /// </para>
        /// <para>
        /// It should be 0 if there is no YMF262 chip used.
        /// </para>
        /// </summary>
        [FieldOffset(0x5C)]
        public uint Ymf262Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the YMF278B chip. A typical value is 33868800.
        /// </para>
        /// <para>
        /// It should be 0 if there is no YMF278B chip used.
        /// </para>
        /// </summary>
        [FieldOffset(0x60)]
        public uint Ymf278bClock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the YMF271 chip. A typical value is 16934400.
        /// </para>
        /// <para>
        /// It should be 0 if there is no YMF271 chip used.
        /// </para>
        /// </summary>
        [FieldOffset(0x64)]
        public uint Ymf271Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the YMZ280B chip. A typical value is 16934400.
        /// </para>
        /// <para>
        /// It should be 0 if there is no YMZ280B chip used. 
        /// </para>
        /// </summary>
        [FieldOffset(0x68)]
        public uint Ymz280bClock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the RF5C164 PCM chip. A typical value is 12500000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no RF5C164 chip used. 
        /// </para>
        /// </summary>
        [FieldOffset(0x6C)]
        public uint Rf5c164Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the PWM chip. A typical value is 23011361.
        /// </para>
        /// <para>
        /// It should be 0 if there is no PWM chip used. 
        /// </para>
        /// </summary>
        [FieldOffset(0x70)]
        public uint PwmClock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the AY8910 chip. A typical value is 1789750.
        /// </para>
        /// <para>
        /// It should be 0 if there is no AY8910 chip used.
        /// </para>
        /// </summary>
        [FieldOffset(0x74)]
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
        [FieldOffset(0x78)]
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
        [FieldOffset(0x79)]
        public byte Ay8910Flags;

        /// <summary>
        /// Misc flags for the AY8910. This one is specific for the AY8910 that's connected with/part of the YM2203.
        /// </summary>
        [FieldOffset(0x7A)]
        public byte Ym2203Ay8910Flags;

        /// <summary>
        /// Misc flags for the AY8910. This one is specific for the AY8910 that's connected with/part of the YM2608.
        /// </summary>
        [FieldOffset(0x7B)]
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
        [FieldOffset(0x7C)]
        public byte VolumeModifier;

        /// <summary>
        /// Modifies the number of loops that are played before the playback ends. Set this value to eg. 1 to reduce the
        /// number of played loops by one. This is useful, if the song is looped twice in the vgm, because there are
        /// minor differences between the first and second loop and the song repeats just the second loop. The resulting
        /// number of loops that are played is calculated as following: NumLoops = NumLoopsModified - LoopBase Default
        /// is 0. Negative numbers are possible (80h...FFh = -128...-1)
        /// </summary>
        [FieldOffset(0x7E)]
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
        [FieldOffset(0x7F)]
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
        [FieldOffset(0x80)]
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
        [FieldOffset(0x84)]
        public uint NesApuClock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the MultiPCM chip. A typical value is 8053975.
        /// </para>
        /// <para>
        /// It should be 0 if there is no MultiPCM chip used.
        /// </para>
        /// </summary>
        [FieldOffset(0x88)]
        public uint MultiPcmClock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the uPD7759 chip. A typical value is 640000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no uPD7759 chip used.
        /// </para>
        /// </summary>
        [FieldOffset(0x8C)]
        public uint Upd7759Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the OKIM6258 chip. A typical value is 4000000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no OKIM6258 chip used.
        /// </para>
        /// </summary>
        [FieldOffset(0x90)]
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
        [FieldOffset(0x94)]
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
        [FieldOffset(0x95)]
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
        [FieldOffset(0x96)]
        public byte C140ChipType;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the OKIM6295 chip. A typical value is 8000000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no OKIM6295 chip used.
        /// </para>
        /// </summary>
        [FieldOffset(0x98)]
        public uint Okim6295Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the K051649 chip. A typical value is 1500000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no K051649 chip used.
        /// </para>
        /// </summary>
        [FieldOffset(0x9C)]
        public uint K051649Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the K054539 chip. A typical value is 18432000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no K054539 chip used.
        /// </para>
        /// </summary>
        [FieldOffset(0xA0)]
        public uint K054539Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the HuC6280 chip. A typical value is 3579545.
        /// </para>
        /// <para>
        /// It should be 0 if there is no HuC6280 chip used.
        /// </para>
        /// </summary>
        [FieldOffset(0xA4)]
        public uint HuC6280Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the C140 chip. A typical value is 21390.
        /// </para>
        /// <para>
        /// It should be 0 if there is no C140 chip used.
        /// </para>
        /// </summary>
        [FieldOffset(0xA8)]
        public uint C140Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the K053260 chip. A typical value is 3579545.
        /// </para>
        /// <para>
        /// It should be 0 if there is no K053260 chip used.
        /// </para>
        /// </summary>
        [FieldOffset(0xAC)]
        public uint K053260Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the Pokey chip. A typical value is 1789772.
        /// </para>
        /// <para>
        /// It should be 0 if there is no Pokey chip used.
        /// </para>
        /// </summary>
        [FieldOffset(0xB0)]
        public uint PokeyClock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the QSound chip. A typical value is 4000000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no QSound chip used.
        /// </para>
        /// </summary>
        [FieldOffset(0xB4)]
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
        [FieldOffset(0xB8)]
        public uint ScspClock;

        #endregion

        #region VGM 1.70 Additions

        /// <summary>
        /// Relative offset to the extra header or 0 if no extra header is present.
        /// </summary>
        [FieldOffset(0xBC)]
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
        [FieldOffset(0xC0)]
        public uint WonderSwanClock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the VSU chip. A typical value is 5000000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no VSU chip used.
        /// </para>
        /// </summary>
        [FieldOffset(0xC4)]
        public uint VsuClock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the SAA1099 chip. A typical value is 8000000 (or 7159000/7159090).
        /// </para>
        /// <para>
        /// It should be 0 if there is no SAA1099 chip used.
        /// </para>
        /// </summary>
        [FieldOffset(0xC8)]
        public uint Saa1099Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the ES5503 chip. A typical value is 7159090.
        /// </para>
        /// <para>
        /// It should be 0 if there is no ES5503 chip used.
        /// </para>
        /// </summary>
        [FieldOffset(0xCC)]
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
        [FieldOffset(0xD0)]
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
        [FieldOffset(0xD4)]
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
        [FieldOffset(0xD5)]
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
        [FieldOffset(0xD6)]
        public byte C352ClockDivider;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the X1-010 chip. A typical value is 16000000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no X1-010 chip used.
        /// </para>
        /// </summary>
        [FieldOffset(0xD8)]
        public uint X1010Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the C352 chip. A typical value is 24192000.
        /// </para>
        /// <para>
        /// It should be 0 if there is no C352 chip used.
        /// </para>
        /// </summary>
        [FieldOffset(0xDC)]
        public uint C352Clock;

        /// <summary>
        /// <para>
        /// Input clock rate in Hz for the GA20 chip. A typical value is 3579545.
        /// </para>
        /// <para>
        /// It should be 0 if there is no GA20 chip used.
        /// </para>
        /// </summary>
        [FieldOffset(0xE0)]
        public uint Ga20Clock;

        #endregion
    }
}
