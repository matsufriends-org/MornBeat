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
        private CancellationTokenSource _cts;

        private bool Contain(AudioClip clip)
        {
            if (clip == null)
            {
                return false;
            }
            
            if (_audioSourceIntro.clip == clip)
            {
                return true;
            }
            
            if (_audioSourceLoop.clip == clip)
            {
                return true;
            }

            return false;
        }
        
        /// <summary> null可 </summary>
        public async UniTask LoadAsync(AudioClip introClip, AudioClip loopClip, CancellationToken ct = default)
        {
            _audioSourceIntro.clip = introClip;
            _audioSourceIntro.loop = false;
            _audioSourceLoop.clip = loopClip;
            _audioSourceLoop.loop = true;
            var taskList = new List<UniTask>();
            taskList.Add(introClip.LoadAsync(ct));
            taskList.Add(loopClip.LoadAsync(ct));
            await UniTask.WhenAll(taskList);
        }

        public async UniTask PlayWithFadeIn(double startDspTime, float duration = 0, CancellationToken ct = default)
        {
            // Load忘れを防ぐ
            Assert.IsTrue(
                _audioSourceIntro.clip == null || _audioSourceIntro.clip.loadState == AudioDataLoadState.Loaded);
            Assert.IsTrue(
                _audioSourceLoop.clip == null || _audioSourceLoop.clip.loadState == AudioDataLoadState.Loaded);

            if (startDspTime < AudioSettings.dspTime)
            {
                MornBeatGlobal.LogError($"再生時刻が過去です。startDspTime: {startDspTime}, dspTime: {AudioSettings.dspTime}");
            }
            
            MornBeatGlobal.Log($"PlayWithFadeIn startDspTime: {startDspTime}, dspTime: {AudioSettings.dspTime}");
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

        public async UniTask UnloadWithFadeOutAsync(MornBeatIntroLoopAudioSource other, float duration, CancellationToken ct = default)
        {
            await FadeOutAsync(duration, ct);
            var unloadClipList = new List<AudioClip>();
            unloadClipList.Add(_audioSourceIntro.clip);
            unloadClipList.Add(_audioSourceLoop.clip);
            var taskList = new List<UniTask>();
            foreach (var clip in unloadClipList)
            {
                if (other == null || !other.Contain(clip))
                {
                    taskList.Add(clip.UnloadAsync(ct));
                }
            }

            await UniTask.WhenAll(taskList);
            _audioSourceIntro.clip = null;
            _audioSourceLoop.clip = null;
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