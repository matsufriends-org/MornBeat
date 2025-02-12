using System.Threading;
using Cysharp.Threading.Tasks;
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

        internal async static UniTask LoadAudioDataAsync(this AudioClip clip, CancellationToken ct)
        {
            if (clip == null)
            {
                return;
            }

            if (clip.preloadAudioData)
            {
                Log($"ロード済み！: {clip.name}");
                return;
            }

            Log($"ロード開始...: {clip.name}");
            clip.LoadAudioData();
            while (clip.loadState != AudioDataLoadState.Loaded)
            {
                await UniTask.Yield(cancellationToken: ct);
            }

            Log($"ロード完了！: {clip.name}");
        }

        internal async static UniTask UnLoadAudioDataAsync(this AudioClip clip, CancellationToken ct)
        {
            if (clip == null)
            {
                return;
            }

            if (clip.preloadAudioData)
            {
                Log($"アンロード不要！: {clip.name}");
                return;
            }

            Log($"アンロード開始...: {clip.name}");
            clip.UnloadAudioData();
            while (clip.loadState != AudioDataLoadState.Unloaded)
            {
                await UniTask.Yield(cancellationToken: ct);
            }

            Log($"アンロード完了！: {clip.name}");
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