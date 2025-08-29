using System;
using UniRx;
using UnityEngine;

namespace MornBeat
{
    [Serializable]
    public class MornBeatPlayModule
    {
        [SerializeField] [ReadOnly] private MornBeatMemoSo _beatMemo;
        [SerializeField] [ReadOnly] private MornBeatTimingSolver _offsetTiming;
        [SerializeField] [ReadOnly] private MornBeatTimingSolver _pureTiming;
        [SerializeField] [ReadOnly] private double _pausingTime;
        [SerializeField] [ReadOnly] private double _pauseOffset;
        private readonly Subject<MornBeatSetInfo> _initializeBeatSubject = new();
        public MornBeatMemoSo BeatMemo => _beatMemo;
        public float SpeedScale => _offsetTiming.SpeedScale;
        public double CurrentBpm => _offsetTiming.CurrentBpm;
        public float BeatLengthF => _offsetTiming.BeatLengthF;
        public double CurrentBeatLength => _offsetTiming.CurrentBeatLength;
        public double StartDspTime => _offsetTiming.StartDspTime;
        public int BeatCount => _beatMemo?.BeatCount ?? 0;
        public int BeatTick => _beatMemo?.BeatTick ?? 0;
        public int MeasureTick => _beatMemo?.MeasureTickCount ?? 0;
        /// <summary> ループ時に0から初期化（単位：秒）</summary>
        public double MusicPlayingTime => _offsetTiming.MusicPlayingTime
                                          + (_beatMemo != null ? _beatMemo.Offset : 0)
                                          - _pauseOffset
                                          - _pausingTime;
        /// <summary> ループ後に値を継続（単位：秒）</summary>
        public double MusicPlayingTimeNoRepeat => _offsetTiming.MusicPlayingTimeNoRepeat
                                                  + (_beatMemo != null ? _beatMemo.Offset : 0)
                                                  - _pauseOffset
                                                  - _pausingTime;
        /// <summary> ループ時に0から初期化（単位：拍）</summary>
        public double MusicBeatTime => _offsetTiming.MusicBeatTime;
        /// <summary> ループ後に値を継続（単位：拍）</summary>
        public double MusicBeatTimeNoRepeat => _offsetTiming.MusicBeatTimeNoRepeat;
        public double OffsetTime => _offsetTiming.OffsetTime;
        public IObservable<MornBeatSetInfo> OnInitializeBeat => _initializeBeatSubject;
        public IObservable<MornBeatTimingInfo> OnBeat => _offsetTiming.OnBeat;
        public IObservable<MornBeatTimingInfo> OnPureBeat => _pureTiming.OnBeat;
        public IObservable<Unit> OnLoop => _offsetTiming.OnLoop;
        public IObservable<Unit> OnEndBeat => _offsetTiming.OnEndBeat;

        internal void SetBeatMemo(MornBeatSetInfo setInfo)
        {
            _beatMemo = setInfo.BeatMemo;
            _offsetTiming.SetBeatMemo(setInfo);
            _pureTiming.SetBeatMemo(setInfo);
            _pauseOffset = 0;
            _pausingTime = 0;
            _initializeBeatSubject.OnNext(setInfo);
        }

        internal void Reset()
        {
            _beatMemo = null;
            _offsetTiming.Reset();
            _pureTiming.Reset();
            _pauseOffset = 0;
            _pausingTime = 0;
        }

        internal void UpdateBeat()
        {
            if (_beatMemo == null)
            {
                return;
            }

            _offsetTiming.UpdateBeat(_beatMemo);
            _pureTiming.UpdateBeat(_beatMemo);
        }

        public void SetOffsetTime(double offsetTime)
        {
            _offsetTiming.SetOffsetTime(offsetTime);
            // _pureTiming.SetOffsetTime(offsetTime);
        }

        public float ConvertToTime(int tick)
        {
            if (_beatMemo == null)
            {
                return Mathf.Infinity;
            }

            return _beatMemo.GetBeatTiming(tick);
        }

        public int GetNearTick(out double nearDif)
        {
            if (_beatMemo == null)
            {
                nearDif = double.MaxValue;
                return -1;
            }

            return _offsetTiming.GetNearTick(_beatMemo, out nearDif);
        }

        /// <summary>ポーズ時間を追加する</summary>
        internal void UpdatePausing(double pausingTime)
        {
            _pausingTime = pausingTime;
        }

        /// <summary>ポーズ時間を追加する</summary>
        internal void EndPausing(double pausingTime)
        {
            _pausingTime = 0;
            _pauseOffset += pausingTime;
        }
    }
}