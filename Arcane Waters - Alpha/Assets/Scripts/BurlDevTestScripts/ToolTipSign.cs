using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ToolTipSign : MonoBehaviour {
   #region Public Variables

   // The message upon hover
   public string toolTipMessage;

   // The tooltip panel
   public GameObject toolTipPanel;

   // The text of the tooltip
   public Text toolTipText;

   #endregion

   private void Awake () {
      toolTipText.text = toolTipMessage;
   }

   public void toggleToolTip (bool isActive) {
      toolTipPanel.SetActive(isActive);
   }

   #region Private Variables
      
   #endregion
}
