using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;

namespace Ymfm
{
    /// <summary>
    /// Interface for family-specific register classes; this provides a few
    /// constants, common defaults, and helpers, but mostly each derived class is
    /// responsible for defining all commonly-called methods
    /// </summary>
    public interface IFmRegisters
    {
        /// <summary>
        /// This value is returned from <see cref="Write"/> for rhythm channels
        /// </summary>
        public virtual uint RhythmChannel => 0xff;

        /// <summary>
        /// this is the size of a full sin waveform
        /// </summary>
        public virtual uint WaveformLength => 0x400;

        /// <summary>
        /// The number of outputs exposed (1-4)
        /// </summary>
        public uint Outputs { get; }

        /// <summary>
        /// The number of channels on the chip
        /// </summary>
        public uint Channels { get; }

        /// <summary>
        /// A bitmask of all channels
        /// </summary>
        public uint AllChannels { get; }

        /// <summary>
        /// The number of operators on the chip
        /// </summary>
        public uint Operators { get; }

        /// <summary>
        /// The number of waveforms offered
        /// </summary>
        public uint Waveforms { get; }

        /// <summary>
        /// The number of 8-bit registers allocated
        /// </summary>
        public uint Registers { get; }

        /// <summary>
        /// The starting clock prescale
        /// </summary>
        public uint DefaultPrescale { get; }

        /// <summary>
        /// The clock divider of the envelope generator
        /// </summary>
        public uint EgClockDivider { get; }

        /// <summary>
        /// Mask of channels to trigger in CSM mode
        /// </summary>
        public uint CsmTriggerMask { get; }

        /// <summary>
        /// The address of the "mode" register controlling timers
        /// </summary>
        public uint RegisterMode { get; }

        /// <summary>
        /// Status bit to set when timer A fires
        /// </summary>
        public byte StatusTimerA { get; }

        /// <summary>
        /// Status bit to set when timer B fires
        /// </summary>
        public byte StatusTimerB { get; }

        /// <summary>
        /// Status bit to set when the chip is busy
        /// </summary>
        public byte StatusBusy { get; }

        /// <summary>
        /// Status bit to set when an IRQ is signalled
        /// </summary>
        public byte StatusIrq { get; }


        // the following constants are uncommon
        /// <summary>
        /// True if ops/channel can be changed at runtime (OPL3+)
        /// </summary>
        public virtual bool DynamicOps => false;

        /// <summary>
        /// True if the chip has a DP ("depress"?) envelope stage (OPLL)
        /// </summary>
        public virtual bool EgHasDepress => false;

        /// <summary>
        /// True if the chip has a faux reverb envelope stage (OPQ/OPZ)
        /// </summary>
        public virtual bool EgHasReverb => false;

        /// <summary>
        /// True if the chip has SSG envelope support (OPN)
        /// </summary>
        public virtual bool EgHasSsg => false;

        /// <summary>
        /// True if the modulator is delayed by 1 sample (OPL pre-OPL3)
        /// </summary>
        public virtual bool ModulatorDelay => false;


        // system-wide register defaults
        public virtual uint StatusMask => 0; // OPL only
        public virtual uint IrqReset => 0; // OPL only
        public virtual uint NoiseEnable => 0; // OPM only
        public virtual uint RhythmEnable => 0; // OPL only
        public virtual uint NoiseState => 0;
        public uint EnableTimerA { get; }
        public uint EnableTimerB { get; }
        public uint ResetTimerA { get; }
        public uint ResetTimerB { get; }
        public uint LoadTimerA { get; }
        public uint LoadTimerB { get; }
        public uint TimerAValue { get; }
        public uint TimerBValue { get; }
        public uint Csm { get; }

        // per-operator register defaults
        public virtual uint OpSsgEgEnable(uint opoffs) => 0; // OPN(A) only
        public virtual uint OpSsgEgMode(uint opoffs) => 0; // OPN(A) only

        public void OperatorMap(Span<uint> dest);

        // caching helpers
        public void CacheOperatorData(uint channelOffset, uint operatorOffset, ref OpDataCache cache);
        // compute the phase step, given a PM value

        public uint ComputePhaseStep(uint channelOffset, uint operatorOffset, ref OpDataCache cache, int lfoRawPm);

        // log a key-on event
        public string LogKeyOn(uint channelOffset, uint operatorOffset);
        public bool Write(ushort index, byte data, ref uint channel, ref uint opMask);

        public int ClockNoiseAndLfo();

        // return the AM offset from LFO for the given channel
        public uint LfoAmOffset(uint channelOffset);

        // per-channel registers
        public uint ChannelFeedback(uint channelOffset);
        public uint ChannelOutputAny(uint channelOffset);
        public uint ChannelAlgorithm(uint channelOffset);
        public uint ChannelOutput0(uint channelOffset);
        public uint ChannelOutput1(uint channelOffset);
        public uint ChannelOutput2(uint channelOffset);
        public uint ChannelOutput3(uint channelOffset);

        // per-operator registers

        public uint OpLfoAmEnable(uint operatorOffset);
        public void Reset();
        public void SaveRestore(YmfmSavedState state);

        // map channel number to register offset
        public uint ChannelOffset(uint chnum)
        {
            Debug.Assert(chnum < Channels);
            return chnum;
        }

        // map operator number to register offset
        public uint OperatorOffset(uint opnum)
        {
            Debug.Assert(opnum < Operators);
            return opnum;
        }

        // helper to encode four operator numbers into a 32-bit value in the
        // operator maps for each register class
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static uint OperatorList(
            byte o1 = 0xff,
            byte o2 = 0xff,
            byte o3 = 0xff,
            byte o4 = 0xff
        )
        {
            return (uint)(o1 | (o2 << 8) | (o3 << 16) | (o4 << 24));
        }

        // helper to apply KSR to the raw ADSR rate, ignoring ksr if the
        // raw value is 0, and clamping to 63
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static uint EffectiveRate(uint rawrate, uint ksr)
        {
            return (rawrate == 0) ? 0 : math.min(rawrate + ksr, 63);
        }
    };
}
