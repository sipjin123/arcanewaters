using UnityEngine;
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

   public void showDefaultMenuForUser (int targetUserId, string userName, bool isInSameGroup = false) {
      if (Global.player == null) {
         D.debug("Missing Global Player");
         return;
      }

      // Try to find the entity of the clicked user
      NetEntity targetEntity = EntityManager.self.getEntity(targetUserId);
      if (targetEntity == null) {
         D.debug("Target entity is missing! User {" + targetUserId + ":" + userName + "}, Try broadcast across servers");
         Global.player.rpc.Cmd_ContextMenuRequest(targetUserId);
         return;
      }

      int targetGuildId = targetEntity.guildId;
      int targetVoyageGroupId = targetEntity.voyageGroupId;
      processDefaultMenuForUser(targetEntity, targetUserId, userName, targetGuildId, targetVoyageGroupId, isInSameGroup);
   }

   public void processDefaultMenuForUser (NetEntity targetEntity, int targetUserId, string userName, int targetGuildId, int targetVoyageGroupId, bool isInSameGroup = false, bool isInSameGuild = false) {
      clearButtons();
      if (Global.player.userId != targetUserId) {
         if (VoyageGroupManager.isInGroup(Global.player)) {
            // If we can locally see the clicked user, only allow inviting if he is not already in the group
            if ((targetEntity == null && !isInSameGroup) || (targetEntity != null && targetVoyageGroupId != Global.player.voyageGroupId)) {
               addButton("Group Invite", () => VoyageGroupManager.self.inviteUserToVoyageGroup(userName));
            }
            // If clicked user is already in the group and the player is group leader then allow kicking group members. Only make the button interactable if the player can be kicked from the group.
            else if (isInSameGroup && VoyageGroupPanel.self.isGroupLeader(Global.player.userId)) {
               if ((targetEntity != null && !targetEntity.hasAttackers()) || targetEntity == null) {
                  addButton("Kick player", () => VoyageGroupPanel.self.OnKickPlayerButtonClickedOn(targetUserId));
               }
            }
         } else {
            // If we are not in a group, always allow to invite someone
            addButton("Group Invite", () => VoyageGroupManager.self.inviteUserToVoyageGroup(userName));
         }

         if (targetEntity != null) {
            if (Global.player.guildId > 0 && targetGuildId > 0 && Global.player.guildRankPriority <= 1 
               && Global.player.guildId != targetGuildId && !Global.player.guildAllies.Contains(targetGuildId)) {
               addButton("Form Guild Alliance", () => Global.player.rpc.Cmd_AddGuildAlly(targetUserId, Global.player.guildId, targetGuildId));
            } else {
               D.adminLog("Not a guild ally because: {" + Global.player.guildId + "} {" + targetGuildId + "} {"
                  + Global.player.guildAllies.Contains(targetGuildId) + "} {" + Global.player.guildRankPriority + "}", D.ADMIN_LOG_TYPE.Player_Menu);
            }
         }

         if (!FriendListManager.self.isFriend(targetUserId)) {
            addButton("Friend Invite", () => FriendListManager.self.sendFriendshipInvite(targetUserId, userName));
         }

         if (FriendListManager.self.isFriend(targetUserId)) {
            addButton("Visit User", () => {
               VisitListPanel panel = (VisitListPanel) PanelManager.self.get(Panel.Type.VisitPanel);
               if (!panel.isShowing()) {
                  panel.refreshPanel();
               } else {
                  PanelManager.self.togglePanel(Panel.Type.VisitPanel);
               }
            });
         }

         if (Global.player.canInvitePracticeDuel(targetEntity)) {
            addButton("Practice Duel", () => initializePVP(targetUserId, userName));
         }

         // Only allow inviting to guild if we can locally see the invitee
         if (Global.player.canInviteGuild(targetEntity, isInSameGuild)) {
            addButton("Guild Invite", () => {
               D.debug("Successfully Sent Guild Invite using {method A} to user {" + targetUserId + "}");
               Global.player.rpc.Cmd_InviteToGuild(targetUserId, userName);
            });
         } else {
            string targetEntityInfo = (targetEntity == null ? "Null" : (targetEntity.entityName + ":" + targetEntity.userId));
            if (Global.player.guildId < 1 || (targetEntity != null && targetEntity.guildId < 1)) {
               D.debug("GuildInviteFailed! Invalid Guild parameters! {" + targetEntityInfo + "}");
            } else if (targetEntity != null && targetEntity.canPerformAction(GuildPermission.Invite)) {
               D.debug("GuildInviteFailed! No permission to invite! {" + targetEntityInfo + "}");
            } else {
               D.debug("Cant invite the user to guild, all conditions failed!");
            }
         }

         addButton("Whisper", () => ChatPanel.self.sendWhisperTo(userName));
         addButton("Message", () => ((MailPanel) PanelManager.self.get(Panel.Type.Mail)).composeMailTo(userName));

         if (Global.player.isAdmin()) {
            addButton("Mute 5 min", () => Global.player.admin.requestMutePlayerWithConfirmation(userName, 5 * 60, ""));
            addButton("Ban 5 min", () => Global.player.admin.requestBanPlayerWithConfirmation(userName, 5 * 60, ""));
         }
      } else {
         if (!VoyageGroupManager.isInGroup(Global.player)) {
            addButton("Create Group", () => VoyageGroupManager.self.requestPrivateGroupCreation());
         } else {
            // If the player is in a group and clicks their own avatar, allow them to leave the group. Only make the button interactable if the player can leave the group.
            addButton("Leave Group", () => VoyageGroupPanel.self.OnLeaveGroupButtonClickedOn(), () => !Global.player.hasAttackers() && !PanelManager.self.countdownScreen.isShowing());
         }

         addButton("Copy Location", () => {
            WorldMapSpot spot = WorldMapManager.self.getSpotFromPosition(Global.player.areaKey, Global.player.transform.localPosition);

            if (spot != null) {
               WorldMapGeoCoords geoCoords = WorldMapManager.self.getGeoCoordsFromSpot(spot);
               string geoCoordsStr = WorldMapManager.self.getStringFromGeoCoords(geoCoords);
               ChatPanel.self.inputField.insertText(geoCoordsStr);

               if (PanelManager.self != null) {
                  PanelManager.self.noticeScreen.show("Location copied!");
               }
            }
         });
      }

      addButton("Player Info", () => ((CharacterInfoPanel) PanelManager.self.get(Panel.Type.CharacterInfo)).refreshPanel(targetUserId));
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