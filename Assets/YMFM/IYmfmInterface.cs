namespace Ymfm
{
// this class represents the interface between the fm_engine and the outside
// world; it provides hooks for timers, synchronization, and I/O
    public interface IYmfmInterface
    {
        // the following functions must be implemented by any derived classes; the
        // default implementations are sufficient for some minimal operation, but will
        // likely need to be overridden to integrate with the outside world; they are
        // all prefixed with ymfm_ to reduce the likelihood of namespace collisions

        //
        // timing and synchronizaton
        //

        // the chip implementation calls this when a write happens to the mode
        // register, which could affect timers and interrupts; our responsibility
        // is to ensure the system is up to date before calling the engine's
        // engine_mode_write() method
        public virtual void SyncModeWrite(byte data)
        {
            Engine.ModeWrite(data);
        }

        // the chip implementation calls this when the chip's status has changed,
        // which may affect the interrupt state; our responsibility is to ensure
        // the system is up to date before calling the engine's
        // engine_check_interrupts() method
        public virtual void SyncCheckInterrupts()
        {
            Engine.CheckInterrupts();
        }

        // the chip implementation calls this when one of the two internal timers
        // has changed state; our responsibility is to arrange to call the engine's
        // engine_timer_expired() method after the provided number of clocks; if
        // duration_in_clocks is negative, we should cancel any outstanding timers
        public virtual void SetTimer(uint timerNumber, int durationInClocks)
        {
        }

        // the chip implementation calls this to indicate that the chip should be
        // considered in a busy state until the given number of clocks has passed;
        // our responsibility is to compute and remember the ending time based on
        // the chip's clock for later checking
        public virtual void SetBusyEnd(uint clocks)
        {
        }

        // the chip implementation calls this to see if the chip is still currently
        // is a busy state, as specified by a previous call to ymfm_set_busy_end();
        // our responsibility is to compare the current time against the previously
        // noted busy end time and return true if we haven't yet passed it
        public virtual bool IsBusy => false;
        //
        // I/O functions
        //

        // the chip implementation calls this when the state of the IRQ signal has
        // changed due to a status change; our responsibility is to respond as
        // needed to the change in IRQ state, signaling any consumers
        public virtual void UpdateIrq(bool asserted)
        {
        }

        // the chip implementation calls this whenever data is read from outside
        // of the chip; our responsibility is to provide the data requested
        public virtual byte ExternalRead(AccessClass type, uint address)
        {
            return 0;
        }

        // the chip implementation calls this whenever data is written outside
        // of the chip; our responsibility is to pass the written data on to any consumers
        public virtual void ExternalWrite(AccessClass type, uint address, byte data)
        {
        }

        // pointer to engine callbacks -- this is set directly by the engine at
        // construction time
        protected internal IEngineCallbacks Engine { get; internal set; }
    };
}
