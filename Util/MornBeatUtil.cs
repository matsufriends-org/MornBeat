using UnityEngine;

namespace MornBeat
{
    internal static class MornBeatUtil
    {
#if DISABLE_MORN_BEAT_LOG
        private const bool ShowLOG = false;
#else
        private const bool ShowLOG = true;
#endif
        private const string Prefix = "[<color=green>MornBeat</color>] ";
        internal const char OpenSplit = '[';
        internal const char CloseSplit = ']';

        internal static void Log(string message)
        {
            if (ShowLOG)
            {
                Debug.Log(Prefix + message);
            }
        }

        internal static void LogError(string message)
        {
            if (ShowLOG)
            {
                Debug.LogError(Prefix + message);
            }
        }

        internal static void LogWarning(string message)
        {
            if (ShowLOG)
            {
                Debug.LogWarning(Prefix + message);
            }
        }

        internal static double InverseLerp(double a, double b, double value)
        {
            var dif = b - a;
            return (value - a) / dif;
        }

        internal static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * t;
        }

        internal static bool BitHas(this int self, int flag)
        {
            return (self & flag) != 0;
        }

        internal static bool BitEqual(this int self, int flag)
        {
            return (self & flag) == flag;
        }

        internal static int BitAdd(this int self, int flag)
        {
            return self | flag;
        }

        internal static int BitRemove(this int self, int flag)
        {
            return self & ~flag;
        }

        internal static int BitXor(this int self, int flag)
        {
            return self ^ flag;
        }
    }
}