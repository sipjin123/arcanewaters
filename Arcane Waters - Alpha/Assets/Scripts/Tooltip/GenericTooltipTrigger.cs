﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class GenericTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
   #region Public Variables

   // The tooltip message
   public string message = "";

   // The rarity of the item.  Default set to common.
   public Rarity.Type itemRarityType = Rarity.Type.Common;

   // The tooltip type
   public ToolTipComponent.Type tooltipType;

   // The placement of the tooltip
   public ToolTipComponent.TooltipPlacement tooltipPlacement = ToolTipComponent.TooltipPlacement.AutoPlacement;

   // The max size of the tooltip
   public float maxWidth = 100;

   #endregion

   public void OnPointerEnter (PointerEventData eventData) {
      if (enabled && !Util.isEmpty(message)) {
         TooltipHandler.self.callToolTip(itemRarityType, gameObject, tooltipType, message, tooltipPlacement, transform.position, gameObject, maxWidth);
      }
   }

   public void OnPointerExit (PointerEventData eventData) {
      TooltipHandler.self.cancelToolTip();
   }

   #region Private Variables

   #endregion
}
