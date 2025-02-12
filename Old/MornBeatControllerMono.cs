using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using UnityEngine.Assertions;

namespace MornBeat
{
    public sealed class MornBeatControllerMono : MonoBehaviour
    {
        private const double PlayStartOffset = 0.5d;
        [SerializeField] private AudioSource _introAudioSource;
        [SerializeField] private AudioSource _loopAudioSource;
        [SerializeField] [ReadOnly] private MornBeatMemoSo _currentBeatMemo;
        [SerializeField] [ReadOnly] private int _tick;
        [SerializeField] [ReadOnly] private bool _waitLoop;
        [SerializeField] [ReadOnly] private double _loopStartDspTime;
        [SerializeField] [ReadOnly] private double _startDspTime;
        [SerializeField] [ReadOnly] private double _offsetTime;
        private bool _isLoading;
        private Subject<MornBeatTimingInfo> _beatSubject = new();
        private Subject<Unit> _endBeatSubject = new();
        private Subject<MornBeatMemoSo> _initializeBeatSubject = new();
        private Subject<Unit> _updateBeatSubject = new();
        public IObservable<MornBeatTimingInfo> OnBeat => _beatSubject;
        public IObservable<MornBeatMemoSo> OnInitializeBeat => _initializeBeatSubject;
        public IObservable<Unit> OnEndBeat => _endBeatSubject;
        public IObservable<Unit> OnUpdateBeat => _updateBeatSubject;
        public double CurrentBpm { get; private set; } = 120;
        public int MeasureTickCount => _currentBeatMemo.MeasureTickCount;
        public int BeatCount => _currentBeatMemo.BeatCount;
        public int BeatTick => MeasureTickCount / BeatCount;
        public double CurrentBeatLength => 60d / CurrentBpm;
        public double StartDspTime => _startDspTime;
        /// <summary> ループ時に0から初期化 </summary>
        public double MusicPlayingTime => AudioSettings.dspTime
                                          - _loopStartDspTime
                                          + (_currentBeatMemo != null ? _currentBeatMemo.Offset : 0)
                                          + _offsetTime;
        /// <summary> ループ後に値を継続 </summary>
        public double MusicPlayingTimeNoRepeat => AudioSettings.dspTime
                                                  - _startDspTime
                                                  + (_currentBeatMemo != null ? _currentBeatMemo.Offset : 0)
                                                  + _offsetTime;
        public double MusicBeatTime => MusicPlayingTime / CurrentBeatLength;
        public double MusicBeatTimeNoRepeat => MusicPlayingTimeNoRepeat / CurrentBeatLength;
        public MornBeatMemoSo CurrentBeatMemo => _currentBeatMemo;

        private void Update()
        {
            UpdateBeatInternal();
            _updateBeatSubject.OnNext(Unit.Default);
        }

        public void ChangeOffset(double offset)
        {
            _offsetTime = offset;
        }

        public void ResetBeat()
        {
            _currentBeatMemo = null;
            _tick = 0;
            CurrentBpm = 120;
            _waitLoop = false;
            _startDspTime = AudioSettings.dspTime;
            _loopStartDspTime = _startDspTime;
            _beatSubject = new Subject<MornBeatTimingInfo>();
            _initializeBeatSubject = new Subject<MornBeatMemoSo>();
            _endBeatSubject = new Subject<Unit>();
            _updateBeatSubject = new Subject<Unit>();
            _introAudioSource.Stop();
            _loopAudioSource.Stop();
        }

        public float GetBeatTiming(int tick)
        {
            if (_currentBeatMemo == null)
                return Mathf.Infinity;
            return _currentBeatMemo.GetBeatTiming(tick);
        }

        private void UpdateBeatInternal()
        {
            if (_currentBeatMemo == null)
                return;
            var time = MusicPlayingTime;
            if (_waitLoop)
            {
                if (time < _currentBeatMemo.TotalLength)
                    return;
                _loopStartDspTime += _currentBeatMemo.LoopLength;
                time -= _currentBeatMemo.LoopLength;
                _waitLoop = false;
            }

            if (time < _currentBeatMemo.GetBeatTiming(_tick))
                return;
            CurrentBpm = _currentBeatMemo.GetBpm(time);
            _beatSubject.OnNext(new MornBeatTimingInfo(_tick, _currentBeatMemo.MeasureTickCount));
            _tick++;
            if (_tick == _currentBeatMemo.TickSum)
            {
                if (_currentBeatMemo.IsLoop)
                    _tick = _currentBeatMemo.IntroTickSum;
                _waitLoop = true;
                _endBeatSubject.OnNext(Unit.Default);
            }
        }

        public async UniTask InitializeBeatAsync(MornBeatMemoSo beatMemo, bool isForceInitialize = false, CancellationToken ct = default)
        {
            if (_currentBeatMemo == beatMemo && isForceInitialize == false)
            {
                return;
            }
            _isLoading = true;
            var taskList = new List<UniTask>();
            if (_currentBeatMemo != null)
            {
                _introAudioSource.Stop();
                _loopAudioSource.Stop();
                taskList.Add(_currentBeatMemo.IntroClip.UnLoadAudioDataAsync(ct));
                taskList.Add(_currentBeatMemo.Clip.UnLoadAudioDataAsync(ct));
            }

            taskList.Add(beatMemo.IntroClip.LoadAudioDataAsync(ct));
            taskList.Add(beatMemo.Clip.LoadAudioDataAsync(ct));

            await UniTask.WhenAll(taskList).SuppressCancellationThrow();

            _tick = 0;
            _waitLoop = false;
            _startDspTime = AudioSettings.dspTime + PlayStartOffset;
            _loopStartDspTime = _startDspTime;
            _introAudioSource.loop = false;
            _loopAudioSource.loop = beatMemo.IsLoop;
            _introAudioSource.clip = beatMemo.IntroClip;
            _loopAudioSource.clip = beatMemo.Clip;
            _introAudioSource.volume = beatMemo.Volume;
            _loopAudioSource.volume = beatMemo.Volume;
            _introAudioSource.PlayScheduled(_startDspTime);
            _loopAudioSource.PlayScheduled(_startDspTime + beatMemo.IntroLength);
            _currentBeatMemo = beatMemo;
            _initializeBeatSubject.OnNext(beatMemo);
        }
        
        public int GetNearTick(out double nearDif)
        {
            return GetNearTickBySpecifiedBeat(out nearDif, _currentBeatMemo.MeasureTickCount);
        }

        public int GetNearTickBySpecifiedBeat(out double nearDif, int beat)
        {
            Assert.IsTrue(beat <= _currentBeatMemo.MeasureTickCount);
            var tickSize = _currentBeatMemo.MeasureTickCount / beat;
            var lastTick = _tick - _tick % tickSize;
            var nextTick = lastTick + tickSize;
            var curTime = MusicPlayingTime;
            var preTime = GetBeatTiming(lastTick);
            var nexTime = GetBeatTiming(nextTick);
            while (curTime < preTime && lastTick - tickSize >= 0)
            {
                lastTick -= tickSize;
                nextTick -= tickSize;
                preTime = GetBeatTiming(lastTick);
                nexTime = GetBeatTiming(nextTick);
            }

            while (nexTime < curTime && nextTick + tickSize < _currentBeatMemo.TickSum)
            {
                lastTick += tickSize;
                nextTick += tickSize;
                preTime = GetBeatTiming(lastTick);
                nexTime = GetBeatTiming(nextTick);
            }

            if (curTime < (preTime + nexTime) / 2f)
            {
                nearDif = preTime - curTime;
                return lastTick;
            }

            nearDif = nexTime - curTime;
            return nextTick;
        }
    }
}