namespace Ymfm
{
    // this class represents functions in the engine that the ymfm_interface
    // needs to be able to call; it is represented here as a separate interface
    // that is independent of the actual engine implementation
    public interface IEngineCallbacks
    {
        // timer callback; called by the interface when a timer fires
        public void TimerExpired(uint timerNumber);

        // check interrupts; called by the interface after synchronization
        public void CheckInterrupts();

        // mode register write; called by the interface after synchronization
        public void ModeWrite(byte data);
    }
}
