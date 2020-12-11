using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;

public class TargetCircle : MonoBehaviour {
   #region Public Variables

   // Reference to the dotted circle that represents the outline of the circle
   public DottedCircle circleOutline;

   // Reference to the sprite that represents the center of the circle
   public SpriteRenderer circleCenter;

   #endregion

   private void Update () {
      updateCircle();
   }

   public void updateCircle () {
      transform.position = Util.getMousePos();
   }

   #region Private Variables

   #endregion
}
