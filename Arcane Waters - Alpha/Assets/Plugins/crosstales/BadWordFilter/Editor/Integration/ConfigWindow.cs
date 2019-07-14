using UnityEditor;
using UnityEngine;
using Crosstales.BWF.EditorUtil;

namespace Crosstales.BWF.EditorIntegration
{
    /// <summary>Editor window extension.</summary>
    public class ConfigWindow : ConfigBase
    {

        #region Variables 

        private int tab = 0;
        private int lastTab = 0;
        private string inputText = "MARTIANS are asses.... => watch mypage.com";
        private string outputText;

        private Vector2 scrollPosPrefabs;
        private Vector2 scrollPosTD;

        #endregion


        #region EditorWindow methods

        [MenuItem("Tools/" + Util.Constants.ASSET_NAME + "/ Configuration...", false, EditorHelper.MENU_ID + 1)]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ConfigWindow));
        }

        public static void ShowWindow(int tab)
        {
            ConfigWindow window = EditorWindow.GetWindow(typeof(ConfigWindow)) as ConfigWindow;
            window.tab = tab;
        }

        public void OnEnable()
        {
            titleContent = new GUIContent(Util.Constants.ASSET_NAME_SHORT, EditorHelper.Logo_Asset_Small);
        }

        public void OnGUI()
        {
            tab = GUILayout.Toolbar(tab, new string[] { "Config", "Prefabs", "TD", "Help", "About" });

            if (tab != lastTab)
            {
                lastTab = tab;
                GUI.FocusControl(null);
            }

            if (tab == 0)
            {
                showConfiguration();

                EditorHelper.SeparatorUI();

                GUILayout.BeginHorizontal();
                {
                    if (GUILayout.Button(new GUIContent(" Save", EditorHelper.Icon_Save, "Saves the configuration settings for this project.")))
                    {
                        save();
                    }

                    if (GUILayout.Button(new GUIContent(" Reset", EditorHelper.Icon_Reset, "Resets the configuration settings for this project.")))
                    {
                        if (EditorUtility.DisplayDialog("Reset configuration?", "Reset the configuration of " + Util.Constants.ASSET_NAME + "?", "Yes", "No"))
                        {
                            Util.Config.Reset();
                            EditorConfig.Reset();
                            save();
                        }
                    }
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(6);
            }
            else if (tab == 1)
            {
                showPrefabs();
            }
            else if (tab == 2)
            {
                showTestDrive();
            }
            else if (tab == 3)
            {
                showHelp();
            }
            else
            {
                showAbout();
            }
        }

        public void OnInspectorUpdate()
        {
            Repaint();
        }

        #endregion


        #region Private methods

        private void showPrefabs()
        {
            scrollPosPrefabs = EditorGUILayout.BeginScrollView(scrollPosPrefabs, false, false);
            {
                GUILayout.Label("Available Prefabs", EditorStyles.boldLabel);

                GUILayout.Space(6);

                if (!EditorHelper.isBWFInScene)
                {

                    if (!EditorHelper.isBWFInScene)
                    {
                        GUILayout.Label(Util.Constants.MANAGER_SCENE_OBJECT_NAME);

                        if (GUILayout.Button(new GUIContent(" Add", EditorHelper.Icon_Plus, "Adds a '" + Util.Constants.MANAGER_SCENE_OBJECT_NAME + "'-prefab to the scene.")))
                        {
                            EditorHelper.InstantiatePrefab(Util.Constants.MANAGER_SCENE_OBJECT_NAME);
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("All available prefabs are already in the scene.", MessageType.Info);
                }

                GUILayout.Space(6);
            }
            EditorGUILayout.EndScrollView();
        }

        private void showTestDrive()
        {
            if (Util.Helper.isEditorMode)
            {
                if (BWFManager.isReady && EditorHelper.isBWFInScene)
                {
                    scrollPosTD = EditorGUILayout.BeginScrollView(scrollPosTD, false, false);
                    {

                        GUILayout.Label("Test-Drive", EditorStyles.boldLabel);

                        inputText = EditorGUILayout.TextField(new GUIContent("Input Text", "Text to check."), inputText);

                        EditorHelper.ReadOnlyTextField("Output Text", outputText);
                    }
                    EditorGUILayout.EndScrollView();

                    EditorHelper.SeparatorUI();

                    GUILayout.BeginHorizontal();
                    {
                        if (GUILayout.Button(new GUIContent(" Contains", EditorHelper.Icon_Contains, "Contains any bad words?")))
                        {
                            BWFManager.Load();
                            outputText = BWFManager.Contains(inputText).ToString();
                        }

                        if (GUILayout.Button(new GUIContent(" Get", EditorHelper.Icon_Get, "Get all bad words.")))
                        {
                            BWFManager.Load();
                            outputText = string.Join(", ", BWFManager.GetAll(inputText).ToArray());
                        }

                        if (GUILayout.Button(new GUIContent(" Replace", EditorHelper.Icon_Replace, "Check and replace all bad words.")))
                        {
                            BWFManager.Load();
                            outputText = BWFManager.ReplaceAll(inputText);
                        }

                        if (GUILayout.Button(new GUIContent(" Mark", EditorHelper.Icon_Mark, "Mark all bad words.")))
                        {
                            BWFManager.Load();
                            outputText = BWFManager.Mark(inputText);
                        }
                    }
                    GUILayout.EndHorizontal();

                }
                else
                {
                    EditorHelper.BWFUnavailable();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Disabled in Play-mode!", MessageType.Info);
            }
        }

        #endregion
    }
}
// © 2016-2019 crosstales LLC (https://www.crosstales.com)