using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class GroupMemberArrow2 : ArrowIndicator {
   #region Public Variables

   // The text displays what the arrow is pointing at
   public TextMeshPro[] textIndicators;

   #endregion

   public void setTargetName (string targetName) {
      foreach (TextMeshPro textIndicator in textIndicators) {
         textIndicator.text = targetName;
      }
   }

   public void addTargetName (string targetName) {
      foreach (TextMeshPro textIndicator in textIndicators) {
         textIndicator.text += "\n" +targetName;
      }
   }

   #region Private Variables

   #endregion
}
