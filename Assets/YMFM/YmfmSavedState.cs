using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace Ymfm
{
    using uint8_t = System.Byte;
    using int8_t = System.SByte;
    using int16_t = System.Int16;
    using uint16_t = System.UInt16;
    using int32_t = System.Int32;
    using uint32_t = System.UInt32;

    // this class contains a managed vector of bytes that is used to save and
    // restore state
    public class YmfmSavedState
    {
        // internal state
        private readonly List<byte> _buffer;
        private int _offset;

        // construction
        public YmfmSavedState([NotNull] List<byte> buffer, bool saving)

        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));

            _offset = saving ? 1 : 0;

            if (saving)
            {
                buffer.Clear();
            }
        }

        // are we saving or restoring?
        public bool Saving => (_offset < 0);


        // save data to the buffer

        #region bool

        public void Save(bool data)
        {
            Write((byte)(data ? 1 : 0));
        }

        public void Restore(out bool data)
        {
            data = (Read() != 0);
        }

        public void SaveRestore(ref bool data)
        {
            if (Saving)
            {
                Save(data);
            }
            else
            {
                Restore(out data);
            }
        }

        #endregion bool

        public void Save(ReadOnlySpan<bool> data)
        {
            foreach (var b in data)
            {
                Save(b);
            }
        }

        public void Save(sbyte data)
        {
            Write((byte)data);
        }

        #region byte

        public void Save(byte data)
        {
            Write(data);
        }

        public void Restore(out byte data)
        {
            data = Read();
        }

        public void SaveRestore(ref byte data)
        {
            if (Saving)
            {
                Save(data);
            }
            else
            {
                Restore(out data);
            }
        }

        public void Save(ReadOnlySpan<byte> data)
        {
            foreach (var b in data)
            {
                Save(b);
            }
        }

        public void Restore(Span<byte> data)
        {
            for (var index = 0; index < data.Length; index++)
            {
                Restore(out data[index]);
            }
        }

        public void SaveRestore(Span<byte> data)
        {
            if (Saving)
            {
                Save(data);
            }
            else
            {
                Restore(data);
            }
        }

        #endregion byte

        #region short

        public void Save(short data)
        {
            Write((byte)(data)).Write((byte)(data >> 8));
        }


        public void Restore(out short data)
        {
            data = Read();
            data |= (short)(Read() << 8);
        }

        public void SaveRestore(ref short data)
        {
            if (Saving)
            {
                Save(data);
            }
            else
            {
                Restore(out data);
            }
        }

        #endregion short

        #region ushort

        public void Save(ushort data)
        {
            Write((byte)(data)).Write((byte)(data >> 8));
        }

        public void Restore(out ushort data)
        {
            data = Read();
            data |= (ushort)(Read() << 8);
        }

        public void SaveRestore(ref ushort data)
        {
            if (Saving)
            {
                Save(data);
            }
            else
            {
                Restore(out data);
            }
        }

        #endregion ushort

        public void Save(int data)
        {
            Write((byte)data).Write((byte)(data >> 8)).Write((byte)(data >> 16)).Write((byte)(data >> 24));
        }

        #region uint

        public void Save(uint data)
        {
            Write((byte)data).Write((byte)(data >> 8)).Write((byte)(data >> 16)).Write((byte)(data >> 24));
        }

        public void Restore(out uint data)
        {
            data = Read();
            data |= (uint)(Read() << 8);
            data |= (uint)(Read() << 16);
            data |= (uint)(Read() << 24);
        }

        public void SaveRestore(ref uint data)
        {
            if (Saving)
            {
                Save(data);
            }
            else
            {
                Restore(out data);
            }
        }

        #endregion

        #region EnvelopeState

        public void Save(EnvelopeState data)
        {
            Write((byte)(data));
        }

        public void Restore(out EnvelopeState data)
        {
            data = (EnvelopeState)(Read());
        }

        public void SaveRestore(ref EnvelopeState data)
        {
            if (Saving)
            {
                Save(data);
            }
            else
            {
                Restore(out data);
            }
        }

        #endregion EnvelopeState

        // restore data from the buffer


        public void Restore(out sbyte data)
        {
            data = (sbyte)Read();
        }


        public void Restore(out int data)
        {
            data = Read();
            data |= Read() << 8;
            data |= Read() << 16;
            data |= Read() << 24;
        }

        // internal helper
        public YmfmSavedState Write(byte data)
        {
            _buffer.Add(data);
            return this;
        }

        public byte Read()
        {
            return (byte)((_offset < _buffer.Count) ? _buffer[_offset++] : 0);
        }
    }
}
