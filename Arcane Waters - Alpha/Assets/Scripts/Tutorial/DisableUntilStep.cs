using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class DisableUntilStep : ClientMonoBehaviour {
   #region Public Variables

   // The tutorial step that first enables this
   public Step disabledUntilThisStep;

   // The collider to disable
   public Collider2D colliderToDisable;

   #endregion

   void Update () {
      // Check if the collider should be disabled because the player hasn't gotten far enough in the tutorial
      bool disabled = (int) TutorialManager.currentStep < (int) disabledUntilThisStep;

      // Toggle the script accordingly
      colliderToDisable.enabled = !disabled;
   }

   #region Private Variables

   #endregion
}
