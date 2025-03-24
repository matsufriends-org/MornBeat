using System;
using UnityEngine;

namespace MornBeat
{
    [Serializable]
    public struct MornBeatAction<TEnum> where TEnum : Enum
    {
        [SerializeField] private int _measure;
        [SerializeField] private int _tick;
        [SerializeField] private TEnum _beatActionType;
        public int Measure => _measure;
        public int TickOnMeasure => _tick;
        public TEnum BeatActionType => _beatActionType;

        public MornBeatAction(int measure, int tick, TEnum beatActionType)
        {
            _measure = measure;
            _tick = tick;
            _beatActionType = beatActionType;
        }

        public MornBeatAction<TEnum> Add(int measure, int tick)
        {
            return new MornBeatAction<TEnum>(_measure + measure, _tick + tick, _beatActionType);
        }

        public int Tick(int measurerTick)
        {
            return _measure * measurerTick + _tick;
        }
    }
}