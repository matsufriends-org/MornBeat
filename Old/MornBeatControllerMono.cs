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
        [SerializeField] private MornBeatPlayModule _playModule;
        private readonly Subject<Unit> _beforeUpdateBeatSubject = new();
        private readonly Subject<Unit> _afterUpdateBeatSubject = new();
        public MornBeatPlayModule PlayModule => _playModule;
        public IObservable<Unit> OnBeforeUpdateBeat => _beforeUpdateBeatSubject;
        public IObservable<Unit> OnAfterUpdateBeat => _afterUpdateBeatSubject;
        
        private const double DefaultStartDspTimeOffset = 0.5d;

        private void Update()
        {
            _beforeUpdateBeatSubject.OnNext(Unit.Default);
            _playModule.UpdateBeat();
            _afterUpdateBeatSubject.OnNext(Unit.Default);
        }

        public async UniTask StartAsync(MornBeatStartInfo startInfo)
        {
            Assert.IsNotNull(startInfo.BeatMemo);
            var beatMemo = startInfo.BeatMemo;
            var startDspTime = startInfo.StartDspTime ?? AudioSettings.dspTime + DefaultStartDspTimeOffset;
            var fadeDuration = startInfo.FadeDuration ?? 0;
            var isForceInitialize = startInfo.IsForceInitialize ?? false;
            var ct = startInfo.Ct;
            if (_playModule.BeatMemo == beatMemo && isForceInitialize == false)
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

            _playModule.SetBeatMemo(new MornBeatSetInfo(beatMemo, startDspTime));
            var taskList = new List<UniTask>
            {
                prev.UnloadWithFadeOutAsync(next, fadeDuration, ct),
                next.PlayWithFadeIn(startDspTime, fadeDuration, ct),
            };
            await UniTask.WhenAll(taskList);
        }

        public async UniTask StopBeatAsync(float duration = 0, CancellationToken? ct = null)
        {
            ct ??= destroyCancellationToken;
            var current = _audioSourceModule.GetCurrent(true);
            await current.UnloadWithFadeOutAsync(null, duration, ct.Value);
            _playModule.Reset();
        }
    }
}