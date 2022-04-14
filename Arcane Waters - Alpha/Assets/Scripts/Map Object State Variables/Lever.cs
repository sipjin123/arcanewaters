using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Lever : VaryingStateObject
{
   #region Public Variables

   // The lever we rotate
   public Transform leverVisual;

   #endregion

   private void Update () {
      _rotation = Mathf.MoveTowards(_rotation, state.Equals("left") ? 45f : -45f, Time.deltaTime * 90);
      leverVisual.transform.localRotation = Quaternion.Euler(0, 0, _rotation);
   }

   public override bool clientTriesInteracting () {
      requestStateState(state.Equals("left") ? "right" : "left");
      return true;
   }

   #region Private Variables

   // Current rotation in degrees
   private float _rotation = 0;

   #endregion
}
