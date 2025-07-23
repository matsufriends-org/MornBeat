﻿using System;

namespace MornBeat
{
    /// <summary>1小節内における拍の構造体</summary>
    public readonly struct MornBeatTimingInfo
    {
        /// <summary>何チック目か</summary>
        public readonly int CurrentTick;
        /// <summary>1小節に何チックあるか</summary>
        public readonly int TickCountPerMeasure;
        /// <summary>何小節目か</summary>
        public int CurrentMeasure => CurrentTick / TickCountPerMeasure;

        /// <summary>コンストラクタ</summary>
        /// <param name="currentTick">何チック目か</param>
        /// <param name="tickCountPerMeasure">1小節に何チックあるか</param>
        public MornBeatTimingInfo(int currentTick, int tickCountPerMeasure)
        {
            CurrentTick = currentTick;
            TickCountPerMeasure = tickCountPerMeasure;
        }

        /// <summary>現在のチックに<paramref name="offsetTick"/>を加算したインスタンスを作成する。</summary>
        /// <param name="offsetTick">オフセットチック</param>
        /// <returns>作成したインスタンス</returns>
        public MornBeatTimingInfo CloneWithOffset(int offsetTick)
        {
            return new MornBeatTimingInfo(CurrentTick + offsetTick, TickCountPerMeasure);
        }

        /// <summary>チックを上書きしたインスタンスを作成する</summary>
        /// <param name="tick">チック</param>
        /// <returns>作成したインスタンス</returns>
        public MornBeatTimingInfo CloneWithOverridingTick(int tick)
        {
            return new MornBeatTimingInfo(tick, TickCountPerMeasure);
        }

        /// <summary>1小節[<paramref name="beat"/>]拍の、いずれかに合うかどうか</summary>
        /// <param name="beat">1小節に何拍あるか</param>
        /// <param name="offsetTick">オフセットチック</param>
        /// <returns>拍に合うかどうか</returns>
        public bool IsJustForAnyBeat(int beat, int offsetTick = 0)
        {
            return (CurrentTick + offsetTick) % (TickCountPerMeasure / beat) == 0;
        }

        /// <summary>1小節[<paramref name="beat"/>]拍の、何拍目か返す</summary>
        /// <param name="beat">1小節に何拍あるか</param>
        /// <param name="offsetTick">オフセットチック</param>
        /// <returns>拍に丁度合うときは何拍目か返す 拍に合わないときは-1を返す</returns>
        public int GetBeatCountBySpecificBeat(int beat, int offsetTick = 0)
        {
            if ((CurrentTick + offsetTick) % (TickCountPerMeasure / beat) != 0)
            {
                return -1;
            }

            return (CurrentTick + offsetTick) / (TickCountPerMeasure / beat);
        }

        [Obsolete("IsJust へ移行")]
        public bool IsJustForSpecificBeat(int numerator, int beat, int offsetTick = 0)
        {
            return IsJust(beat, numerator);
        }
        
        /// <summary>
        /// 1小節をmeasureTick分割した、tick拍の拍に合うかどうか
        /// </summary>
        public bool IsJust(int tick, int measureTick)
        {
            var tickScale = (float)TickCountPerMeasure / measureTick;
            var measureCount = 1 + tick / measureTick;
            return (int)(CurrentTick % (measureCount * measureTick * tickScale)) == (int)(tick * tickScale);
        }
    }
}