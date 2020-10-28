using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class TooltipHandler : MonoBehaviour {
   #region Public Variables

   // Self
   public static TooltipHandler self;

   // Tool tip UI components
   public GameObject toolTipPanel;
   public TextMeshProUGUI toolTipTMPText;
   public RectTransform backgroundRect;
   public float offSetY = 50;

   #endregion

   private void Awake () {
      self = this;
   }

   public void callToolTip (string msg, Vector2 coordinates) {
      if (toolTipTMPText) {
         toolTipTMPText.transform.gameObject.SetActive(true);
         toolTipTMPText.SetText(msg);
         toolTipTMPText.ForceMeshUpdate();
      }
      if (backgroundRect) {
         backgroundRect.transform.gameObject.SetActive(true);

         // Set position of tooltip
         backgroundRect.transform.position = new Vector2(coordinates.x, coordinates.y + offSetY);
      }
   }

   public void cancelToolTip() {
      if (toolTipTMPText) {
         toolTipTMPText.transform.gameObject.SetActive(false);
      }
      if (backgroundRect) {
         backgroundRect.transform.gameObject.SetActive(false);
      }
   }

   #region Private Variables

   #endregion
}
