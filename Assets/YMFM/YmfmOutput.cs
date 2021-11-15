using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Ymfm
{
    public interface IOutput
    {
        // clear all outputs to 0
        public void Clear();

        // clamp all outputs to a 16-bit signed value
        public void Clamp16();

        // run each output value through the floating-point processor
        public void RoundTripFp();

        public int NumOutputs { get; }

        public int this[int i] { get; set; }
    }

// struct containing an array of output values
    public struct MonoOutput : IOutput
    {
        private int _data;

        // clear all outputs to 0
        public void Clear()
        {
            _data = 0;
        }

        // clamp all outputs to a 16-bit signed value
        public void Clamp16()
        {
            _data = Mathf.Clamp(_data, short.MinValue, short.MaxValue);
        }

        // run each output value through the floating-point processor
        public void RoundTripFp()
        {
            _data = Utils.RoundtripFp(_data);
        }

        public int NumOutputs => 1;

        public int this[int i]
        {
            readonly get
            {
                if (i == 0)
                {
                    return _data;
                }

                throw new ArgumentOutOfRangeException(nameof(i));
            }
            set
            {
                if (i == 0)
                {
                    _data = value;
                }

                throw new ArgumentOutOfRangeException(nameof(i));
            }
        }
    }

    public struct StereoOutput : IOutput
    {
        private int _data0;
        private int _data1;

        // clear all outputs to 0
        public void Clear()
        {
            _data0 = 0;
            _data1 = 0;
        }

        // clamp all outputs to a 16-bit signed value
        public void Clamp16()
        {
            _data0 = Mathf.Clamp(_data0, short.MinValue, short.MaxValue);
            _data1 = Mathf.Clamp(_data1, short.MinValue, short.MaxValue);
        }

        // run each output value through the floating-point processor
        public void RoundTripFp()
        {
            _data0 = Utils.RoundtripFp(_data0);
            _data1 = Utils.RoundtripFp(_data1);
        }

        public int NumOutputs => 2;

        public int this[int i]
        {
            readonly get
            {
                return i switch
                {
                    0 => _data0,
                    1 => _data1,
                    _ => throw new ArgumentOutOfRangeException(nameof(i)),
                };
            }
            set
            {
                switch (i)
                {
                    case 0:
                        _data0 = value;
                        break;
                    case 1:
                        _data1 = value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(i));
                }
            }
        }

        [Pure]
        public override readonly string ToString()
        {
            return $"({_data0}, {_data1})";
        }
    }
}
