using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace MornBeat
{
    public struct MornBeatStartInfo
    {
        public MornBeatMemoSo BeatMemo;
        public IEnumerable<AudioClip> LoadWith;
        public IEnumerable<AudioClip> UnLoadWith;
        public double? StartDspTime;
        public float? FadeDuration;
        public bool? IsForceInitialize;
        public CancellationToken Ct;
        
    }
}