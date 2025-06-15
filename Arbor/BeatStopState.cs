#if USE_ARBOR
using System.Threading;
using Arbor;
using UnityEngine;
using VContainer;

namespace MornBeat
{
    internal class BeatStopState : StateBehaviour
    {
        [SerializeField] private StateLink _onComplete;
        [SerializeField] private float _stopDuration;
        [SerializeField] private bool _isIsolate;
        [Inject] private MornBeatControllerMono _beatController;

        public override async void OnStateBegin()
        {
            CancellationToken? ct = _isIsolate ? null : CancellationTokenOnEnd;
            await _beatController.StopBeatAsync(_stopDuration, ct);
            Transition(_onComplete);
        }
    }
}
#endif