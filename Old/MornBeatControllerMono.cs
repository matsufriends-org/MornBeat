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
        private double _pauseDspTime;
        private float _previousTimeScale;
        public MornBeatPlayModule PlayModule => _playModule;
        public IObservable<Unit> OnBeforeUpdateBeat => _beforeUpdateBeatSubject;
        public IObservable<Unit> OnAfterUpdateBeat => _afterUpdateBeatSubject;
        public bool IsPaused { get; private set; }
        private const double DefaultStartDspTimeOffset = 0.5d;

        private void Update()
        {
            if (IsPaused)
            {
                return;
            }

            _beforeUpdateBeatSubject.OnNext(Unit.Default);
            _playModule.UpdateBeat();
            _afterUpdateBeatSubject.OnNext(Unit.Default);
        }

        public async UniTask StartAsync(MornBeatStartInfo startInfo)
        {
            Assert.IsNotNull(startInfo.BeatMemo);
            var beatMemo = startInfo.BeatMemo;
            var isForceInitialize = startInfo.IsForceInitialize ?? false;
            if (_playModule.BeatMemo == beatMemo && isForceInitialize == false)
            {
                return;
            }
            
            var startDspTime = startInfo.StartDspTime ?? AudioSettings.dspTime + DefaultStartDspTimeOffset;
            var fadeDuration = startInfo.FadeDuration ?? 0;
            var ct = startInfo.Ct;

            IsPaused = false;
            var prev = _audioSourceModule.GetCurrent();
            var next = _audioSourceModule.GetOther(true);
            await next.LoadAsync(beatMemo.IntroClip, beatMemo.Clip, beatMemo.IsLoop, ct);
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

        /// <summary>音楽を一時停止する</summary>
        /// <param name="pauseTimeScale">Time.timeScaleを0にするかどうか</param>
        public void Pause(bool pauseTimeScale = true)
        {
            if (IsPaused)
            {
                MornBeatGlobal.LogWarning("既にポーズ中です");
                return;
            }

            IsPaused = true;
            _pauseDspTime = AudioSettings.dspTime;
            if (pauseTimeScale)
            {
                _previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                _previousTimeScale = -1f; // timeScaleを変更していないことを示すフラグ
            }

            // AudioSourceを一時停止
            var current = _audioSourceModule.GetCurrent();
            current.Pause();
        }

        /// <summary>音楽を再開する（カウントダウン対応）</summary>
        /// <param name="resumeDspTime">再開時のdspTime（カウントダウン等で調整可能）</param>
        /// <param name="onCountdownTick">カウントダウン中に呼ばれるコールバック（残り秒数を受け取る）</param>
        public async UniTask ResumeAsync(double resumeDspTime, Action<float> onCountdownTick,
            CancellationToken ct = default)
        {
            if (!IsPaused)
            {
                MornBeatGlobal.LogWarning("ポーズ中ではありません");
                return;
            }

            // カウントダウン処理
            if (onCountdownTick != null)
            {
                while (AudioSettings.dspTime < resumeDspTime)
                {
                    var remainingTime = (float)(resumeDspTime - AudioSettings.dspTime);
                    onCountdownTick(remainingTime);
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }

                // 最後に0秒を通知
                onCountdownTick(0f);
            }

            // ポーズ時間を計算して補正
            var pauseDuration = resumeDspTime - _pauseDspTime;
            _playModule.AddPauseDuration(pauseDuration);

            // Time.timeScaleを戻す（Pauseで変更していた場合のみ）
            if (_previousTimeScale >= 0f)
            {
                Time.timeScale = _previousTimeScale;
            }

            // AudioSourceを再開
            var current = _audioSourceModule.GetCurrent();
            current.UnPause();
            IsPaused = false;
        }
    }
}