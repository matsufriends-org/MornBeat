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
            var tick = 0;
            for (var measure = 0; measure < lines.Length; measure++)
            {
                var text = lines[measure];

                // IndexとLengthを無視
                var tmpLineNotes = new List<MornBeatAction<TEnum>>();
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

                        tmpLineNotes.Add(new MornBeatAction<TEnum>(measure, -1, (TEnum)(flag as object)));
                        index = endIndex;
                    }
                    else
                    {
                        var noteType = ToFlag(c);
                        tmpLineNotes.Add(new MornBeatAction<TEnum>(measure, -1, (TEnum)(noteType as object)));
                    }
                }

                // IndexとLengthを再計算
                var length = tmpLineNotes.Count;
                for (var i = 0; i < length; i++)
                {
                    var note = tmpLineNotes[i];
                    if ((int)(object)note.BeatActionType != 0)
                    {
                        var newNote = new MornBeatAction<TEnum>(note.Measure, i, note.BeatActionType);
                        dictionary.Add(tick, newNote);
                    }

                    tick++;
                }
            }

            return dictionary;
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