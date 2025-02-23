using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MornBeat
{
    [CreateAssetMenu(menuName = "MornLib/" + nameof(MornBeatMusic))]
    public sealed class MornBeatMusic : ScriptableObject
    {
        [SerializeField] internal AudioClip MusicClip;
        [SerializeField] internal List<MornBeatMeasure> MeasureList;
        [SerializeField] internal List<MornBeatPhase> BpmList;
        [SerializeField] internal List<double> TimeList;

        /// <summary>対象のサンプル数を返します</summary>
        /// <param name="measure">何小節目か</param>
        /// <param name="beat">基準となる拍の何番目か</param>
        /// <param name="beatBase">基準となる拍が何拍子か</param>
        public long GetSample(int measure, int beat, int beatBase)
        {
            throw new NotImplementedException();
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MornBeatMusic))]
    internal sealed class MornBeatMusicEditor : Editor
    {
        private SerializedProperty _musicClipSerializedProperty;
        private SerializedProperty _measureListSerializedProperty;
        private SerializedProperty _bpmListSerializedProperty;
        private SerializedProperty _timeListSerializedProperty;
        private MornBeatFoldoutGroup _generateFromBpm;
        private MornBeatFoldoutGroup _generateFromTimeStamp;

        private void OnEnable()
        {
            _musicClipSerializedProperty = serializedObject.FindProperty(nameof(MornBeatMusic.MusicClip));
            _measureListSerializedProperty = serializedObject.FindProperty(nameof(MornBeatMusic.MeasureList));
            _bpmListSerializedProperty = serializedObject.FindProperty(nameof(MornBeatMusic.BpmList));
            _timeListSerializedProperty = serializedObject.FindProperty(nameof(MornBeatMusic.TimeList));
            _generateFromBpm = new MornBeatFoldoutGroup(DrawGenerateFromBpm, "Generate From Bpm");
            _generateFromTimeStamp = new MornBeatFoldoutGroup(DrawGenerateFromTimeStamp, "Generate From TimeStamp");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawAudioClipData();
            DrawMeasureList();
            GUILayout.Space(30);
            _generateFromBpm.OnGUI();
            _generateFromTimeStamp.OnGUI();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawAudioClipData()
        {
            EditorGUILayout.PropertyField(_musicClipSerializedProperty);
            if (_musicClipSerializedProperty.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("MusicClip is required.", MessageType.Error);
                return;
            }

            EditorGUI.indentLevel++;
            var audioClip = (AudioClip)_musicClipSerializedProperty.objectReferenceValue;
            EditorGUILayout.LabelField("Frequency", audioClip.frequency.ToString());
            EditorGUILayout.LabelField("Samples", audioClip.samples.ToString());
            EditorGUILayout.LabelField("Length", $@"{TimeSpan.FromSeconds(audioClip.length):hh\:mm\:ss\.ff}");
            EditorGUI.indentLevel--;
        }

        private void DrawMeasureList()
        {
            GUI.enabled = false;
            EditorGUILayout.PropertyField(_measureListSerializedProperty);
            GUI.enabled = true;
        }

        private void DrawGenerateFromBpm()
        {
            var music = (MornBeatMusic)target;
            // 自動生成:BPMから
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_bpmListSerializedProperty);
            if (GUILayout.Button("Generate", GUILayout.Height(30)))
            {
                var isSuccess = true;
                MornBeatGlobal.Log("Generate Start");
                var result = new List<MornBeatMeasure>();
                result.Add(new MornBeatMeasure(1, 0));
                var frequency = music.MusicClip.frequency;
                var virtualTime = 0d;
                const double deltaBeat = 0.000001d;
                foreach (var phase in music.BpmList)
                {
                    if (phase.Transition.StartBpm == 0)
                    {
                        isSuccess = false;
                        MornBeatGlobal.LogError("StartBpm is 0");
                        break;
                    }

                    if (phase.Transition.EndBpm == 0)
                    {
                        isSuccess = false;
                        MornBeatGlobal.LogError("EndBpm is 0");
                        break;
                    }

                    if (phase.Length.Beat == 0)
                    {
                        isSuccess = false;
                        MornBeatGlobal.LogError("BeatCount is 0");
                        break;
                    }

                    if (phase.Length.NoteType == 0)
                    {
                        isSuccess = false;
                        MornBeatGlobal.LogError("BeatType is 0");
                        break;
                    }

                    if (phase.Length.Measure == 0)
                    {
                        isSuccess = false;
                        MornBeatGlobal.LogError("MeasureCount is 0");
                        break;
                    }

                    var startBpm = phase.Transition.StartBpm;
                    var endBpm = phase.Transition.EndBpm;
                    var difBpm = endBpm - startBpm;
                    var phaseBeat = 0d;

                    // 1Beat = 4分音符１つ分
                    // n/m 拍子のとき
                    //      sum = n / m * 4 * 小節数
                    var phaseMeasureBeat = 4d * phase.Length.Beat / phase.Length.NoteType;
                    var phaseTotalBeat = phaseMeasureBeat * phase.Length.Measure;

                    // 小節またぎ判定Beat
                    var nextPhaseBeat = phaseMeasureBeat;
                    for (var i = 0; i < phase.Length.Measure; i++)
                    {
                        while (phaseBeat < nextPhaseBeat)
                        {
                            // lerp
                            var currentBpm = startBpm + difBpm * (phaseBeat / phaseTotalBeat);

                            // BPM = 1小節の4分音符の数
                            // 1Beatでの経過時間 = 60 / BPM
                            virtualTime += 60d / currentBpm * deltaBeat;
                            phaseBeat += deltaBeat;
                        }

                        result.Add(new MornBeatMeasure(result.Count + 1, (long)(virtualTime * frequency)));
                        nextPhaseBeat += phaseMeasureBeat;
                    }
                }

                music.MeasureList.Clear();
                music.MeasureList.AddRange(result);
                if (isSuccess)
                {
                    MornBeatGlobal.Log("Generate Success");
                }
                else
                {
                    MornBeatGlobal.LogError("Generate Failed. Please check the log.");
                }
            }

            EditorGUI.indentLevel--;
        }

        private void DrawGenerateFromTimeStamp()
        {
            var music = (MornBeatMusic)target;
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_timeListSerializedProperty);
            if (GUILayout.Button("Generate", GUILayout.Height(30)))
            {
                MornBeatGlobal.Log("Generate Start");
                var result = new List<MornBeatMeasure>();
                result.Add(new MornBeatMeasure(1, 0));
                var frequency = music.MusicClip.frequency;
                for (var i = 0; i < music.TimeList.Count; i++)
                {
                    var time = music.TimeList[i];
                    result.Add(new MornBeatMeasure(2 + i, (long)(time * frequency)));
                }

                music.MeasureList.Clear();
                music.MeasureList.AddRange(result);
                MornBeatGlobal.Log("Generate End");
            }

            EditorGUI.indentLevel--;
        }
    }
#endif
}