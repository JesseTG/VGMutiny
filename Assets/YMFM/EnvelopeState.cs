using System;

namespace Ymfm
{
    // various envelope states
    public enum EnvelopeState : uint
    {
        Depress = 0, // OPLL only; set EG_HAS_DEPRESS to enable
        Attack = 1,
        Decay = 2,
        Sustain = 3,
        Release = 4,
        Reverb = 5, // OPQ/OPZ only; set EG_HAS_REVERB to enable
        States = 6,
    }
}
