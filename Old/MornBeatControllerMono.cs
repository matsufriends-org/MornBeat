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
        [SerializeField] private MornBeatAudioSourceModule _audioSourceModule;
        [SerializeField] [ReadOnly] private MornBeatMemoSo _beatMemo;
        [SerializeField] [ReadOnly] private int _tick;
        [SerializeField] [ReadOnly] private bool _waitLoop;
        [SerializeField] [ReadOnly] private double _loopStartDspTime;
        [SerializeField] [ReadOnly] private double _startDspTime;
        [SerializeField] [ReadOnly] private double _offsetTime;
        private readonly Subject<MornBeatTimingInfo> _beatSubject = new();
        private readonly Subject<Unit> _endBeatSubject = new();
        private readonly Subject<double> _setDspTimeSubject = new();
        private readonly Subject<MornBeatMemoSo> _initializeBeatSubject = new();
        private readonly Subject<Unit> _updateBeatSubject = new();
        public IObservable<MornBeatTimingInfo> OnBeat => _beatSubject;
        public IObservable<MornBeatMemoSo> OnInitializeBeat => _initializeBeatSubject;
        public IObservable<Unit> OnEndBeat => _endBeatSubject;
        public IObservable<double> OnSetDspTime => _setDspTimeSubject;
        public IObservable<Unit> OnUpdateBeat => _updateBeatSubject;
        public double CurrentBpm { get; private set; } = 120;
        public int MeasureTickCount => _beatMemo.MeasureTickCount;
        public int BeatCount => _beatMemo.BeatCount;
        public int BeatTick => MeasureTickCount / BeatCount;
        public double CurrentBeatLength => 60d / CurrentBpm;
        public double StartDspTime => _startDspTime;
        /// <summary> ループ時に0から初期化 </summary>
        public double MusicPlayingTime => AudioSettings.dspTime
                                          - _loopStartDspTime
                                          + (_beatMemo != null ? _beatMemo.Offset : 0)
                                          + _offsetTime;
        /// <summary> ループ後に値を継続 </summary>
        public double MusicPlayingTimeNoRepeat => AudioSettings.dspTime
                                                  - _startDspTime
                                                  + (_beatMemo != null ? _beatMemo.Offset : 0)
                                                  + _offsetTime;
        public double MusicBeatTime => MusicPlayingTime / CurrentBeatLength;
        public double MusicBeatTimeNoRepeat => MusicPlayingTimeNoRepeat / CurrentBeatLength;
        public MornBeatMemoSo BeatMemo => _beatMemo;
        public double OffsetTime
        {
            get => _offsetTime;
            set => _offsetTime = value;
        }
        
        private const double DefaultStartDspTimeOffset = 0.5d;


        private void Update()
        {
            UpdateBeatInternal();
            _updateBeatSubject.OnNext(Unit.Default);
        }
        
        public async UniTask StartAsync(MornBeatStartInfo startInfo)
        {
            Assert.IsNotNull(startInfo.BeatMemo);
            var beatMemo = startInfo.BeatMemo;
            var startDspTime = startInfo.StartDspTime ?? AudioSettings.dspTime + DefaultStartDspTimeOffset;
            var fadeDuration = startInfo.FadeDuration ?? 0;
            var isForceInitialize = startInfo.IsForceInitialize ?? false;
            var ct = startInfo.Ct;
            if (_beatMemo == beatMemo && isForceInitialize == false)
            {
                return;
            }

            var prev = _audioSourceModule.GetCurrent();
            var next = _audioSourceModule.GetOther(true);
            
            await next.LoadAsync(beatMemo.IntroClip, beatMemo.Clip, ct);

            if (startDspTime < AudioSettings.dspTime)
            {
                var cached = startDspTime;
                startDspTime = AudioSettings.dspTime + DefaultStartDspTimeOffset;
                MornBeatGlobal.LogError($"再生時刻が過去のため補正します。[{cached} -> {startDspTime}]");
            }
            
            _beatMemo = beatMemo;
            _tick = 0;
            _waitLoop = false;
            _startDspTime = startDspTime;
            _loopStartDspTime = _startDspTime;
            _initializeBeatSubject.OnNext(beatMemo);
            _setDspTimeSubject.OnNext(startDspTime);
            var taskList = new List<UniTask>
            {
                prev.UnloadWithFadeOutAsync(next, fadeDuration, ct),
                next.PlayWithFadeIn(startDspTime, fadeDuration, ct),
            };
            await UniTask.WhenAll(taskList).SuppressCancellationThrow();
        }
        
        public async UniTask StopBeatAsync(float duration = 0, CancellationToken ct = default)
        {
            var current = _audioSourceModule.GetCurrent(true);
            await current.UnloadWithFadeOutAsync(null, duration, ct);
            _tick = 0;
            CurrentBpm = 120;
            _beatMemo = null;
            _waitLoop = false;
            _startDspTime = AudioSettings.dspTime;
            _loopStartDspTime = _startDspTime;
        }

        private void UpdateBeatInternal()
        {
            if (_beatMemo == null)
            {
                return;
            }

            var time = MusicPlayingTime;
            if (_waitLoop)
            {
                if (time < _beatMemo.TotalLength)
                {
                    return;
                }

                _loopStartDspTime += _beatMemo.LoopLength;
                time -= _beatMemo.LoopLength;
                _waitLoop = false;
            }

            if (time < _beatMemo.GetBeatTiming(_tick))
            {
                return;
            }

            CurrentBpm = _beatMemo.GetBpm(time);
            _beatSubject.OnNext(new MornBeatTimingInfo(_tick, _beatMemo.MeasureTickCount));
            _tick++;
            if (_tick == _beatMemo.TotalTickSum)
            {
                if (_beatMemo.IsLoop)
                {
                    _tick = _beatMemo.IntroTickSum;
                }

                _waitLoop = true;
                _endBeatSubject.OnNext(Unit.Default);
            }
        }
        
        public float GetBeatTiming(int tick)
        {
            if (_beatMemo == null)
            {
                return Mathf.Infinity;
            }

            return _beatMemo.GetBeatTiming(tick);
        }

        public int GetNearTick(out double nearDif)
        {
            return GetNearTickBySpecifiedBeat(out nearDif, _beatMemo.MeasureTickCount);
        }

        public int GetNearTickBySpecifiedBeat(out double nearDif, int beat)
        {
            Assert.IsTrue(beat <= _beatMemo.MeasureTickCount);
            var tickSize = _beatMemo.MeasureTickCount / beat;
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

            while (nexTime < curTime && nextTick + tickSize < _beatMemo.TotalTickSum)
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