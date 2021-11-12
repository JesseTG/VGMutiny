using System;
using JetBrains.Annotations;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace Ymfm
{
    using uint8_t = System.Byte;
    using uint32_t = System.UInt32;
    using int32_t = System.Int32;
    using uint16_t = System.UInt16;

// fm_engine_base represents a set of operators and channels which together
// form a Yamaha FM core; chips that implement other engines (ADPCM, wavetable,
// etc) take this output and combine it with the others externally
    public class FmEngineBase<TRegisters, TOutput, TOperatorMapping> : IEngineCallbacks
        where TRegisters : class, IFmRegisters<TOperatorMapping>, new()
        where TOutput : struct, IOutput
        where TOperatorMapping : struct, IOperatorMapping
    {
        // constructor
        public FmEngineBase(IYmfmInterface @interface)
        {
            Interface = @interface ?? throw new ArgumentNullException(nameof(@interface));
            Registers = new TRegisters();
            m_env_counter = 0;
            m_status = 0;
            m_clock_prescale = (byte)Registers.DefaultPrescale;
            m_irq_mask = (byte)(Registers.StatusTimerA | Registers.StatusTimerB);
            m_irq_state = 0;
            m_timer_running = new byte[] { 0, 0 };
            m_active_channels = Registers.AllChannels;
            m_modified_channels = Registers.AllChannels;
            m_prepare_count = 0;

            // inform the interface of their engine
            Interface.Engine = this;

            // create the channels
            _channels = new FmChannel<TRegisters, TOutput, TOperatorMapping>[Registers.Channels];
            for (var chnum = 0u; chnum < _channels.Length; chnum++)
            {
                _channels[chnum] = new FmChannel<TRegisters, TOutput, TOperatorMapping>(this, Registers.ChannelOffset(chnum));
            }

            // create the operators
            _operators = new FmOperator<TRegisters, TOutput, TOperatorMapping>[Registers.Operators];
            for (var opnum = 0u; opnum < _operators.Length; opnum++)
            {
                _operators[opnum] = new FmOperator<TRegisters, TOutput, TOperatorMapping>(this, Registers.OperatorOffset(opnum));
            }

            // do the initial operator assignment
            AssignOperators();
        }

        // save/restore
        public void SaveRestore(YmfmSavedState state)
        {
            // save our data
            state.SaveRestore(ref m_env_counter);
            state.SaveRestore(ref m_status);
            state.SaveRestore(ref m_clock_prescale);
            state.SaveRestore(ref m_irq_mask);
            state.SaveRestore(ref m_irq_state);
            state.SaveRestore(ref m_timer_running[0]);
            state.SaveRestore(ref m_timer_running[1]);

            // save the register/family data
            Registers.SaveRestore(state);

            // save channel data
            foreach (var c in _channels)
            {
                c.SaveRestore(state);
            }

            // save operator data
            foreach (var op in _operators)
            {
                op.SaveRestore(state);
            }

            // invalidate any caches
            InvalidateCaches();
        }

        // reset the overall state
        public void Reset()
        {
            // reset all status bits
            SetResetStatus(0, 0xff);

            // register type-specific initialization
            Registers.Reset();

            // explicitly write to the mode register since it has side-effects
            // QUESTION: old cores initialize this to 0x30 -- who is right?
            Write((ushort)Registers.RegisterMode, 0);

            // reset the channels
            foreach (var chan in _channels)
            {
                chan.Reset();
            }

            // reset the operators
            foreach (var op in _operators)
            {
                op.Reset();
            }
        }

        //-------------------------------------------------
        //  clock - iterate over all channels, clocking
        //  them forward one step
        //-------------------------------------------------
        public uint Clock(uint channelMask)
        {
            // if something was modified, prepare
            // also prepare every 4k samples to catch ending notes
            if (m_modified_channels != 0 || m_prepare_count++ >= 4096)
            {
                // reassign operators to channels if dynamic
                if (Registers.DynamicOps)
                {
                    AssignOperators();
                }

                // call each channel to prepare
                m_active_channels = 0;
                for (var chnum = 0; chnum < _channels.Length; chnum++)
                {
                    if (Utils.Bitfield(channelMask, chnum) != 0 && _channels[chnum].Prepare())
                    {
                        m_active_channels |= 1u << chnum;
                    }
                }

                // reset the modified channels and prepare count
                m_modified_channels = m_prepare_count = 0;
            }

            // if the envelope clock divider is 1, just increment by 4;
            // otherwise, increment by 1 and manually wrap when we reach the divide count
            if (Registers.EgClockDivider == 1)
            {
                m_env_counter += 4;
            }
            else if (Utils.Bitfield(++m_env_counter, 0, 2) == Registers.EgClockDivider)
            {
                m_env_counter += 4 - Registers.EgClockDivider;
            }

            // clock the noise generator
            var lfoRawPm = Registers.ClockNoiseAndLfo();

            // now update the state of all the channels and operators
            for (var chnum = 0; chnum < _channels.Length; chnum++)
            {
                if (Utils.Bitfield(channelMask, chnum) != 0)
                {
                    _channels[chnum].Clock(m_env_counter, lfoRawPm);
                }
            }


            // return the envelope counter as it is used to clock ADPCM-A
            return m_env_counter;
        }

        // compute sum of channel outputs
        public void Output(ref TOutput output, uint rShift, int clipMax, uint channelMask)
        {
            // mask out some channels for debug purposes
            channelMask &= YmfmDebug.GLOBAL_FM_CHANNEL_MASK;

            // mask out inactive channels
            channelMask &= m_active_channels;

            // handle the rhythm case, where some of the operators are dedicated
            // to percussion (this is an OPL-specific feature)
            if (Registers.RhythmEnable != 0)
            {
                // we don't support the OPM noise channel here; ensure it is off
                Assert.AreEqual(0, Registers.NoiseEnable);

                // precompute the operator 13+17 phase selection value
                var op13Phase = _operators[13].Phase;
                var op17Phase = _operators[17].Phase;
                var phaseSelect = (Utils.Bitfield(op13Phase, 2) ^ Utils.Bitfield(op13Phase, 7)) |
                                  Utils.Bitfield(op13Phase, 3) |
                                  (Utils.Bitfield(op17Phase, 5) ^ Utils.Bitfield(op17Phase, 3));

                // sum over all the desired channels
                for (var chnum = 0; chnum < _channels.Length; chnum++)
                {
                    if (Utils.Bitfield(channelMask, chnum) != 0)
                    {
                        if (chnum == 6)
                            _channels[chnum].OutputRhythmChannel6(ref output, rShift, clipMax);
                        else if (chnum == 7)
                            _channels[chnum].OutputRhythmChannel7(phaseSelect, ref output, rShift, clipMax);
                        else if (chnum == 8)
                            _channels[chnum].OutputRhythmChannel8(phaseSelect, ref output, rShift, clipMax);
                        else if (_channels[chnum].Is4Op)
                            _channels[chnum].Output4Op(ref output, rShift, clipMax);
                        else
                            _channels[chnum].Output2Op(ref output, rShift, clipMax);
                    }
                }
            }
            else
            {
                // sum over all the desired channels
                for (var chnum = 0; chnum < _channels.Length; chnum++)
                {
                    if (Utils.Bitfield(channelMask, chnum) != 0)
                    {
                        if (_channels[chnum].Is4Op)
                            _channels[chnum].Output4Op(ref output, rShift, clipMax);
                        else
                            _channels[chnum].Output2Op(ref output, rShift, clipMax);
                    }
                }
            }
        }

        // write to the OPN registers
        public void Write(ushort regNum, byte data)
        {
            // special case: writes to the mode register can impact IRQs;
            // schedule these writes to ensure ordering with timers
            if (regNum == Registers.RegisterMode)
            {
                Interface.SyncModeWrite(data);
                return;
            }

            // for now just mark all channels as modified
            m_modified_channels = Registers.AllChannels;

            // most writes are passive, consumed only when needed
            uint keyOnChannel = 0;
            uint keyOnOpMask = 0;
            if (Registers.Write(regNum, data, ref keyOnChannel, ref keyOnOpMask))
            {
                // handle writes to the keyon register(s)
                if (keyOnChannel < Registers.Channels)
                {
                    // normal channel on/off
                    _channels[keyOnChannel].KeyOnOff(keyOnOpMask, KeyOnType.Normal, keyOnChannel);
                }
                else if (Registers.Channels >= 9 && keyOnChannel == Registers.RhythmChannel)
                {
                    // special case for the OPL rhythm channels
                    _channels[6].KeyOnOff((uint)(Utils.Bitfield(keyOnOpMask, 4) != 0 ? 3 : 0), KeyOnType.Rhythm, 6);
                    _channels[7].KeyOnOff(Utils.Bitfield(keyOnOpMask, 0) | (Utils.Bitfield(keyOnOpMask, 3) << 1),
                        KeyOnType.Rhythm,
                        7);
                    _channels[8].KeyOnOff(Utils.Bitfield(keyOnOpMask, 2) | (Utils.Bitfield(keyOnOpMask, 1) << 1),
                        KeyOnType.Rhythm,
                        8);
                }
            }
        }

        //-------------------------------------------------
        //  status - return the current state of the
        //  status flags
        //-------------------------------------------------
        public byte Status => (byte)(m_status & ~Registers.StatusBusy & ~Registers.StatusMask);

        // set/reset bits in the status register, updating the IRQ status
        public byte SetResetStatus(byte set, byte reset)
        {
            m_status = (byte)((m_status | set) & ~(reset | Registers.StatusBusy));
            Interface.SyncCheckInterrupts();
            return (byte)(m_status & ~Registers.StatusMask);
        }

        // set the IRQ mask
        public void SetIrqMask(byte mask)
        {
            m_irq_mask = mask;
            Interface.SyncCheckInterrupts();
        }

        // return the current clock prescale
        // set prescale factor (2/3/6)
        public uint ClockPrescale
        {
            get => m_clock_prescale;
            set => m_clock_prescale = (byte)value;
        }

        // compute sample rate
        [Pure]
        public uint SampleRate(uint baseClock)
        {
            return baseClock / (m_clock_prescale * Registers.Operators);
        }

        // return the owning device
        [NotNull]
        public IYmfmInterface Interface { get; }

        // return a reference to our registers
        [NotNull]
        public TRegisters Registers { get; }

        // invalidate any caches
        public void InvalidateCaches()
        {
            m_modified_channels = Registers.AllChannels;
        }

        // simple getters for debugging
        public FmChannel<TRegisters, TOutput, TOperatorMapping> DebugChannel(uint index)
        {
            return _channels[index];
        }

        public FmOperator<TRegisters, TOutput, TOperatorMapping> DebugOperator(uint index)
        {
            return _operators[index];
        }

        // timer callback; called by the interface when a timer fires
        public virtual void TimerExpired(uint timerNumber)
        {
            // update status
            if (timerNumber == 0 && Registers.EnableTimerA != 0)
            {
                SetResetStatus(Registers.StatusTimerA, 0);
            }
            else if (timerNumber == 1 && Registers.EnableTimerB != 0)
            {
                SetResetStatus(Registers.StatusTimerB, 0);
            }

            // if timer A fired in CSM mode, trigger CSM on all relevant channels
            if (timerNumber == 0 && Registers.Csm != 0)
            {
                for (var chnum = 0; chnum < _channels.Length; chnum++)
                {
                    if (Utils.Bitfield(Registers.CsmTriggerMask, chnum) != 0)
                    {
                        _channels[chnum].KeyOnOff(1, KeyOnType.Csm, (uint)chnum);
                    }
                }
            }

            // reset
            m_timer_running[timerNumber] = 0;
            UpdateTimer(timerNumber, 1);
        }

        // check interrupts; called by the interface after synchronization
        public virtual void CheckInterrupts()
        {
            // update the state
            var oldState = m_irq_state;
            m_irq_state = (byte)(((m_status & m_irq_mask & ~Registers.StatusMask) != 0) ? 1 : 0);

            // set the IRQ status bit
            if (m_irq_state != 0)
            {
                m_status |= Registers.StatusIrq;
            }
            else
            {
                m_status &= (byte)~Registers.StatusIrq;
            }

            // if changed, signal the new state
            if (oldState != m_irq_state)
            {
                Interface.UpdateIrq(m_irq_state != 0);
            }
        }

        // mode register write; called by the interface after synchronization
        public virtual void ModeWrite(byte data)
        {
            // mark all channels as modified
            m_modified_channels = Registers.AllChannels;

            // actually write the mode register now
            uint dummy1 = 0;
            uint dummy2 = 0;
            Registers.Write((ushort)Registers.RegisterMode, data, ref dummy1, ref dummy2);

            // reset IRQ status -- when written, all other bits are ignored
            // QUESTION: should this maybe just reset the IRQ bit and not all the bits?
            //   That is, check_interrupts would only set, this would only clear?
            if (Registers.IrqReset != 0)
            {
                SetResetStatus(0, 0x78);
            }
            else
            {
                // reset timer status
                byte resetMask = 0;
                if (Registers.ResetTimerB != 0)
                {
                    resetMask |= Registers.StatusTimerB;
                }

                if (Registers.ResetTimerA != 0)
                {
                    resetMask |= Registers.StatusTimerA;
                }

                SetResetStatus(0, resetMask);

                // load timers
                UpdateTimer(1, Registers.LoadTimerB);
                UpdateTimer(0, Registers.LoadTimerA);
            }
        }

        // assign the current set of operators to channels
        protected void AssignOperators()
        {
            var map = new TOperatorMapping(); 
            Registers.OperatorMap(ref map);

            for (var channel = 0; channel < Registers.Channels; channel++)
            {
                for (var index = 0u; index < 4; index++)
                {
                    var op = Utils.Bitfield(map.OperatorIndexes[channel], (int)(8 * index), 8);
                    _channels[channel].Assign(index, (op == 0xff) ? null : _operators[op]);
                }
            }
        }

        // update the state of the given timer
        protected void UpdateTimer(uint timerNumber, uint enable)
        {
            // if the timer is live, but not currently enabled, set the timer
            if (enable != 0 && m_timer_running[timerNumber] == 0)
            {
                // period comes from the registers, and is different for each
                var period = (timerNumber == 0)
                    ? (1024 - Registers.TimerAValue)
                    : 16 * (256 - Registers.TimerBValue);

                // reset it
                Interface.SetTimer(timerNumber, (int)(period * _operators.Length * m_clock_prescale));
                m_timer_running[timerNumber] = 1;
            }

            // if the timer is not live, ensure it is not enabled
            else if (enable == 0)
            {
                Interface.SetTimer(timerNumber, -1);
                m_timer_running[timerNumber] = 0;
            }
        }

        // internal state
        protected uint m_env_counter; // envelope counter; low 2 bits are sub-counter
        protected byte m_status; // current status register
        protected byte m_clock_prescale; // prescale factor (2/3/6)
        protected byte m_irq_mask; // mask of which bits signal IRQs
        protected byte m_irq_state; // current IRQ state
        protected readonly byte[] m_timer_running; // current timer running state
        protected uint m_active_channels; // mask of active channels (computed by prepare)
        protected uint m_modified_channels; // mask of channels that have been modified
        protected uint m_prepare_count; // counter to do periodic prepare sweeps
        protected readonly FmChannel<TRegisters, TOutput, TOperatorMapping>[] _channels; // channel pointers
        protected readonly FmOperator<TRegisters, TOutput, TOperatorMapping>[] _operators; // operator pointers
    };
}
