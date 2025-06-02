using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MornBeat
{
    public abstract class MornBeatConverterBase : ScriptableObject
    {
        protected abstract (int, char)[] ConvertArray { get; }

        public Dictionary<int, MornBeatAction<TEnum>> GetDictionary<TEnum>(TextAsset textAsset) where TEnum : Enum
        {
            var dictionary = new Dictionary<int, MornBeatAction<TEnum>>();
            // 空欄を除く
            var lines = textAsset.text.Replace(" ", "").Split('\n', '\r');
            // 空行を除く
            lines = Array.FindAll(lines, line => !string.IsNullOrEmpty(line));
            var score = new List<List<MornBeatAction<TEnum>>>();
            for (var measure = 0; measure < lines.Length; measure++)
            {
                var text = lines[measure];

                // IndexとLengthを無視
                var tmpMeasureNotes = new List<MornBeatAction<TEnum>>();
                for (var index = 0; index < text.Length; index++)
                {
                    var c = text[index];
                    if (c == MornBeatUtil.OpenSplit)
                    {
                        var endIndex = text.IndexOf(MornBeatUtil.CloseSplit, index);
                        if (endIndex == -1)
                        {
                            MornBeatGlobal.LogWarning("閉じられていません。");
                            break;
                        }

                        var lengthText = text.Substring(index + 1, endIndex - index - 1);
                        var flag = 0;
                        foreach (var c2 in lengthText)
                        {
                            flag |= ToFlag(c2);
                        }

                        tmpMeasureNotes.Add(new MornBeatAction<TEnum>(measure, -1, (TEnum)(flag as object)));
                        index = endIndex;
                    }
                    else
                    {
                        var noteType = ToFlag(c);
                        tmpMeasureNotes.Add(new MornBeatAction<TEnum>(measure, -1, (TEnum)(noteType as object)));
                    }
                }

                score.Add(tmpMeasureNotes);
            }

            // scoreの各種小節Tickの最小公倍数を求める
            var measureTick = 1;
            foreach (var measureNotes in score)
            {
                if (measureNotes.Count == 0)
                    continue;
                var measureLength = measureNotes.Count;
                measureTick = LCM(measureTick, measureLength);
            }

            // tickとして代入していく
            for (var measure = 0; measure < score.Count; measure++)
            {
                var measureList = score[measure];
                var baseTick = measure * measureTick;
                var tickScale = measureTick / measureList.Count;
                
                for (var i = 0; i < measureList.Count; i++)
                {
                    var note = measureList[i];
                    var tick = baseTick + i * tickScale;
                    if ((int)(object)note.BeatActionType != 0)
                    {
                        var newNote = new MornBeatAction<TEnum>(note.Measure, i, note.BeatActionType);
                        dictionary.Add(tick, newNote);
                    }
                }
            }
            
            return dictionary;
        }

        private int LCM(int a, int b)
        {
            int Gcd(int x, int y)
            {
                return y == 0 ? x : Gcd(y, x % y);
            }

            return a / Gcd(a, b) * b;
        }

        public int ToFlag(char c)
        {
            foreach (var pair in ConvertArray)
            {
                if (pair.Item2 == c)
                {
                    return pair.Item1;
                }
            }

            return 0;
        }

        public string ConvertToText(int noteType)
        {
            var list = new List<char>();
            foreach (var pair in ConvertArray)
            {
                if (noteType.BitHas(pair.Item1))
                {
                    list.Add(pair.Item2);
                }
            }

            if (list.Count == 0)
            {
                return "0";
            }

            if (list.Count == 1)
            {
                return list[0].ToString();
            }

            var sb = new StringBuilder();
            sb.Append(MornBeatUtil.OpenSplit);
            foreach (var c in list)
            {
                sb.Append(c);
            }

            sb.Append(MornBeatUtil.CloseSplit);
            return sb.ToString();
        }
    }
}