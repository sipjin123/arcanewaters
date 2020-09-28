#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SoundVolumeTestManager))]
public class SoundVolumeTestManagerEditor : Editor {
   #region Public Variables

   #endregion

   void OnEnable () {
      _types = serializedObject.FindProperty("types");
   }

   public override void OnInspectorGUI () {
      serializedObject.Update();
      EditorGUILayout.PropertyField(_types);
      serializedObject.ApplyModifiedProperties();

      if (GUILayout.Button("Play sound")) {
         (target as SoundVolumeTestManager).playSound();
      }
   }

   #region Private Variables

   // Type of Sound to be played
   private SerializedProperty _types;

   #endregion
}
#endif