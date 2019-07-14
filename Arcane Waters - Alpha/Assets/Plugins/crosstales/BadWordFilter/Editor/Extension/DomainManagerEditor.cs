using UnityEngine;
using UnityEditor;
using Crosstales.BWF.EditorUtil;

namespace Crosstales.BWF.EditorExtension
{
    /// <summary>Custom editor for the 'DomainManager'-class.</summary>
    [CustomEditor(typeof(Manager.DomainManager))]
    public class DomainManagerEditor : Editor
    {

        #region Variables

        private Manager.DomainManager script;

        private string inputText = "Write me an email bwfuser@mypage.com!";
        private string outputText;

        #endregion


        #region Editor methods

        public void OnEnable()
        {
            script = (Manager.DomainManager)target;

            //Debug.Log(script.isActiveAndEnabled);
            /*
            if (script.isActiveAndEnabled)
            {
                Manager.DomainManager.Load();
            }
            */
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorHelper.SeparatorUI();

            if (script.isActiveAndEnabled)
            {
                GUILayout.Label("Stats", EditorStyles.boldLabel);

                GUILayout.Label("Ready:\t" + (Manager.DomainManager.isReady ? "Yes" : "No"));

                if (Manager.DomainManager.isReady)
                {
                    GUILayout.Label("Sources:\t" + Manager.DomainManager.Sources.Count);

                    EditorHelper.SeparatorUI();

                    if (script.DomainProvider != null && script.DomainProvider.Count > 0)
                    {

                        GUILayout.Label("Test-Drive", EditorStyles.boldLabel);

                        if (Util.Helper.isEditorMode)
                        {
                            inputText = EditorGUILayout.TextField(new GUIContent("Input Text", "Text to check."), inputText);

                            EditorHelper.ReadOnlyTextField("Output Text", outputText);

                            GUILayout.Space(8);

                            GUILayout.BeginHorizontal();
                            {
                                if (GUILayout.Button(new GUIContent(" Contains", EditorHelper.Icon_Contains, "Contains any domains?")))
                                {
                                    //Manager.DomainManager.Load();
                                    outputText = Manager.DomainManager.Contains(inputText).ToString();
                                }

                                if (GUILayout.Button(new GUIContent(" Get", EditorHelper.Icon_Get, "Get all domains.")))
                                {
                                    //Manager.DomainManager.Load();
                                    outputText = string.Join(", ", Manager.DomainManager.GetAll(inputText).ToArray());
                                }

                                if (GUILayout.Button(new GUIContent(" Replace", EditorHelper.Icon_Replace, "Check and replace all domains.")))
                                {
                                    //Manager.DomainManager.Load();
                                    outputText = Manager.DomainManager.ReplaceAll(inputText);
                                }

                                if (GUILayout.Button(new GUIContent(" Mark", EditorHelper.Icon_Mark, "Mark all domains.")))
                                {
                                    //Manager.DomainManager.Load();
                                    outputText = Manager.DomainManager.Mark(inputText);
                                }
                            }
                            GUILayout.EndHorizontal();
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("Disabled in Play-mode!", MessageType.Info);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Please add a Domain Provider!", MessageType.Warning);
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