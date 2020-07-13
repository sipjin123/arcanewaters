using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class IdleCreatures : MonoBehaviour {
   #region Public Variables

   // Reference to the simple animation
   public SimpleAnimation simpleAnimation;

   #endregion

   private void Awake () {
      simpleAnimation = GetComponent<SimpleAnimation>();
   }

   #region Private Variables

   #endregion
}
