using UnityEngine;
using UnityEditor;
using Crosstales.BWF.EditorUtil;

namespace Crosstales.BWF.EditorExtension
{
    /// <summary>Custom editor for the 'PunctuationManager'-class.</summary>
	[CustomEditor(typeof(Manager.PunctuationManager))]
    public class PunctuationManagerEditor : Editor
    {

        #region Variables

        private Manager.PunctuationManager script;

        private string inputText = "Come on, test me!!!!!!";
        private string outputText;

        #endregion


        #region Editor methods

        public void OnEnable()
        {
            script = (Manager.PunctuationManager)target;

            if (script.isActiveAndEnabled)
            {
                Manager.PunctuationManager.Load();
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorHelper.SeparatorUI();

            if (script.isActiveAndEnabled)
            {
                if (Manager.PunctuationManager.isReady)
                {
                    GUILayout.Label("Test-Drive", EditorStyles.boldLabel);

                    if (Util.Helper.isEditorMode)
                    {
                        inputText = EditorGUILayout.TextField(new GUIContent("Input Text", "Text to check."), inputText);

                        EditorHelper.ReadOnlyTextField("Output Text", outputText);

                        GUILayout.Space(8);

                        GUILayout.BeginHorizontal();
                        {
                            if (GUILayout.Button(new GUIContent(" Contains", EditorHelper.Icon_Contains, "Contains any extensive punctuations?")))
                            {
                                //Manager.PunctuationManager.Load();
                                outputText = Manager.PunctuationManager.Contains(inputText).ToString();
                            }

                            if (GUILayout.Button(new GUIContent(" Get", EditorHelper.Icon_Get, "Get all extensive punctuations.")))
                            {
                                //Manager.PunctuationManager.Load();
                                outputText = string.Join(", ", Manager.PunctuationManager.GetAll(inputText).ToArray());
                            }

                            if (GUILayout.Button(new GUIContent(" Replace", EditorHelper.Icon_Replace, "Check and replace all extensive punctuations.")))
                            {
                                //Manager.PunctuationManager.Load();
                                outputText = Manager.PunctuationManager.ReplaceAll(inputText);
                            }

                            if (GUILayout.Button(new GUIContent(" Mark", EditorHelper.Icon_Mark, "Mark all extensive punctuations.")))
                            {
                                //Manager.PunctuationManager.Load();
                                outputText = Manager.PunctuationManager.Mark(inputText);
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