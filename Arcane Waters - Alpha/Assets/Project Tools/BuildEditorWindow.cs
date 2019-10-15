﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
using UnityEditor.Build.Reporting;


public class BuildEditorWindow : EditorWindow {

   #region Public Variables

   // Holds the scenes
   List<SceneAsset> m_SceneAssets = new List<SceneAsset>();

   // Determines if auto build upon changing scene
   public bool buildOnClick;

   // Fixed naming for scenes and images
   public const string CURSOR_GAME = "mouse_pointer";
   public const string CURSOR_TOOL = "mouse_hand";
   public const string SCENE_CRAFT = "CraftingScene";
   public const string SCENE_NPC = "NPC Tool";
   public const string SCENE_GAME = "Main";

   #endregion

   // Add menu item named "Example Window" to the Window menu
   [MenuItem("Window/Example Window")]
   public static void ShowWindow () {
      //Show existing window instance. If one doesn't exist, make one.
      EditorWindow.GetWindow(typeof(BuildEditorWindow));
   }

   void OnGUI () {
      GUILayout.Label("Scenes to include in build:", EditorStyles.boldLabel);
      for (int i = 0; i < m_SceneAssets.Count; ++i) {
         m_SceneAssets[i] = (SceneAsset) EditorGUILayout.ObjectField(m_SceneAssets[i], typeof(SceneAsset), false);
      }
      if (GUILayout.Button("Add")) {
         m_SceneAssets.Add(null);
      }

      GUILayout.Space(8);

      if (GUILayout.Button("Build Crafting: "+buildOnClick)) {
         buildOnClick = !buildOnClick;
      }

      GUILayout.Space(8);
      
      if (GUILayout.Button("Main")) {
         m_SceneAssets.Clear();

         setTexture("Main");
         PlayerSettings.productName = "Arcane Waters";
         setEditorBuildSettingsScenes(SCENE_GAME);
         setCursor(CURSOR_GAME);

         if (buildOnClick) {
            buildScene("Main", PlayerSettings.productName);
         }
      }

      if (GUILayout.Button("Crafting")) {
         m_SceneAssets.Clear();

         setTexture("Crafting");
         PlayerSettings.productName = "Arcane Waters Crafting Tool";
         setEditorBuildSettingsScenes(SCENE_CRAFT);
         setCursor(CURSOR_TOOL);

         if (buildOnClick) {
            buildScene("CraftingScene", PlayerSettings.productName);
         }
      }

      if (GUILayout.Button("NPC")) {
         m_SceneAssets.Clear();

         setTexture("NPC");
         PlayerSettings.productName = "Arcane Waters NPC Tool";
         setEditorBuildSettingsScenes(SCENE_NPC);
         setCursor(CURSOR_TOOL);

         if (buildOnClick) {
            buildScene("NPC Tool", PlayerSettings.productName);
         }
      }
   }

   private void setTexture(string textureName) {
      string filePath = "Assets/BuildIcons/"+textureName+".png";
      Texture2D texture = (Texture2D) AssetDatabase.LoadAssetAtPath(filePath, typeof(Texture2D));
      List<Texture2D> text2d = new List<Texture2D>();
      for (int i = 0; i < 8; i++) {
         text2d.Add(texture);
      }
      PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, text2d.ToArray());
   }

   private void setCursor (string mouseImageName) {
      string filePath = "Assets/Sprites/GUI/" + mouseImageName + ".png";
      Texture2D texture = (Texture2D) AssetDatabase.LoadAssetAtPath(filePath, typeof(Texture2D));
    
      PlayerSettings.defaultCursor = texture;
   }

   private void setEditorBuildSettingsScenes (string sceneName) {
      SceneAsset sceneAssetData = (SceneAsset) AssetDatabase.LoadAssetAtPath("Assets/Scenes/" + sceneName + ".unity", typeof(SceneAsset));
      m_SceneAssets.Add(sceneAssetData);

      // Find valid Scene paths and make a list of EditorBuildSettingsScene
      List<EditorBuildSettingsScene> editorBuildSettingsScenes = new List<EditorBuildSettingsScene>();
      foreach (var sceneAsset in m_SceneAssets) {
         string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
         if (!string.IsNullOrEmpty(scenePath))
            editorBuildSettingsScenes.Add(new EditorBuildSettingsScene(scenePath, true));
      }

      // Set the Build Settings window Scene list
      EditorBuildSettings.scenes = editorBuildSettingsScenes.ToArray();
   }

   private void buildScene (string sceneName, string buildName) {

      // Get filename.
      string path = EditorUtility.SaveFolderPanel("Choose Location of Built Game", "", "");
      string[] levels = new string[] { "Assets/Scenes/"+ sceneName + ".unity" };

      // Build player.
      BuildPipeline.BuildPlayer(levels, path + "/"+ buildName + ".exe", BuildTarget.StandaloneWindows, BuildOptions.None);

      // Run the game (Process class from System.Diagnostics).
      System.Diagnostics.Process proc = new System.Diagnostics.Process();
      proc.StartInfo.FileName = path + "/"+ buildName + ".exe";
      proc.Start();
   }
}

#endif