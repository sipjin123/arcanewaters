using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class ToolTipSign : MonoBehaviour {
   #region Public Variables

   // The message upon hover
   public string toolTipMessage;

   // The tooltip panel
   public GameObject toolTipPanel;

   // The text of the tooltip
   public TextMeshProUGUI toolTipText;

   #endregion

   protected virtual void Awake () {
      toolTipText.SetText(toolTipMessage);
   }

   public virtual void toggleToolTip (bool isActive) {
      toolTipPanel.SetActive(isActive);
   }

   #region Private Variables
      
   #endregion
}
