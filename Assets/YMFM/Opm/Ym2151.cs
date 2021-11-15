using System;
using JetBrains.Annotations;
using UnityEngine.Assertions;

namespace Ymfm.Opm
{
    public class Ym2151 : IChip<StereoOutput>
    {
        // constructor
        public Ym2151(IYmfmInterface @interface) : this(@interface, Variant.Ym2151)
        {
        }

        //-------------------------------------------------
        //  reset - reset the system
        //-------------------------------------------------
        public void Reset()
        {
            // reset the engines
            _fm.Reset();
        }

        //-------------------------------------------------
        //  save_restore - save or restore the data
        //-------------------------------------------------

        public void SaveRestore(YmfmSavedState state)
        {
            _fm.SaveRestore(state ?? throw new ArgumentNullException(nameof(state)));
            state.SaveRestore(ref _address);
        }

        // pass-through helpers
        [Pure]
        public uint SampleRate(uint inputClock) => _fm.SampleRate(inputClock);

        public void InvalidateCaches() => _fm.InvalidateCaches();

        //-------------------------------------------------
        //  read_status - read the status register
        //-------------------------------------------------
        public byte ReadStatus()
        {
            var result = _fm.Status;
            if (_fm.Interface.IsBusy)
            {
                result |= OpmRegisters.STATUS_BUSY;
            }

            return result;
        }

        //-------------------------------------------------
        //  read - handle a read from the device
        //-------------------------------------------------
        public byte Read(uint offset)
        {
            byte result = 0xff;
            switch (offset & 1)
            {
                case 0: // data port (unused)
                    Ymfm.YmfmDebug.LogUnexpectedReadWrite("Unexpected read from YM2151 offset %d\n", offset & 3);
                    break;

                case 1: // status port, YM2203 compatible
                    result = ReadStatus();
                    break;
            }

            return result;
        }


        //-------------------------------------------------
        //  write_address - handle a write to the address
        //  register
        //-------------------------------------------------
        public void WriteAddress(byte data)
        {
            // just set the address
            _address = data;
        }

        //-------------------------------------------------
        //  write - handle a write to the register
        //  interface
        //-------------------------------------------------
        public void WriteData(byte data)
        {
            // write the FM register
            _fm.Write(_address, data);

            // special cases
            if (_address == 0x1b)
            {
                // writes to register 0x1B send the upper 2 bits to the output lines
                _fm.Interface.ExternalWrite(AccessClass.Io, 0, (byte)(data >> 6));
            }

            // mark busy for a bit
            _fm.Interface.SetBusyEnd(32 * _fm.ClockPrescale);
        }

        //-------------------------------------------------
        //  write - handle a write to the register
        //  interface
        //-------------------------------------------------
        public void Write(uint offset, byte data)
        {
            switch (offset & 1)
            {
                case 0: // address port
                    WriteAddress(data);
                    break;

                case 1: // data port
                    WriteData(data);
                    break;
            }
        }

        // generate one sample of sound
        public void Generate(Span<StereoOutput> output)
        {
            for (var index = 0; index < output.Length; index++)
            {
                // clock the system
                _fm.Clock(OpmRegisters.ALL_CHANNELS);

                // update the FM content; OPM is full 14-bit with no intermediate clipping

                output[index].Clear();

                Assert.AreEqual(0, output[index][0]);
                _fm.Output(ref output[index], 0, short.MaxValue, OpmRegisters.ALL_CHANNELS);

                // YM2151 uses an external DAC (YM3012) with mantissa/exponent format
                // convert to 10.3 floating point value and back to simulate truncation
                output[index].RoundTripFp();
            }
        }

        public uint Outputs => OpmRegisters.OUTPUTS;

        // variants
        protected enum Variant
        {
            Ym2151,
            Ym2164,
        }

        // internal constructor
        protected Ym2151(IYmfmInterface @interface, Variant variant) : this(
            new FmEngineBase<OpmRegisters, StereoOutput, OpmRegisters.OpmOperatorMapping>(@interface),
            variant
        )
        {
        }

        protected Ym2151(
            FmEngineBase<OpmRegisters, StereoOutput, OpmRegisters.OpmOperatorMapping> fm,
            Variant variant
        )
        {
            _fm = fm ?? throw new ArgumentNullException(nameof(fm));
            m_variant = variant;
            _address = 0;
        }

        // internal state
        protected Variant m_variant; // chip variant
        protected byte _address; // address register

        protected readonly FmEngineBase<OpmRegisters, StereoOutput, OpmRegisters.OpmOperatorMapping> _fm;
        // core FM engine
    }
}
