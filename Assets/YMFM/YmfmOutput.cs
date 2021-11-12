using System;
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

        public Span<int> Data { get; }

        public int NumOutputs { get; }
    }

// struct containing an array of output values
    public unsafe struct MonoOutput : IOutput
    {
        private int _data;

        public readonly Span<int> Data
        {
            get
            {
                fixed (int* i = &_data)
                {
                    return new Span<int>(i, 1);
                }
            }
        }

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
    }

    public unsafe struct StereoOutput : IOutput
    {
        private fixed int _data[2];

        public readonly Span<int> Data
        {
            get
            {
                fixed (int* i = _data)
                {
                    return new Span<int>(i, 2);
                }
            }
        }

        // clear all outputs to 0
        public void Clear()
        {
            _data[0] = 0;
            _data[1] = 0;
        }

        // clamp all outputs to a 16-bit signed value
        public void Clamp16()
        {
            _data[0] = Mathf.Clamp(_data[0], short.MinValue, short.MaxValue);
            _data[1] = Mathf.Clamp(_data[1], short.MinValue, short.MaxValue);
        }

        // run each output value through the floating-point processor
        public void RoundTripFp()
        {
            _data[0] = Utils.RoundtripFp(_data[0]);
            _data[1] = Utils.RoundtripFp(_data[1]);
        }

        public int NumOutputs => 2;
    }
}
