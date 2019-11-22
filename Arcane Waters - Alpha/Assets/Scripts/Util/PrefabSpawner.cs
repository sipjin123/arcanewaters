using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PrefabSpawner : MonoBehaviour {
   #region Public Variables

   // A list of prefabs we want to auto-spawn at startup
   public List<GameObject> prefabs;

   #endregion

   void Awake () {
      foreach (GameObject prefab in prefabs) {
         if (prefab != null) {
            // If it's a map that's already in the scene, don't duplicate it
            Area area = prefab.GetComponent<Area>();

            if (area != null && isAreaAlreadyCreated(area.areaKey)) {
               continue;
            }

            Instantiate(prefab);
         }
      }
   }

   protected bool isAreaAlreadyCreated (string areaKey) {
      foreach (Area area in FindObjectsOfType<Area>()) {
         if (area.areaKey == areaKey) {
            return true;
         }
      }

      return false;
   }

   #region Private Variables

   #endregion
}
