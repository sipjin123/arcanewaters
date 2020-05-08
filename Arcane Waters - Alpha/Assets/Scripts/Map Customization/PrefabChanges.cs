using System;
using UnityEngine;

namespace MapCustomization
{
   /// <summary>
   /// Represents changes applied to a placed prefab in map customization
   /// </summary>
   [Serializable]
   public struct PrefabChanges
   {
      #region Public Variables

      // Unique id of the prefab within the map
      public int id;

      // New localPosition, (-infinity, 0) if not changed
      [SerializeField]
      public Vector2 localPosition;

      #endregion

      public void clearLocalPosition () {
         localPosition = new Vector2(Mathf.NegativeInfinity, 0);
      }

      public bool isLocalPositionSet () {
         return localPosition.x != Mathf.NegativeInfinity;
      }

      #region Private Variables

      #endregion
   }
}