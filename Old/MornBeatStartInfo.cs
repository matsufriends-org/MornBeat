using System.Threading;

namespace MornBeat
{
    public struct MornBeatStartInfo
    {
        public MornBeatMemoSo BeatMemo;
        public double? StartDspTime;
        public float? FadeDuration;
        public bool? IsForceInitialize;
        public CancellationToken Ct;
        
    }
}