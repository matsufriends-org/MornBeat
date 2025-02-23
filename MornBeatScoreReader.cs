using System.Collections.Generic;
using UnityEngine;

namespace MornBeat
{
    public class MornBeatScoreReader
    {
        private readonly Dictionary<long, int> _noteDictionary = new();

        public MornBeatScoreReader(TextAsset score, MornBeatConverterBase converter, MornBeatMusic music)
        {
            _noteDictionary = new Dictionary<long, int>();
            var lines = score.text.Split('\n');
            var measure = 1;
            foreach (var line in lines)
            {
                var tmpFlagList = new List<int>();
                for (var i = 0; i < line.Length; i++)
                {
                    var flag = 0;
                    var c1 = line[i];
                    if (c1 == MornBeatUtil.OpenSplit)
                    {
                        var endIndex = line.IndexOf(MornBeatUtil.CloseSplit, i);
                        if (endIndex == -1)
                        {
                            MornBeatGlobal.LogError("閉じられていません。");
                            break;
                        }

                        var lengthText = line.Substring(i + 1, endIndex - i - 1);
                        foreach (var c2 in lengthText)
                        {
                            flag |= converter.ToFlag(c2);
                        }

                        i = endIndex;
                    }
                    else
                    {
                        flag |= converter.ToFlag(c1);
                    }

                    tmpFlagList.Add(flag);
                }

                for (var x = 0; x < tmpFlagList.Count; x++)
                {
                    var flag = tmpFlagList[x];
                    if (flag != 0)
                    {
                        var tick = music.GetSample(measure, x + 1, tmpFlagList.Count);
                        _noteDictionary.Add(tick, flag);
                    }
                }

                measure++;
            }
        }
    }
}