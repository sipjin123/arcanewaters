using UnityEngine;
using UnityEditor;

namespace MapCreationTool.IssueResolving
{
   [CustomEditor(typeof(IssueResolver))]
   public class IssueResolverEditor : Editor
   {
      private bool alterData = false;
      private bool saveMaps = false;

      public override void OnInspectorGUI () {
         DrawDefaultInspector();

         EditorGUILayout.LabelField("Is the resolver allowed to alter data (ex. change tiles, prefabs) to resolve issues", EditorStyles.wordWrappedMiniLabel);
         alterData = EditorGUILayout.Toggle("Alter data", alterData);
         EditorGUILayout.LabelField("Should maps be saved after resolving issues? If not, resolver will only be useful for providing logs", EditorStyles.wordWrappedMiniLabel);
         saveMaps = EditorGUILayout.Toggle("Save Maps", saveMaps);

         if (Application.isEditor && Application.isPlaying && Overlord.instance != null && IssueResolver.instance != null && Overlord.allRemoteDataLoaded && !IssueResolver.running) {
            if (GUILayout.Button("Resolve Issues")) {
               if (EditorUtility.DisplayDialog("Resolve Issues", "Are you sure you want to start the issue resolve process?", "Yes")) {
                  IssueResolver.run(alterData, saveMaps);
               }
            }
         }
      }
   }
}

