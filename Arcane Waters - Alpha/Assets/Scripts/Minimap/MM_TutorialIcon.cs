using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MM_TutorialIcon : MM_Icon {
   #region Public Variables

   // The tutorial step that this icon is for
   public Step tutorialStepType;

   #endregion

   public override bool shouldShowIcon () {
      // These icons are only visible when they match our current step
      return (TutorialManager.currentStep == this.tutorialStepType);
   }

   #region Private Variables

   #endregion
}
