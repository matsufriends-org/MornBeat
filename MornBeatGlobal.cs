using MornGlobal;
using UnityEngine;

namespace MornBeat
{
    [CreateAssetMenu(fileName = nameof(MornBeatGlobal), menuName = "Morn/" + nameof(MornBeatGlobal))]

    internal sealed class MornBeatGlobal : MornGlobalBase<MornBeatGlobal>
    {
        protected override string ModuleName => nameof(MornBeat);
        
        public static void Log(string message)
        {
            I.LogInternal(message);
        }
        
        public static void LogWarning(string message)
        {
            I.LogWarningInternal(message);
        }
        
        public static void LogError(string message)
        {
            I.LogErrorInternal(message);
        }
    }
}