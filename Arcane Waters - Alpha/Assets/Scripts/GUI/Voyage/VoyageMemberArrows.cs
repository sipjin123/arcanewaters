using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class VoyageMemberArrows : ArrowIndicator {
   #region Public Variables

   // The text displays what the arrow is pointing at
   public TextMeshPro[] textIndicators;

   #endregion

   public void setTargetName (string targetName) {
      foreach (TextMeshPro textIndicator in textIndicators) {
         textIndicator.text = targetName;
      }
   }

   #region Private Variables
      
   #endregion
}
