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

   // The prefab we use for creating a row with the 'empty' label
   public GameObject emptyRowPrefab;

   // The prefab we use for creating a title row
   public GameObject titleRowPrefab;

   // The container for the buttons
   public GameObject buttonContainer;

   // The rect transform of the button zone
   public RectTransform buttonsRectTransform;

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
      if (!string.IsNullOrEmpty(title)) {
         GameObject titleRow = Instantiate(titleRowPrefab, buttonContainer.transform, false);
         titleRow.transform.SetAsFirstSibling();
         titleRow.GetComponentInChildren<Text>().text = title;
      }

      // If there are no buttons, add a row with the 'empty' label
      if (!_hasAtLeastOneButton) {
         Instantiate(emptyRowPrefab, buttonContainer.transform, false);
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

      if (InputManager.isLeftClickKeyPressed() || InputManager.isRightClickKeyPressed()) {
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
      _hasAtLeastOneButton = false;
   }

   public void addButton (string text, UnityAction action) {
      ContextMenuButton button = Instantiate(buttonPrefab, buttonContainer.transform, false);
      button.initForAction(text, action);
      _hasAtLeastOneButton = true;
   }

   public void showDefaultMenuForUser (int userId, string userName) {
      if (Global.player == null) {
         return;
      }

      // Try to find the entity of the clicked user
      NetEntity targetEntity = EntityManager.self.getEntity(userId);

      clearButtons();
      
      if (Global.player.userId != userId) {
         if (VoyageGroupManager.isInGroup(Global.player)) {
            // If we can locally see the clicked user, only allow inviting if he is not already in the group
            if (targetEntity == null || (targetEntity != null && targetEntity.voyageGroupId != Global.player.voyageGroupId)) {
               addButton("Group Invite", () => VoyageGroupManager.self.inviteUserToVoyageGroup(userName));
            }
         } else {
            // If we are not in a group, always allow to invite someone
            addButton("Group Invite", () => VoyageGroupManager.self.inviteUserToVoyageGroup(userName));
         }
         addButton("Friend Invite", () => FriendListManager.self.sendFriendshipInvite(userId, userName));

         // Only allow inviting to guild if we can locally see the invitee
         if (Global.player.guildId > 0 && targetEntity != null && targetEntity.guildId == 0) {
            addButton("Guild Invite", () => Global.player.rpc.Cmd_InviteToGuild(userId));
         }

         addButton("Message", () => ((MailPanel) PanelManager.self.get(Panel.Type.Mail)).composeMailTo(userName));
      } else if (!VoyageGroupManager.isInGroup(Global.player)) {
         addButton("Create Group", () => VoyageGroupManager.self.requestPrivateGroupCreation());
      }

      show(userName);
   }

   #region Private Variables

   // Gets set to true when the context menu has at least one button
   private bool _hasAtLeastOneButton = false;

   #endregion
}