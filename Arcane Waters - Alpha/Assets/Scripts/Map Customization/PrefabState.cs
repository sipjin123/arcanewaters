using System;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace MapCustomization
{
   /// <summary>
   /// Represents state of a prefab, set by map customization
   /// </summary>
   [Serializable]
   public struct PrefabState
   {
      #region Public Variables

      // Unique id of the prefab within the map
      public int id;

      // New localPosition, (-infinity, 0) if not set
      [SerializeField]
      public Vector2 localPosition;

      #endregion

      public void clearLocalPosition () {
         localPosition = new Vector2(Mathf.NegativeInfinity, 0);
      }

      public bool isLocalPositionSet () {
         return localPosition.x != Mathf.NegativeInfinity;
      }

      public PrefabState add (PrefabState state) {
         return new PrefabState {
            id = id,
            localPosition = state.isLocalPositionSet() ? state.localPosition : localPosition
         };
      }

      public void clearAll () {
         clearLocalPosition();
      }

      public override string ToString () {
         return $"{id}: {localPosition} ";
      }

      #region Private Variables

      #endregion
   }
}