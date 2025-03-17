#if USE_VCONTAINER
using UniRx;
using UnityEngine;
using VContainer;

namespace MornBeat
{
    public sealed class MornBeatScaler : MonoBehaviour
    {
        [Inject] private MornBeatControllerMono _beatController;
        [SerializeField] private MornBeatScaleSettings _settings;
        private Vector3 _originScale;

        private void Start()
        {
            _originScale = transform.localScale;
            _beatController.PlayModule.OnBeat.Where(x => x.IsJustForAnyBeat(_settings.PerBeat)).Subscribe(_ => transform.localScale = _settings.AimScale).AddTo(this);
        }

        private void Update()
        {
            var scale = Vector3.Lerp(transform.localScale, _originScale, Time.deltaTime * _settings.LerpSpeed);
            transform.localScale = scale;
        }
    }
}
#endif