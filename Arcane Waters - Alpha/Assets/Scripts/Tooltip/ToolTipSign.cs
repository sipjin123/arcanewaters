using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using UnityEngine.EventSystems;

public class ToolTipSign : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
   #region Public Variables

   // The message upon hover
   public string toolTipMessage;

   // The tooltip panel
   public GameObject toolTipPanel;

   // The text of the tooltip
   public TextMeshProUGUI toolTipText;

   // Distance limit to determine if player is close enough to display tooltip
   public float maxDistanceForTooltip = 0.4f;

   // Is mouse pointer over the sign
   public bool pointerIsHovering;

   #endregion

   protected virtual void Awake () {
      toolTipText.SetText(toolTipMessage);
   }

   public virtual void toggleToolTip (bool isActive) {
      toolTipPanel.SetActive(isActive);
   }

   private void Update () {
      if (Global.player == null) {
         return;
      }

      if ((Vector2.Distance(this.transform.position, Global.player.transform.position) < maxDistanceForTooltip) || pointerIsHovering) {
         toggleToolTip(true);
      } else {
         toggleToolTip(false);
      }
   }

   public void OnPointerEnter (PointerEventData eventData) {
      pointerIsHovering = true;
   }

   public void OnPointerExit (PointerEventData eventData) {
      pointerIsHovering = false;
   }

   #region Private Variables

   #endregion
}
