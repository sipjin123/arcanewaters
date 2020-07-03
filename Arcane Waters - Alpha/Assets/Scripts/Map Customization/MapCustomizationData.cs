using System;
using UnityEngine;


namespace MapCustomization
{
   public class MapCustomizationData
   {
      #region Public Variables

      // Id of the user, who owns the customizations
      public int userId;

      // Id of the map that is modified
      public int mapId;

      // Deserialized prefab changes
      [NonSerialized]
      public PrefabState[] prefabChanges = new PrefabState[0];

      #endregion

      public MapCustomizationData () {
         prefabChanges = new PrefabState[0];
      }

      public static MapCustomizationData deserialize (string data) {
         if (data == null) return new MapCustomizationData();

         MapCustomizationData result = JsonUtility.FromJson<MapCustomizationData>(data);

         return result;
      }

      public string serialize () {
         return JsonUtility.ToJson(this);
      }

      public void add (PrefabState newChanges) {
         for (int i = 0; i < prefabChanges.Length; i++) {
            if (prefabChanges[i].id == newChanges.id) {
               prefabChanges[i] = prefabChanges[i].add(newChanges);
               return;
            }
         }

         Array.Resize(ref prefabChanges, prefabChanges.Length + 1);
         prefabChanges[prefabChanges.Length - 1] = newChanges;
      }

      #region Private Variables

      #endregion
   }
}
