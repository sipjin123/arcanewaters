using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CombatCollider : MonoBehaviour {
   #region Public Variables

   #endregion

   private void Start () {
      _entity = GetComponentInParent<NetEntity>();
   }

   private void Update () {
      this.transform.rotation = Quaternion.Euler(0, 0, getRotationForFacingDirection());
   }

   protected int getRotationForFacingDirection () {
      switch (_entity.facing) {
         case Direction.South:
            return 0;
         case Direction.SouthEast:
            return 45;
         case Direction.East:
            return 90;
         case Direction.NorthEast:
            return 135;
         case Direction.North:
            return 180;
         case Direction.NorthWest:
            return 225;
         case Direction.West:
            return 270;
         case Direction.SouthWest:
            return 315;
      }

      return 0;
   }

   #region Private Variables

   // Our associated entity
   protected NetEntity _entity;
      
   #endregion
}
