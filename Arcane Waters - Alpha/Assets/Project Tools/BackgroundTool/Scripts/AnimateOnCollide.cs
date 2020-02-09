using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AnimateOnCollide : MonoBehaviour {
   #region Public Variables

   // Reference to simple animation
   public SimpleAnimation simpleAnimation;

   #endregion

   private void OnTriggerEnter2D (Collider2D collision) {
      simpleAnimation.resetAnimation();
   }

   #region Private Variables

   #endregion
}
