using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Lever : VaryingStateObject
{
   #region Public Variables

   // The animator of the lever
   public SpriteRendererAnimator leverAnim;

   #endregion

   private void Update () {
      leverAnim.playBackwards = state.Equals("left");
   }

   protected override void clientInteract () {
      requestStateState(state.Equals("left") ? "right" : "left");
   }

   #region Private Variables

   // Current rotation in degrees
   private float _rotation = 0;

   #endregion
}
