using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using UnityEngine.EventSystems;

public class SpeakChatLine : ChatLine {
   #region Public Variables

   // The Text component that we manage
   public TextMeshProUGUI text;

   #endregion

   public override void OnPointerClick (PointerEventData eventData) {
      if (isValidInteraction()) {
         PanelManager.self.contextMenuPanel.showDefaultMenuForUser(chatInfo.senderId, chatInfo.sender);
      }
   }

   public void chatLineHoverEnter () {
      if (isValidInteraction()) {
         text.alpha = .75f;
      }
   }

   public void chatLineHoverExit () {
      if (isValidInteraction()) {
         text.alpha = 1f;
      }
   }

   private bool isValidInteraction () {
      return Global.player != null && Global.player.userId != chatInfo.senderId && chatInfo.senderId > 0;
   }

   #region Private Variables

   #endregion
}
