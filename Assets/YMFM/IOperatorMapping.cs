using System;

namespace Ymfm
{
    public interface IOperatorMapping
    {
        public Span<uint> OperatorIndexes { get; }
    }
}
