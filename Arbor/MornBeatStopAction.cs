#if USE_ARBOR
using Arbor;
using UnityEngine;
using VContainer;

namespace MornBeat
{
    public class MornBeatStopAction : StateBehaviour
    {
        [SerializeField] private StateLink _onComplete;
        [Inject] private MornBeatControllerMono _beatController;

        public override async void OnStateBegin()
        {
            await _beatController.StopBeatAsync(ct: CancellationTokenOnEnd);
            Transition(_onComplete);
        }
    }
}
#endif