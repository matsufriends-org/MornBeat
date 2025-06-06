using MornGlobal;
using UnityEngine;

namespace MornBeat
{
    internal sealed class MornBeatGlobal : MornGlobalPureBase<MornBeatGlobal>
    {
        protected override string ModuleName => nameof(MornBeat);

        internal static void Log(string message)
        {
            I.LogInternal(message);
        }

        internal static void LogWarning(string message)
        {
            I.LogWarningInternal(message);
        }

        internal static void LogError(string message)
        {
            I.LogErrorInternal(message);
        }

        internal static void SetDirty(Object obj)
        {
            I.SetDirtyInternal(obj);
        }

        internal static void LogAndSetDirty(string message, Object obj)
        {
            Log(message);
            SetDirty(obj);
        }
    }
}