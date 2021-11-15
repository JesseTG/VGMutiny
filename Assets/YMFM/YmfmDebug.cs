using System.Diagnostics;

namespace Ymfm
{
    public static class YmfmDebug
    {
        // masks to help isolate specific channels
        public const uint GLOBAL_FM_CHANNEL_MASK = 0xffffffffu;
        public const uint GLOBAL_ADPCM_A_CHANNEL_MASK = 0xffffffff;
        public const uint GLOBAL_ADPCM_B_CHANNEL_MASK = 0xffffffff;
        public const uint GLOBAL_PCM_CHANNEL_MASK = 0xffffffff;

        // helpers to write based on the log type
        [Conditional("LOG_FM_WRITES")]
        public static void LogFmWrite(string format, params object[] args)
        {
            Log(format, args);
        }

        [Conditional("LOG_KEYON_EVENTS")]
        public static void LogKeyOn(string format, params object[] args)
        {
            Log(format, args);
        }

        [Conditional("LOG_UNEXPECTED_READ_WRITES")]
        public static void LogUnexpectedReadWrite(string format, params object[] args)
        {
            Log(format, args);
        }

        // downstream helper to output log data; defaults to printf
        public static void Log(string format, params object[] args)
        {
            UnityEngine.Debug.LogFormat(format, args);
        }
    }
}
