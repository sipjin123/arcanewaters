﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.InputSystem;

public class VoyageGroupPanel : ClientMonoBehaviour
{
   #region Public Variables

   // Our Canvas Group
   public CanvasGroup canvasGroup;

   // The container for the group member rows
   public GameObject memberContainer;

   // The prefab we use for creating member cells
   public VoyageGroupMemberCell memberCellPrefab;

   // The prefab we use to instantiate group member arrows
   public VoyageGroupMemberArrow groupMemberArrowPrefab;

   // When the mouse is over this defined zone, we consider that it hovers the panel
   public RectTransform panelHoveringZone;

   // The exit button
   public Button xButton;

   // The prefab for the bottom of the group members column
   public GameObject columnBottomPrefab;

   // Self
   public static VoyageGroupPanel self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   public void Start () {
      // Hide the panel by default
      hide();

      // Hide the x button
      xButton.gameObject.SetActive(false);

      // Clear out any info
      memberContainer.DestroyAllChildrenExcept(panelHoveringZone.gameObject);
      _memberCells.Clear();
   }

   public void Update () {
      // Hide the panel when there is no player or he doesn't belong to a group
      if (!VoyageGroupManager.isInGroup(Global.player) || _memberCells.Count <= 0) {
         hide();
         return;
      }

      // Show the panel
      show();

      // Display the X button only if the mouse is over the defined zone
      if (RectTransformUtility.RectangleContainsScreenPoint(panelHoveringZone, MouseUtils.mousePosition)) {
         xButton.gameObject.SetActive(true);
      } else {
         xButton.gameObject.SetActive(false);
      }
   }

   public void updatePanelWithGroupMembers (VoyageGroupMemberCellInfo[] groupMembers) {
      // Clear out any old info
      memberContainer.DestroyAllChildrenExcept(panelHoveringZone.gameObject);
      _memberCells.Clear();
      VoyageGroupManager.self.groupMemberArrowContainer.DestroyChildren();
      _memberArrows.Clear();

      foreach (VoyageGroupMemberCellInfo cellInfo in groupMembers) {
         // Instantiate the cell
         VoyageGroupMemberCell cell = Instantiate(memberCellPrefab, memberContainer.transform, false);
         cell.setCellForGroupMember(cellInfo);
         _memberCells.Add(cell);

         // Instantiate the arrow
         VoyageGroupMemberArrow arrow = Instantiate(groupMemberArrowPrefab, VoyageGroupManager.self.groupMemberArrowContainer.transform, false);
         arrow.setTarget(cell, cellInfo.userId, cellInfo.userName);
         _memberArrows.Add(arrow);
      }

      Instantiate(columnBottomPrefab, memberContainer.transform, false);
   }

   public void updateCellTooltip (int userId, string userName, int XP, string areaKey) {
      // Search for the cell
      foreach (VoyageGroupMemberCell cell in _memberCells) {
         if (cell.getUserId() == userId) {
            cell.updateTooltip(userName, XP, areaKey);
         }
      }
   }

   public void OnKickPlayerButtonClickedOn (int playerToKick) {
      PanelManager.self.showConfirmationPanel("Are you sure you want to kick player from the voyage group?",
         () => {
            // Request the server to remove the user from the group
            if (Global.player != null) {
               Global.player.rpc.Cmd_KickUserFromGroup(playerToKick);
            }
         });
   }

   public void cleanUpPanelOnKick () {
      // Hide the countdown
      PanelManager.self.countdownScreen.hide();

      // Close the current voyage panel if it is open
      if (PanelManager.self.get(Panel.Type.ReturnToCurrentVoyagePanel).isShowing()) {
         PanelManager.self.unlinkPanel();
      }
   }

   public void notifyMember (VoyageGroupMemberCell.NotificationType notifType, int memberId) {
      VoyageGroupMemberCell memberCell = _memberCells.Find(_ => _.getUserId() == memberId);
      if (memberCell != null) {
         memberCell.triggerNotification(notifType);
      }
   }

   public bool isGroupLeader (int userId) {
      if (_memberCells != null && _memberCells.Count > 0) {
         return _memberCells[0].getUserId() == userId;
      }

      return false;
   }

   public void OnLeaveGroupButtonClickedOn () {
      // Check if the player is already leaving the group or if it is in combat
      if (PanelManager.self.countdownScreen.isShowing() || Global.player.hasAttackers()) {
         return;
      }

      if (!Global.player.isGhost && (VoyageManager.isTreasureSiteArea(Global.player.areaKey) || VoyageManager.isVoyageOrLeagueArea(Global.player.areaKey))) {
         // Outside of safe areas, start a leave countdown
         PanelManager.self.countdownScreen.cancelButton.onClick.RemoveAllListeners();
         PanelManager.self.countdownScreen.onCountdownEndEvent.RemoveAllListeners();
         PanelManager.self.countdownScreen.cancelButton.onClick.AddListener(() => PanelManager.self.countdownScreen.hide());
         PanelManager.self.countdownScreen.onCountdownEndEvent.AddListener(() => onLeaveCountdownEnd());
         PanelManager.self.countdownScreen.customText.text = "Leaving Voyage Group in";

         // Start the countdown
         PanelManager.self.countdownScreen.seconds = LEAVING_COUNTDOWN_SECONDS;
         PanelManager.self.countdownScreen.show();
      } else {
         // In safe areas or in ghost mode, ask confirmation and leave without countdown
         PanelManager.self.showConfirmationPanel("Are you sure you want to leave the voyage group?", () => onLeaveCountdownEnd());
      }

      // Close the current voyage panel if it is open
      if (PanelManager.self.get(Panel.Type.ReturnToCurrentVoyagePanel).isShowing()) {
         PanelManager.self.unlinkPanel();
      }
   }

   public void onLeaveCountdownEnd () {
      // Hide the countdown
      PanelManager.self.countdownScreen.hide();

      // Trigger the tutorial
      TutorialManager3.self.tryCompletingStep(TutorialTrigger.LeaveVoyageGroup);

      // Request the server to remove the user from the group
      Global.player.rpc.Cmd_RemoveUserFromGroup();
   }

   public bool isMouseOverMemberCell (int memberUserId) {
      if (!isShowing()) {
         return false;
      }

      foreach (VoyageGroupMemberCell cell in _memberCells) {
         if (cell.isMouseOver() && cell.getUserId() == memberUserId) {
            return true;
         }
      }

      return false;
   }

   public bool isMouseOverAnyMemberCell () {
      if (!isShowing()) {
         return false;
      }

      foreach (VoyageGroupMemberCell cell in _memberCells) {
         if (cell.isMouseOver()) {
            return true;
         }
      }

      return false;
   }

   public void show () {
      if (!canvasGroup.IsShowing()) {
         canvasGroup.Show();
      }

      foreach (VoyageGroupMemberArrow arrow in _memberArrows) {
         arrow.activate();
      }
   }

   public void hide () {
      if (canvasGroup.IsShowing()) {
         canvasGroup.Hide();
      }

      foreach (VoyageGroupMemberArrow arrow in _memberArrows) {
         arrow.deactivate();
      }
   }

   public bool isShowing () {
      return this.gameObject.activeSelf && canvasGroup.alpha > 0f;
   }

   #region Private Variables

   // The number of seconds the player must wait when leaving the group
   private static int LEAVING_COUNTDOWN_SECONDS = 5;

   // The countdown when leaving the group
   private float _countdown = 0;

   // The list of member cells
   private List<VoyageGroupMemberCell> _memberCells = new List<VoyageGroupMemberCell>();

   // The list of group member arrows used to indicate the members location
   private List<VoyageGroupMemberArrow> _memberArrows = new List<VoyageGroupMemberArrow>();

   #endregion
}
