using UnityEngine;
using UnityEditor;
using Crosstales.BWF.EditorUtil;

namespace Crosstales.BWF.EditorExtension
{
    /// <summary>Custom editor for the 'CapitalizationManager'-class.</summary>
    [CustomEditor(typeof(Manager.CapitalizationManager))]
    public class CapitalizationManagerEditor : Editor
    {

        #region Variables

        private Manager.CapitalizationManager script;

        private string inputText = "COME ON, TEST ME User!";
        private string outputText;

        #endregion


        #region Editor methods

        public void OnEnable()
        {
            script = (Manager.CapitalizationManager)target;

            if (script.isActiveAndEnabled)
            {
                Manager.CapitalizationManager.Load();
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorHelper.SeparatorUI();

            if (script.isActiveAndEnabled)
            {
                if (Manager.CapitalizationManager.isReady)
                {
                    GUILayout.Label("Test-Drive", EditorStyles.boldLabel);

                    if (Util.Helper.isEditorMode)
                    {
                        inputText = EditorGUILayout.TextField(new GUIContent("Input Text", "Text to check."), inputText);

                        EditorHelper.ReadOnlyTextField("Output Text", outputText);

                        GUILayout.Space(8);

                        GUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button(new GUIContent(" Contains", EditorHelper.Icon_Contains, "Contains any extensive capitalizations?")))
                            {
                                //Manager.CapitalizationManager.Load();
                                outputText = Manager.CapitalizationManager.Contains(inputText).ToString();
                            }

                            if (GUILayout.Button(new GUIContent(" Get", EditorHelper.Icon_Get, "Get all extensive capitalizations.")))
                            {
                                //Manager.CapitalizationManager.Load();
                                outputText = string.Join(", ", Manager.CapitalizationManager.GetAll(inputText).ToArray());
                            }

                            if (GUILayout.Button(new GUIContent(" Replace", EditorHelper.Icon_Replace, "Check and replace all extensive capitalizations.")))
                            {
                                //Manager.CapitalizationManager.Load();
                                outputText = Manager.CapitalizationManager.ReplaceAll(inputText);
                            }

                            if (GUILayout.Button(new GUIContent(" Mark", EditorHelper.Icon_Mark, "Mark all extensive capitalizations.")))
                            {
                                //Manager.CapitalizationManager.Load();
                                outputText = Manager.CapitalizationManager.Mark(inputText);
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Disabled in Play-mode!", MessageType.Info);
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Script is disabled!", MessageType.Info);
            }
        }

        public override bool RequiresConstantRepaint()
        {
            return true;
        }

        #endregion

    }
}
// © 2016-2019 crosstales LLC (https://www.crosstales.com)