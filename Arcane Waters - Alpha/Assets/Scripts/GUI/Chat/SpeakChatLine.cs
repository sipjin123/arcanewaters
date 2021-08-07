using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using UnityEngine.EventSystems;

public class SpeakChatLine : ChatLine, IScrollHandler
{
   #region Public Variables

   // The Text component that we manage
   public TextMeshProUGUI text;

   #endregion

   public override void OnPointerClick (PointerEventData eventData) {
      if (isValidInteraction()) {
         if (chatInfo.messageType == ChatInfo.Type.PvpAnnouncement) {
            ((PvpArenaPanel) PanelManager.self.get(Panel.Type.PvpArena)).togglePanel();
         } else {
            PanelManager.self.contextMenuPanel.showDefaultMenuForUser(chatInfo.senderId, chatInfo.sender);
         }
      }
   }

   public void OnScroll (PointerEventData eventData) {
      ChatPanel.self.scrollRect.OnScroll(eventData);
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

   public bool isValidInteraction () {
      if (chatInfo.messageType == ChatInfo.Type.PvpAnnouncement) {
         return true;
      }
      
      return Global.player != null && Global.player.userId != chatInfo.senderId && chatInfo.senderId > 0;
   }

   #region Private Variables

   #endregion
}
