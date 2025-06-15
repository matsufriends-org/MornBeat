#if USE_ARBOR
using Arbor;
using UnityEngine;
using VContainer;

namespace MornBeat
{
    internal class BeatPlayState : StateBehaviour
    {
        [SerializeField] private MornBeatMemoSo _beatMemo;
        [SerializeField] private StateLink _onComplete;
        [Inject] private MornBeatControllerMono _beatController;

        public override async void OnStateBegin()
        {
            await _beatController.StartAsync(
                new MornBeatStartInfo
                {
                    BeatMemo = _beatMemo,
                    Ct = CancellationTokenOnEnd,
                });
            Transition(_onComplete);
        }
    }
}
#endif