using System.Linq;
using UnityEditor;
using UnityEngine;

[InitializeOnLoadAttribute]
public static class EditorMonitor {
   static EditorMonitor () {
      EditorApplication.hierarchyChanged += OnHierarchyChanged;
      EditorApplication.projectChanged += OnProjectChanged;
   }

   static void OnHierarchyChanged () {
      /*var all = Resources.FindObjectsOfTypeAll(typeof(GameObject));
      var numberVisible =
          all.Where(obj => (obj.hideFlags & HideFlags.HideInHierarchy) != HideFlags.HideInHierarchy).Count();
      Debug.LogFormat("There are currently {0} GameObjects visible in the hierarchy.", numberVisible);*/
   }

   static void OnProjectChanged () {
      #if UNITY_EDITOR && !CLOUD_BUILD
         Debug.Log("Project hierarchy changed, so updating image manager.");
         EditorUtil.updateImagerManager();
      #endif
   }
}