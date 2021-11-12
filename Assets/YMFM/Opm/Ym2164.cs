namespace Ymfm.Opm
{
    // the YM2164 is almost 100% functionally identical to the YM2151, except
    // it apparently has some mystery registers in the 00-07 range, and timer
    // B's frequency is half that of the 2151
    public sealed class Ym2164 : Ym2151
    {
        public Ym2164(IYmfmInterface @interface) : base(@interface, Variant.Ym2164)
        {
        }
    }
}
