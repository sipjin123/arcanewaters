using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class SpeakChatLine : ChatLine, IScrollHandler
{
   #region Public Variables

   // The category of fragment 
   public enum LineFragmentType { None = 0, Text = 1, ItemInsert = 2 };

   // The reference to the text mesh
   public TextMeshProUGUI textMeshReference;

   // The prefabfor displaying item inserts
   public HoverableItemIcon itemInsertPrefab = null;

   #endregion

   private void Start () {
      if (textMeshReference) {
         textMeshReference.fontSize = ChatManager.self.chatFontSize;
      }
   }

   public void setFormattedText (string text) {
      _formattedText = text;

      if (_textMesh == null) {
         _textMesh = GetComponent<TextMeshProUGUI>();
      }

      _textMesh.text = ChatManager.injectItemSnippetLinks(text, out int itemTagCount);

      if (gameObject.activeInHierarchy) {
         StartCoroutine(CO_AddItemSnippetsAfterDelay());
      }
   }

   private IEnumerator CO_AddItemSnippetsAfterDelay () {
      yield return new WaitForEndOfFrame();

      for (int i = 0; i < _textMesh.textInfo.linkCount; i++) {
         string linkId = _textMesh.textInfo.linkInfo[i].GetLinkID();
         if (linkId.StartsWith(ChatManager.ITEM_INSERT_ID_PREFIX)) {
            if (int.TryParse(linkId.Replace(ChatManager.ITEM_INSERT_ID_PREFIX, ""), out int itemId)) {
               int firstCharIndex = _textMesh.textInfo.linkInfo[i].linkTextfirstCharacterIndex;
               int lastCharIndex = firstCharIndex + ChatManager.ITEM_INSERT_TEXT_PLACEHOLDER.Length - 1;

               Vector2 center =
                  (_textMesh.textInfo.characterInfo[firstCharIndex].bottomLeft +
                  _textMesh.textInfo.characterInfo[lastCharIndex].topRight) / 2f;

               HoverableItemIcon itemInsert = Instantiate(itemInsertPrefab, transform);
               itemInsert.GetComponent<RectTransform>().anchoredPosition = center;
               itemInsert.setItemId(itemId);
            }
         }
      }
   }

   public string getFormattedText () {
      return _formattedText;
   }

   private void setAlpha (float alpha) {
      _textMesh.alpha = alpha;
   }

   public override void OnPointerClick (PointerEventData eventData) {
      if (isValidInteraction()) {
         if (chatInfo.messageType == ChatInfo.Type.PvpAnnouncement) {
            ((PvpArenaPanel) PanelManager.self.get(Panel.Type.PvpArena)).togglePanel();
         } else if (chatInfo.messageType == ChatInfo.Type.PendingFriendRequestsNotification) {
            if (!PanelManager.self.get(Panel.Type.FriendList).isShowing()) {
               BottomBar.self.toggleFriendListPanelAtTab(FriendListPanel.FriendshipPanelTabs.InvitesReceived);
            }
         } else {
            D.adminLog("ContextMenu: Interact was performed via speak line button CMD-1: " +
               "{" + Global.player.userId + ":" + Global.player.entityName + "}{" + chatInfo.senderId + ":" + chatInfo.sender + "}", D.ADMIN_LOG_TYPE.Player_Menu);
            PanelManager.self.contextMenuPanel.showDefaultMenuForUser(chatInfo.senderId, chatInfo.sender);
         }
      }
   }

   public void chatlineButtonClick () {
      if (isValidInteraction()) {
         if (chatInfo.messageType == ChatInfo.Type.PvpAnnouncement) {
            ((PvpArenaPanel) PanelManager.self.get(Panel.Type.PvpArena)).togglePanel();
         } else if (chatInfo.messageType == ChatInfo.Type.PendingFriendRequestsNotification) {
            if (!PanelManager.self.get(Panel.Type.FriendList).isShowing()) {
               BottomBar.self.toggleFriendListPanelAtTab(FriendListPanel.FriendshipPanelTabs.InvitesReceived);
            }
         } else {
            D.adminLog("ContextMenu: Interact was performed via chat line button CMD-2:" +
            "{" + Global.player.userId + ":" + Global.player.entityName + "}{" + chatInfo.senderId + ":" + chatInfo.sender + "}", D.ADMIN_LOG_TYPE.Player_Menu);
            PanelManager.self.contextMenuPanel.showDefaultMenuForUser(chatInfo.senderId, chatInfo.sender);
         }
      }
   }

   public void OnScroll (PointerEventData eventData) {
      ChatPanel.self.scrollRect.OnScroll(eventData);
   }

   public void chatLineHoverEnter () {
      if (isValidInteraction()) {
         setAlpha(.75f);
      }
   }

   public void chatLineHoverExit () {
      if (isValidInteraction()) {
         setAlpha(1f);
      }
   }

   public bool isValidInteraction () {
      if (chatInfo.messageType == ChatInfo.Type.PvpAnnouncement || chatInfo.messageType == ChatInfo.Type.PendingFriendRequestsNotification) {
         return true;
      }

      return Global.player != null && Global.player.userId != chatInfo.senderId && chatInfo.senderId > 0;
   }

   #region Private Variables

   // The text assigned to this chatline, including attribute tags, custom data tags, etc.
   private string _formattedText = "";

   // Component that displays the text
   private TextMeshProUGUI _textMesh = null;

   #endregion
}
