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
      if (Global.player != null && Global.player.userId != chatInfo.senderId) {
         PanelManager.self.contextMenuPanel.showDefaultMenuForUser(chatInfo.senderId, chatInfo.sender);
      }
   }

   public void chatLineHoverEnter () {
      if (Global.player != null && Global.player.userId != chatInfo.senderId) {
         text.alpha = .75f;
      }
   }

   public void chatLineHoverExit () {
      if (Global.player != null && Global.player.userId != chatInfo.senderId) {
         text.alpha = 1f;
      }
   }

   #region Private Variables

   #endregion
}
