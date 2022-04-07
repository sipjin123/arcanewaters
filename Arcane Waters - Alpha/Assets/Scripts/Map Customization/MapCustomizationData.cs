using System;
using UnityEngine;


namespace MapCustomization
{
   public class MapCustomizationData
   {
      #region Public Variables

      // Id of the user, who owns the customizations
      public int ownerId;

      // Id of the map that is modified
      public int mapId;

      // Deserialized prefab changes
      [SerializeField]
      public PrefabState[] prefabChanges = new PrefabState[0];

      #endregion

      public MapCustomizationData () {
         prefabChanges = new PrefabState[0];
      }

      #region Private Variables

      #endregion
   }
}
