namespace Ymfm
{
    public interface IRegisterConstants
    {
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

        public virtual bool DynamicOps => false;
        public virtual bool EgHasDepress => false;
        public virtual bool EgHasReverb => false;
        public virtual bool EgHasSsg => false;
        public virtual bool ModulatorDelay => false;

    }
}
