using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TestWindow : EditorWindow {
   #region Public Variables

   #endregion

   void OnGUI () {
      GUILayout.Label("Base Settings", EditorStyles.boldLabel);
      myString = EditorGUILayout.TextField("Text Field", myString);

      groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", groupEnabled);
      myBool = EditorGUILayout.Toggle("Toggle", myBool);
      myFloat = EditorGUILayout.Slider("Slider", myFloat, -3, 3);
      EditorGUILayout.EndToggleGroup();
   }

   #region Private Variables

   protected string myString = "Hello World";
   protected bool groupEnabled;
   protected bool myBool = true;
   protected float myFloat = 1.23f;

   #endregion
}
