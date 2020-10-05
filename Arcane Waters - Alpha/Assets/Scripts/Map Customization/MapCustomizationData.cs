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
      [SerializeField]
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

      #region Private Variables

      #endregion
   }
}
