using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MornBeat
{
    public struct MornBeatStartInfo
    {
        public MornBeatMemoSo BeatMemo;
        public IReadOnlyList<AudioClip> LoadWith;
        public double? StartDspTime;
        public float? FadeDuration;
        public bool? IsForceInitialize;
        public CancellationToken Ct;
        
    }
}