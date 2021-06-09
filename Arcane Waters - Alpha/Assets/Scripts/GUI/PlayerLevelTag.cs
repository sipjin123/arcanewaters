using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class PlayerLevelTag : MonoBehaviour {
   #region Public Variables

   // Reference to the UI element that displays the local player's level
   public TextMeshProUGUI content;

   #endregion

   public void setLevel(string level) {
      if (content != null) {
         content.text = level;
      }
   }

   public void toggle(bool show) {
      this.gameObject.SetActive(show);
   }

   #region Private Variables

   #endregion
}
