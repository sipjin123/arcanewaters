using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TutorialStepTrigger : ClientMonoBehaviour {
   #region Public Variables

   // The step we need to be on in order to activate this trigger
   public Step requiredTutorialStep;

   #endregion

   void OnTriggerEnter2D (Collider2D other) {
      NetEntity player = other.transform.GetComponent<NetEntity>();

      // Make sure it was our player that just entered
      if (player != null && player == Global.player && TutorialManager.currentStep == requiredTutorialStep) {
         Global.player.Cmd_CompletedTutorialStep(requiredTutorialStep);
      }
   }

   #region Private Variables

   #endregion
}
