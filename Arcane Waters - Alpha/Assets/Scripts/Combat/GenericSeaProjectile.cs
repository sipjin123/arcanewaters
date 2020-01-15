using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class GenericSeaProjectile : MonoBehaviour {
   #region Public Variables

   #endregion

   public void init (float startTime, float endTime, Vector2 startPos, Vector2 endPos, SeaEntity creator, Attack.ImpactMagnitude impactMagnitude, GameObject targetObj = null) {
      if (targetObj != null) {
         _targetObject = targetObj;
      }

      this._impactMagnitude = impactMagnitude;

      _startTime = startTime;
      _endTime = endTime;

      _startPos = startPos;
      _endPos = endPos;

      _creator = creator;
   }

   #region Private Variables

   // The creator of this Attack Circle
   protected SeaEntity _creator;

   // The target of the attack, if any
   protected GameObject _targetObject;

   // Our Start Point
   protected Vector2 _startPos;

   // Our End Point
   protected Vector2 _endPos;

   // Our Start Time
   protected float _startTime;

   // Our End Time
   protected float _endTime;

   // Determines the impact level of this projectile
   protected Attack.ImpactMagnitude _impactMagnitude = Attack.ImpactMagnitude.None;

   #endregion
}
