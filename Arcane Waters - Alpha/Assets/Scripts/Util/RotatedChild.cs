using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class RotatedChild : MonoBehaviour {
   #region Public Variables

   #endregion

   private void Start () {
      _entity = GetComponentInParent<SeaEntity>();
   }

   void Update () {
      this.transform.localRotation = Quaternion.AngleAxis(DirectionUtil.getAngle(_entity.facing), Vector3.forward);
   }

   #region Private Variables

   // The associated Sea Entity
   protected SeaEntity _entity;

   #endregion
}
