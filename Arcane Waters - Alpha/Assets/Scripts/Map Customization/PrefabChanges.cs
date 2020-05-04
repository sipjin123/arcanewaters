using System;
using UnityEngine;

namespace MapCustomization
{
   /// <summary>
   /// Represents changes applied to a placed prefab in map customization
   /// </summary>
   [System.Serializable]
   public struct PrefabChanges
   {
      #region Public Variables

      #endregion

      public Vector2? localPosition
      {
         get
         {
            if (_localPosition.x == Mathf.NegativeInfinity) {
               return null;
            } else {
               return _localPosition;
            }
         }
         set
         {
            if (value == null) {
               _localPosition = new Vector2(Mathf.NegativeInfinity, 0);
            } else {
               _localPosition = value.Value;
            }
         }
      }

      #region Private Variables

      // New localPosition, (-infinity, 0) if not changed
      [SerializeField]
      private Vector2 _localPosition;

      #endregion
   }
}