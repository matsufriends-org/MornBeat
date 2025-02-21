using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

namespace MornBeat
{
    internal sealed class MornBeatIntroLoopAudioSource : MonoBehaviour
    {
        [SerializeField] private AudioSource _audioSourceIntro;
        [SerializeField] private AudioSource _audioSourceLoop;
        [SerializeField] private List<AudioClip> _loadWith;
        private CancellationTokenSource _cts;

        /// <summary> null可 </summary>
        public async UniTask LoadAsync(AudioClip introClip, AudioClip loopClip,
            IReadOnlyList<AudioClip> loadWith = null, CancellationToken ct = default)
        {
            // Unload忘れを防ぐ
            Assert.IsTrue(
                _audioSourceIntro.clip == null || _audioSourceIntro.clip.loadState == AudioDataLoadState.Unloaded);
            Assert.IsTrue(
                _audioSourceLoop.clip == null || _audioSourceLoop.clip.loadState == AudioDataLoadState.Unloaded);
            foreach (var clip in _loadWith)
            {
                Assert.IsTrue(clip == null || clip.loadState == AudioDataLoadState.Unloaded);
            }

            _loadWith.Clear();
            if (loadWith != null)
            {
                _loadWith.AddRange(loadWith);
            }

            _audioSourceIntro.clip = introClip;
            _audioSourceIntro.loop = false;
            _audioSourceLoop.clip = loopClip;
            _audioSourceLoop.loop = true;
            var taskList = new List<UniTask>();
            taskList.Add(introClip.LoadAsync(ct));
            taskList.Add(loopClip.LoadAsync(ct));
            foreach (var clip in _loadWith)
            {
                taskList.Add(clip.LoadAsync(ct));
            }

            await UniTask.WhenAll(taskList);
        }

        public async UniTask PlayWithFadeIn(double startDspTime, float duration = 0, CancellationToken ct = default)
        {
            // Load忘れを防ぐ
            Assert.IsTrue(
                _audioSourceIntro.clip == null || _audioSourceIntro.clip.loadState == AudioDataLoadState.Loaded);
            Assert.IsTrue(
                _audioSourceLoop.clip == null || _audioSourceLoop.clip.loadState == AudioDataLoadState.Loaded);
            foreach (var clip in _loadWith)
            {
                Assert.IsTrue(clip == null || clip.loadState == AudioDataLoadState.Loaded);
            }

            if (_audioSourceIntro.clip != null)
            {
                // イントロ込みで再生
                _audioSourceIntro.PlayScheduled(startDspTime);
                _audioSourceLoop.PlayScheduled(startDspTime + _audioSourceIntro.clip.length);
            }
            else if (_audioSourceLoop.clip != null)
            {
                // イントロ無しで再生
                _audioSourceLoop.PlayScheduled(startDspTime);
            }

            while (AudioSettings.dspTime < startDspTime)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            await FadeInAsync(duration, ct);
        }

        public async UniTask UnloadWithFadeOutAsync(float duration, CancellationToken ct = default)
        {
            await FadeOutAsync(duration, ct);
            var taskList = new List<UniTask>();
            taskList.Add(_audioSourceIntro.clip.UnloadAsync(ct));
            taskList.Add(_audioSourceLoop.clip.UnloadAsync(ct));
            foreach (var clip in _loadWith)
            {
                taskList.Add(clip.UnloadAsync(ct));
            }

            await UniTask.WhenAll(taskList);
        }

        public async UniTask FadeInAsync(float duration, CancellationToken ct = default)
        {
            await FadeAsync(duration, true, ct);
        }

        public async UniTask FadeOutAsync(float duration, CancellationToken ct = default)
        {
            await FadeAsync(duration, false, ct);
        }

        private async UniTask FadeAsync(float duration, bool isIn, CancellationToken ct = default)
        {
            var aimValue = isIn ? 1 : 0;
            _cts?.Cancel();
            if (duration == 0)
            {
                _cts = null;
                _audioSourceIntro.volume = aimValue;
                _audioSourceLoop.volume = aimValue;
                if (!isIn)
                {
                    _audioSourceIntro.Stop();
                    _audioSourceLoop.Stop();
                }
                return;
            }

            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            var startVolumeI = _audioSourceIntro.volume;
            var startVolumeL = _audioSourceLoop.volume;
            var averageDif = (Mathf.Abs(startVolumeI - aimValue) + Mathf.Abs(startVolumeL - aimValue)) / 2f;
            duration *= averageDif;
            var startTime = Time.time;
            while (Time.time - startTime < duration)
            {
                if (_cts.Token.IsCancellationRequested)
                {
                    return;
                }

                var rate = (Time.time - startTime) / duration;
                _audioSourceIntro.volume = Mathf.Lerp(startVolumeI, aimValue, rate);
                _audioSourceLoop.volume = Mathf.Lerp(startVolumeL, aimValue, rate);
                await UniTask.Yield(PlayerLoopTiming.Update, ct);
            }

            _audioSourceIntro.volume = aimValue;
            _audioSourceLoop.volume = aimValue;
            if (!isIn)
            {
                _audioSourceIntro.Stop();
                _audioSourceLoop.Stop();
            }   
        }
    }
}