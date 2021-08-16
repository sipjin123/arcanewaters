﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Text;
using UnityEngine.Events;
using UnityEngine.InputSystem;

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

   // Padding from the edge of the screen
   public float PADDING = 30F;

   #endregion

   public void show(string title) {
      show(title, MouseUtils.mousePosition);
   }

   public void show (string title, Vector3 position) {
      this.gameObject.SetActive(true);
      this.canvasGroup.alpha = 0f;
      this.canvasGroup.blocksRaycasts = false;
      this.canvasGroup.interactable = false;

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

      StopAllCoroutines();
      StartCoroutine(CO_ClampToScreenBoundsAndShow());
   }

   private IEnumerator CO_ClampToScreenBoundsAndShow () {
      // Wait for the panel buttons to be instantiated
      yield return null;

      // Get the panel size
      Vector2 panelSize = buttonsRectTransform.sizeDelta;

      // Check if the panel is out of the screen and move it if needed
      if (transform.position.x + panelSize.x > Screen.width) {
         transform.position = new Vector3(transform.position.x - panelSize.x - PADDING, transform.position.y, transform.position.z);
      }

      if (transform.position.y - panelSize.y < 0) {
         transform.position = new Vector3(transform.position.x, transform.position.y + panelSize.y + PADDING, transform.position.z);
      }

      this.canvasGroup.alpha = 1f;
      this.canvasGroup.blocksRaycasts = true;
      this.canvasGroup.interactable = true;
   }

   public void hide () {
      StopAllCoroutines();
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

      if (Util.isBatch()) {
         return;
      }

      if (KeyUtils.GetButtonDown(MouseButton.Left) || KeyUtils.GetButtonDown(MouseButton.Right)) {
         // Hide the menu if a mouse button is clicked and the pointer is not over any button
         if (!RectTransformUtility.RectangleContainsScreenPoint(buttonsRectTransform, MouseUtils.mousePosition)) {
            hide();
         }
      } else if (Keyboard.current.anyKey.isPressed) {
         hide();
      }
   }

   public void clearButtons () {
      buttonContainer.DestroyChildren();
      _hasAtLeastOneButton = false;
   }

   public void addButton (string text, UnityAction action, ContextMenuButton.ContextMenuCondition enableButtonCondition = null) {
      ContextMenuButton button = Instantiate(buttonPrefab, buttonContainer.transform, false);
      button.initForAction(text, action, enableButtonCondition);
      _hasAtLeastOneButton = true;
   }

   public void showDefaultMenuForUser (int userId, string userName, bool isInSameGroup = false) {
      if (Global.player == null) {
         return;
      }

      // Try to find the entity of the clicked user
      NetEntity targetEntity = EntityManager.self.getEntity(userId);

      clearButtons();
      
      if (Global.player.userId != userId) {
         if (VoyageGroupManager.isInGroup(Global.player)) {
            // If we can locally see the clicked user, only allow inviting if he is not already in the group
            if ((targetEntity == null && !isInSameGroup) || (targetEntity != null && targetEntity.voyageGroupId != Global.player.voyageGroupId)) {
               addButton("Group Invite", () => VoyageGroupManager.self.inviteUserToVoyageGroup(userName));
            }
            // If clicked user is already in the group and the player is group leader then allow kicking group members. Only make the button interactable if the player can be kicked from the group.
            else if (isInSameGroup && VoyageGroupPanel.self.isGroupLeader(Global.player.userId)) {
               if ((targetEntity && !targetEntity.hasAttackers()) || !targetEntity) {
                  addButton("Kick player", () => VoyageGroupPanel.self.OnKickPlayerButtonClickedOn(userId));
               }
            }
         } else {
            // If we are not in a group, always allow to invite someone
            addButton("Group Invite", () => VoyageGroupManager.self.inviteUserToVoyageGroup(userName));
         }

         if (!FriendListManager.self.isFriend(userId)) {
            addButton("Friend Invite", () => FriendListManager.self.sendFriendshipInvite(userId, userName));
         }

         if (Global.player.canInvitePracticeDuel(targetEntity)) {
            addButton("Practice Duel", () => initializePVP(userId, userName));
         }

         // Only allow inviting to guild if we can locally see the invitee
         if (Global.player.canInviteGuild(targetEntity)) {
            addButton("Guild Invite", () => Global.player.rpc.Cmd_InviteToGuild(userId));
         }

         addButton("Whisper", () => ChatPanel.self.sendWhisperTo(userName));
         addButton("Message", () => ((MailPanel) PanelManager.self.get(Panel.Type.Mail)).composeMailTo(userName));
      } else if (!VoyageGroupManager.isInGroup(Global.player)) {
         addButton("Create Group", () => VoyageGroupManager.self.requestPrivateGroupCreation());
      } else {
         // If the player is in a group and clicks their own avatar, allow them to leave the group. Only make the button interactable if the player can leave the group.
         addButton("Leave Group", () => VoyageGroupPanel.self.OnLeaveGroupButtonClickedOn(), () => !Global.player.hasAttackers() && !PanelManager.self.countdownScreen.isShowing());
      }

      addButton("Player Info", () => ((CharacterInfoPanel) PanelManager.self.get(Panel.Type.CharacterInfo)).refreshPanel(targetEntity.userId));
      show(userName);
   }

   private void initializePVP (int userId, string userName) {
      ChatManager.self.addChat("Practice duel invite sent to " + userName, ChatInfo.Type.System);
      Global.player.rpc.Cmd_InviteToPvp(userId);
   }

   #region Private Variables

   // Gets set to true when the context menu has at least one button
   private bool _hasAtLeastOneButton = false;

   #endregion
}