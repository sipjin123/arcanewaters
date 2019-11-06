using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TooltipHandler : MonoBehaviour {
   #region Public Variables

   // Self
   public static TooltipHandler self;

   // Tool tip UI components
   public GameObject toolTipPanel;
   public GameObject toolTipPivot;
   public Text toolTipText;

   #endregion

   private void Awake () {
      self = this;
   }

   public void callToolTip (string msg, Vector2 coordinates) {
      toolTipPivot.transform.position = coordinates;

      toolTipText.text = msg;
      toolTipPanel.SetActive(true);
   }

   public void cancelToolTip() {
      toolTipPanel.SetActive(false);
   }

   #region Private Variables

   #endregion
}
