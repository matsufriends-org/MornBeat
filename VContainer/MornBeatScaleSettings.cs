#if USE_VCONTAINER
using UnityEngine;

namespace MornBeat
{
    [CreateAssetMenu(fileName = nameof(MornBeatScaleSettings), menuName = "MornBeat/" + nameof(MornBeatScaleSettings))]
    public sealed class MornBeatScaleSettings : ScriptableObject
    {
        [SerializeField] private Vector3 _aimScale = Vector3.one * 1.2f;
        [SerializeField] private float _lerpSpeed = 10f;
        [SerializeField] private int _perBeat = 4;
        public Vector3 AimScale => _aimScale;
        public float LerpSpeed => _lerpSpeed;
        public int PerBeat => _perBeat;
    }
}
#endif