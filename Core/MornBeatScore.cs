using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MornBeat
{
    [Serializable]
    public struct MornBeatScore
    {
        [SerializeField] public TextAsset Score;
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(MornBeatScore))]
    public class MornBeatScoreDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            {
                var scoreSerializedProperty = property.FindPropertyRelative(nameof(MornBeatScore.Score));
                EditorGUI.PropertyField(position, scoreSerializedProperty, label);
            }
            EditorGUI.EndProperty();
        }
    }
#endif
}