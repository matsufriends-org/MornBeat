using System;
using UniRx;
using UnityEngine;

namespace MornBeat
{
    [Serializable]
    public class MornBeatTimingSolver
    {
        [SerializeField] [ReadOnly] private double _currentBpm = 120;
        [SerializeField] [ReadOnly] private int _tick;
        [SerializeField] [ReadOnly] private bool _waitLoop;
        [SerializeField] [ReadOnly] private double _loopStartDspTime;
        [SerializeField] [ReadOnly] private double _startDspTime;
        [SerializeField] [ReadOnly] private double _offsetTime;
        private readonly Subject<MornBeatTimingInfo> _beatSubject = new();
        private readonly Subject<Unit> _loopSubject = new();
        private readonly Subject<Unit> _endBeatSubject = new();
        public float SpeedScale => (float)_currentBpm / 120f;
        public double CurrentBpm => _currentBpm;
        public float BeatLengthF => (float)(60d / CurrentBpm);
        public double CurrentBeatLength => 60d / CurrentBpm;
        public double StartDspTime => _startDspTime;
        /// <summary> ループ時に0から初期化（単位：秒）</summary>
        public double MusicPlayingTime => AudioSettings.dspTime - _loopStartDspTime + _offsetTime;
        /// <summary> ループ後に値を継続（単位：秒）</summary>
        public double MusicPlayingTimeNoRepeat => AudioSettings.dspTime - _startDspTime + _offsetTime;
        /// <summary> ループ時に0から初期化（単位：拍）</summary>
        public double MusicBeatTime => MusicPlayingTime / CurrentBeatLength;
        /// <summary> ループ後に値を継続（単位：拍）</summary>
        public double MusicBeatTimeNoRepeat => MusicPlayingTimeNoRepeat / CurrentBeatLength;
        public double OffsetTime => _offsetTime;
        public IObservable<MornBeatTimingInfo> OnBeat => _beatSubject;
        public IObservable<Unit> OnLoop => _loopSubject;
        public IObservable<Unit> OnEndBeat => _endBeatSubject;

        internal void SetBeatMemo(MornBeatSetInfo setInfo)
        {
            _tick = 0;
            _waitLoop = false;
            _startDspTime = setInfo.StartDspTime;
            _loopStartDspTime = _startDspTime;
            _currentBpm = setInfo.BeatMemo.GetBpm(0);
        }

        internal void Reset()
        {
            _tick = 0;
            _waitLoop = false;
            _startDspTime = AudioSettings.dspTime;
            _loopStartDspTime = _startDspTime;
            _currentBpm = 120;
        }

        internal void SetOffsetTime(double offsetTime)
        {
            _offsetTime = offsetTime;
        }

        internal void UpdateBeat(MornBeatMemoSo beatMemo)
        {
            var time = MusicPlayingTime;
            if (_waitLoop)
            {
                if (time >= beatMemo.TotalLength)
                {
                    _loopStartDspTime += beatMemo.LoopLength;
                    time -= beatMemo.LoopLength;
                    _loopSubject.OnNext(Unit.Default);
                    _waitLoop = false;
                }
                else
                {
                    return;
                }
            }

            if (time < beatMemo.GetBeatTiming(_tick))
            {
                return;
            }

            _currentBpm = beatMemo.GetBpm(time);
            _beatSubject.OnNext(new MornBeatTimingInfo(_tick, beatMemo.MeasureTickCount));
            _tick++;
            if (_tick == beatMemo.TotalTickSum)
            {
                if (beatMemo.IsLoop)
                {
                    _tick = beatMemo.IntroTickSum;
                }

                _waitLoop = true;
                _endBeatSubject.OnNext(Unit.Default);
            }
        }

        internal int GetNearTick(MornBeatMemoSo beatMemo, out double nearDif)
        {
            var preTick = _tick;
            var nexTick = preTick + 1;
            var preTime = beatMemo.GetBeatTiming(preTick);
            var nexTime = beatMemo.GetBeatTiming(nexTick);
            var curTime = MusicPlayingTime;

            // preTimeが現在時刻より手前に来るよう調整する
            while (curTime < preTime && preTick - 1 >= 0)
            {
                preTick -= 1;
                nexTick -= 1;
                preTime = beatMemo.GetBeatTiming(preTick);
                nexTime = beatMemo.GetBeatTiming(nexTick);
            }

            // nexTimeが現在時刻より後に来るよう調整する
            while (nexTime < curTime && nexTick + 1 < beatMemo.TotalTickSum)
            {
                preTick += 1;
                nexTick += 1;
                preTime = beatMemo.GetBeatTiming(preTick);
                nexTime = beatMemo.GetBeatTiming(nexTick);
            }

            var prevIsCloser = curTime < (preTime + nexTime) / 2f;
            var aimTime = prevIsCloser ? preTime : nexTime;
            var aimTick = prevIsCloser ? preTick : nexTick;
            nearDif = aimTime - curTime;
            return aimTick;
        }
    }
}