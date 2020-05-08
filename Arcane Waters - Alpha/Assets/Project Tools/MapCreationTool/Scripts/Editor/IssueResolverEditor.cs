using UnityEngine;
using UnityEditor;

namespace MapCreationTool.IssueResolving
{
   [CustomEditor(typeof(IssueResolver))]
   public class IssueResolverEditor : Editor
   {
      private bool alterData = false;
      private bool saveMaps = false;
      private bool createNewVersion = true;

      public override void OnInspectorGUI () {
         DrawDefaultInspector();

         EditorGUILayout.LabelField("Is the resolver allowed to alter data (ex. change tiles, prefabs) to resolve issues", EditorStyles.wordWrappedMiniLabel);
         alterData = EditorGUILayout.Toggle("Alter data", alterData);
         EditorGUILayout.LabelField("Should maps be saved after resolving issues? If not, resolver will only be useful for providing logs", EditorStyles.wordWrappedMiniLabel);
         saveMaps = EditorGUILayout.Toggle("Save maps", saveMaps);
         EditorGUILayout.LabelField("Should a new version be created, or should the results be saved on top of the existing version", EditorStyles.wordWrappedMiniLabel);
         createNewVersion = EditorGUILayout.Toggle("Create new version", createNewVersion);

         if (Application.isEditor && Application.isPlaying && Overlord.instance != null && IssueResolver.instance != null && Overlord.allRemoteDataLoaded && !IssueResolver.running) {
            if (GUILayout.Button("Resolve Issues")) {
               if (EditorUtility.DisplayDialog("Resolve Issues", "Are you sure you want to start the issue resolve process?", "Yes")) {
                  IssueResolver.run(alterData, saveMaps, createNewVersion);
               }
            }
            if (GUILayout.Button("Validate map")) {
               if (Overlord.validateMap(out string errors)) {
                  UI.messagePanel.displayInfo("Map validation", "No errors found");
               } else {
                  UI.messagePanel.displayError("Map validation", "Found following errors" + System.Environment.NewLine + errors);
               }
            }
         }
      }
   }
}

