using System;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine;

namespace Ymfm
{
    using uint32_t = System.UInt32;
    using uint8_t = System.Byte;

    // base class for family-specific register classes; this provides a few
    // constants, common defaults, and helpers, but mostly each derived class is
    // responsible for defining all commonly-called methods
    public interface IFmRegisters<TOperatorMapping> where TOperatorMapping : struct, IOperatorMapping
    {
        // this is the size of a full sin waveform
        public virtual uint WaveformLength => 0x400;

        //
        // the following constants need to be defined per family:
        //          uint32_t OUTPUTS: The number of outputs exposed (1-4)
        public abstract uint Outputs { get; }

        //         uint32_t CHANNELS: The number of channels on the chip
        public abstract uint Channels { get; }

        //     uint32_t ALL_CHANNELS: A bitmask of all channels
        public abstract uint AllChannels { get; }

        //        uint32_t OPERATORS: The number of operators on the chip
        public abstract uint Operators { get; }

        //        uint32_t WAVEFORMS: The number of waveforms offered
        public abstract uint Waveforms { get; }

        //        uint32_t REGISTERS: The number of 8-bit registers allocated
        public abstract uint Registers { get; }

        // uint32_t DEFAULT_PRESCALE: The starting clock prescale
        public abstract uint DefaultPrescale { get; }

        // uint32_t EG_CLOCK_DIVIDER: The clock divider of the envelope generator
        public abstract uint EgClockDivider { get; }

        // uint32_t CSM_TRIGGER_MASK: Mask of channels to trigger in CSM mode
        public abstract uint CsmTriggerMask { get; }

        //         uint32_t REG_MODE: The address of the "mode" register controlling timers
        public abstract uint RegisterMode { get; }

        //     uint8_t STATUS_TIMERA: Status bit to set when timer A fires
        public abstract byte StatusTimerA { get; }

        //     uint8_t STATUS_TIMERB: Status bit to set when tiemr B fires
        public abstract byte StatusTimerB { get; }

        //       uint8_t STATUS_BUSY: Status bit to set when the chip is busy
        public abstract byte StatusBusy { get; }

        //        uint8_t STATUS_IRQ: Status bit to set when an IRQ is signalled
        public abstract byte StatusIrq { get; }

        //
        // the following constants are uncommon:
        //          bool DYNAMIC_OPS: True if ops/channel can be changed at runtime (OPL3+)
        //       bool EG_HAS_DEPRESS: True if the chip has a DP ("depress"?) envelope stage (OPLL)
        //        bool EG_HAS_REVERB: True if the chip has a faux reverb envelope stage (OPQ/OPZ)
        //           bool EG_HAS_SSG: True if the chip has SSG envelope support (OPN)
        //      bool MODULATOR_DELAY: True if the modulator is delayed by 1 sample (OPL pre-OPL3)
        //
        public virtual bool DynamicOps => false;
        public virtual bool EgHasDepress => false;
        public virtual bool EgHasReverb => false;
        public virtual bool EgHasSsg => false;
        public virtual bool ModulatorDelay => false;

        // this value is returned from the write() function for rhythm channels
        public virtual uint RhythmChannel => 0xff;


        // system-wide register defaults
        public virtual uint StatusMask => 0; // OPL only

        public virtual uint IrqReset => 0; // OPL only

        public virtual uint NoiseEnable => 0; // OPM only
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
        public virtual uint RhythmEnable => 0; // OPL only

        // per-operator register defaults
        public virtual uint OpSsgEgEnable(uint opoffs) => 0; // OPN(A) only

        public virtual uint OpSsgEgMode(uint opoffs) => 0; // OPN(A) only

        public void OperatorMap(ref TOperatorMapping dest);

        // caching helpers
        public void CacheOperatorData(uint channelOffset, uint operatorOffset, ref OpDataCache cache);
        // compute the phase step, given a PM value

        public uint ComputePhaseStep(uint channelOffset, uint operatorOffset, in OpDataCache cache, int lfoRawPm);

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
