using System;
using UnityEngine;

namespace MornBeat
{
    [Serializable]
    internal class MornBeatAudioSourceModule
    {
        [SerializeField] [ReadOnly] private bool _isUsingAudioSourceA;
        [SerializeField] private MornBeatIntroLoopAudioSource _audioSourceA;
        [SerializeField] private MornBeatIntroLoopAudioSource _audioSourceB;

        public MornBeatIntroLoopAudioSource GetCurrent(bool changeSource = false)
        {
            var result = _isUsingAudioSourceA ? _audioSourceA : _audioSourceB;
            if (changeSource)
            {
                _isUsingAudioSourceA = !_isUsingAudioSourceA;
            }

            return result;
        }

        public MornBeatIntroLoopAudioSource GetOther(bool changeSource = false)
        {
            var result = _isUsingAudioSourceA ? _audioSourceB : _audioSourceA;
            if (changeSource)
            {
                _isUsingAudioSourceA = !_isUsingAudioSourceA;
            }

            return result;
        }
    }
}