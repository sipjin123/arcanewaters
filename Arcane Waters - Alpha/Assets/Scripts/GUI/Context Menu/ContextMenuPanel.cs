using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Text;
using UnityEngine.Events;

public class ContextMenuPanel : MonoBehaviour
{
   #region Public Variables

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // The prefab we use for creating buttons
   public ContextMenuButton buttonPrefab;

   // The container for the buttons
   public GameObject buttonContainer;

   // The rect transform of the button zone
   public RectTransform buttonsRectTransform;

   // The title of the context menu (often the user name)
   public Text titleText;

   #endregion

   public void show(string title) {
      show(title, Input.mousePosition);
   }

   public void show (string title, Vector3 position) {
      this.gameObject.SetActive(true);
      this.canvasGroup.alpha = 1f;
      this.canvasGroup.blocksRaycasts = true;
      this.canvasGroup.interactable = true;

      transform.position = position;

      // Set the title
      if (string.IsNullOrEmpty(title)) {
         titleText.transform.parent.gameObject.SetActive(false);
      } else {
         titleText.transform.parent.gameObject.SetActive(true);
         titleText.text = title;
      }
   }

   public void hide () {
      this.canvasGroup.alpha = 0f;
      this.canvasGroup.blocksRaycasts = false;
      this.canvasGroup.interactable = false;
      this.gameObject.SetActive(false);
   }

   public bool isShowing () {
      return gameObject.activeSelf;
   }

   public void Update () {
      if (Global.player == null) {
         hide();
      }

      if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) {
         // Hide the menu if a mouse button is clicked and the pointer is not over any button
         if (!RectTransformUtility.RectangleContainsScreenPoint(buttonsRectTransform, Input.mousePosition)) {
            hide();
         }
      } else if (Input.anyKeyDown) {
         hide();
      }
   }

   public void clearButtons () {
      buttonContainer.DestroyChildren();
   }

   public void addButton (string text, UnityAction action) {
      ContextMenuButton button = Instantiate(buttonPrefab, buttonContainer.transform, false);
      button.initForAction(text, action);
   }

   public void showDefaultMenuForUser (int userId, string userName) {
      if (Global.player == null) {
         return;
      }

      // Try to find the entity of the clicked user
      NetEntity targetEntity = EntityManager.self.getEntity(userId);

      clearButtons();
      addButton("Info", () => Global.player.rpc.Cmd_RequestCharacterInfoFromServer(userId));
      
      if (Global.player.userId != userId) {
         if (VoyageManager.isInVoyage(Global.player)) {
            // If we can locally see the clicked user, only allow inviting if he is not already in the group
            if (targetEntity == null || (targetEntity != null && targetEntity.voyageGroupId != Global.player.voyageGroupId)) {
               PanelManager.self.contextMenuPanel.addButton("Group Invite", () => VoyageManager.self.invitePlayerToVoyageGroup(userName));
            }
         }
         PanelManager.self.contextMenuPanel.addButton("Friend Invite", () => FriendListManager.self.sendFriendshipInvite(userId, userName));

         // Only allow inviting to guild if we can locally see the invitee
         if (Global.player.guildId > 0 && targetEntity != null && targetEntity.guildId == 0) {
            PanelManager.self.contextMenuPanel.addButton("Guild Invite", () => Global.player.rpc.Cmd_InviteToGuild(userId));
         }

         PanelManager.self.contextMenuPanel.addButton("Message", () => ((MailPanel) PanelManager.self.get(Panel.Type.Mail)).composeMailTo(userName));
      }

      show(userName);
   }

   #region Private Variables

   #endregion
}