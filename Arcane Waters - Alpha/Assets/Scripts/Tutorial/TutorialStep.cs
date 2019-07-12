using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TutorialStep {
   #region Public Variables

   // The instruction text displayed in the title bar
   public string titleText;

   // Some detailed instructions on what to do to complete this step
   public string detailsText;

   #endregion

   public TutorialStep(string titleText, string detailsText) {
      this.titleText = titleText;
      this.detailsText = detailsText;
   }

   #region Private Variables

   #endregion
}
