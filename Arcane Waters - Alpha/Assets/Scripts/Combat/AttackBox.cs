using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AttackBox : MonoBehaviour {
   #region Public Variables

   #endregion

   private void Start () {
      _entity = GetComponentInParent<SeaEntity>();
   }

   #region Private Variables

   // The associated Sea Entity
   protected SeaEntity _entity;

   #endregion
}
