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

      // Whether this prefab was created
      public bool created;

      // Serialization id of the prefab, as defined in asset serialization maps
      public int serializationId;

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
            localPosition = state.isLocalPositionSet() ? state.localPosition : localPosition,
            created = state.created || created,
            serializationId = Math.Max(state.serializationId, serializationId)
         };
      }

      public void clearAll () {
         clearLocalPosition();
         created = false;
      }

      public override string ToString () {
         return $"{ id }:{ serializationId } ({ (created ? "new" : "old") }): {localPosition}";
      }

      #region Private Variables

      #endregion
   }
}