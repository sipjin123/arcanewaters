using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.InputSystem;
using System.Linq;

public class GroupPanel : ClientMonoBehaviour
{
   #region Public Variables

   // Our Canvas Group
   public CanvasGroup canvasGroup;

   // The container for the group member rows
   public GameObject memberContainer;

   // The prefab we use for creating member cells
   public GroupMemberCell memberCellPrefab;

   // The prefab we use to instantiate group member arrows
   public GroupMemberArrow groupMemberArrowPrefab;

   // When the mouse is over this defined zone, we consider that it hovers the panel
   public RectTransform panelHoveringZone;

   // The exit button
   public Button xButton;

   // The prefab for the bottom of the group members column
   public GameObject columnBottomPrefab;

   // Self
   public static GroupPanel self;

   // List of arrow indicators
   public List<GroupMemberArrow2> directionalArrowList;

   // Distance before the indicator will be hidden, means its too near to the player already
   public const float CLAMP_HIDE_DISTANCE = 1.5f;

   // The maximum number of cells the panel can display, above which only the local player cell will be shown
   public static int MAX_MEMBER_CELLS = 6;

   // Distance between member arrow before colliding
   public const float ARROW_COLLISION_DISTANCE = .1f;

   // The user id of the group leader
   public int groupLeader;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   public void Start () {
      // Hide the panel by default
      hide();

      // Clear out any info
      memberContainer.DestroyAllChildrenExcept(panelHoveringZone.gameObject);
      _memberCells.Clear();

      InvokeRepeating(nameof(updateMemberCellDamage), 1, 1);
   }

   public void Update () {
      // Hide the panel when there is no player or he doesn't belong to a group
      if (!GroupManager.isInGroup(Global.player) || _memberCells.Count <= 0) {
         hide();
         return;
      }

      // Show the panel
      show();
   }

   private void updateMemberCellDamage () {
      foreach (GroupMemberArrow2 arrowIndicator in directionalArrowList) {
         arrowIndicator.gameObject.SetActive(false);
      }

      int i = 0;
      Dictionary<string, GroupMemberArrow2> coordinateValue = new Dictionary<string, GroupMemberArrow2>();

      int highestDamagerId = -1;
      int highestTankedId = -1;
      int highestHealerId = -1;
      int highestBuffedId = -1;
      if (_memberCells.Count > 0) {
         GroupMemberCell highestDamager = _memberCells.OrderByDescending(_ => _.totalDamage).ToList()[0];
         highestDamagerId = highestDamager.getUserId();

         GroupMemberCell highestTanker = _memberCells.OrderByDescending(_ => _.totalTanked).ToList()[0];
         highestTankedId = highestTanker.getUserId();

         GroupMemberCell highestHealed = _memberCells.OrderByDescending(_ => _.totalHealed).ToList()[0];
         highestHealerId = highestHealed.getUserId();

         GroupMemberCell highestBuffed = _memberCells.OrderByDescending(_ => _.totalBuffed).ToList()[0];
         highestBuffedId = highestBuffed.getUserId();
      }

      foreach (GroupMemberCell memberCell in _memberCells) {
         NetEntity entity = EntityManager.self.getEntity(memberCell.getUserId());
         if (entity != null) {
            if (entity is PlayerShipEntity) {
               memberCell.updateCellDamage(((PlayerShipEntity) entity).totalDamageDealt);
               memberCell.updateCellTanked(((PlayerShipEntity) entity).totalDamageTaken);
               memberCell.updateCellHealed(((PlayerShipEntity) entity).totalHeals);
               memberCell.updateCellBuffed(((PlayerShipEntity) entity).totalBuffs);

               if (memberCell.getUserId() == highestDamagerId && memberCell.totalDamage > 0) {
                  memberCell.highestDamageIndicator.SetActive(true);
               } else {
                  memberCell.highestDamageIndicator.SetActive(false);
               }

               if (memberCell.getUserId() == highestTankedId && memberCell.totalTanked > 0) {
                  memberCell.highestTankIndicator.SetActive(true);
               } else {
                  memberCell.highestTankIndicator.SetActive(false);
               }

               if (memberCell.getUserId() == highestHealerId && memberCell.totalHealed > 0) {
                  memberCell.highestHealIndicator.SetActive(true);
               } else {
                  memberCell.highestHealIndicator.SetActive(false);
               }

               if (memberCell.getUserId() == highestBuffedId && memberCell.totalBuffed > 0) {
                  memberCell.highestBuffIndicator.SetActive(true);
               } else {
                  memberCell.highestBuffIndicator.SetActive(false);
               }
            }

            if (i < directionalArrowList.Count) {
               GroupMemberArrow2 memArrow = directionalArrowList[i];
               memArrow.setTarget(entity.gameObject);
               memArrow.setTargetName(entity.entityName);
               bool isOutsideScreen = Global.player == null ? false : Vector3.Distance(Global.player.transform.position, entity.transform.position) > CLAMP_HIDE_DISTANCE;

               if (memArrow.isHorizontal) {
                  string cleanupVal = memArrow.transform.localPosition.y.ToString("f1");
                  string topOffset = (float.Parse(cleanupVal) - ARROW_COLLISION_DISTANCE).ToString("f2");
                  string bottomOffset = (float.Parse(cleanupVal) + ARROW_COLLISION_DISTANCE).ToString("f2");

                  // If there is already an arrow existing within these coordinates, add this new users name to that arrow
                  if (coordinateValue.ContainsKey(cleanupVal) || coordinateValue.ContainsKey(topOffset) || coordinateValue.ContainsKey(bottomOffset)) {
                     memArrow.isActive = false;
                     memArrow.gameObject.SetActive(false);
                     coordinateValue[cleanupVal].addTargetName(entity.entityName);
                  } else {
                     memArrow.isActive = true;
                     memArrow.gameObject.SetActive(true);
                     coordinateValue.Add(cleanupVal, memArrow);
                  }
               } else {
                  if (isOutsideScreen) {
                     memArrow.gameObject.SetActive(true);
                     memArrow.isActive = true;
                  }
               }
            }
            i++;
         }
      }
   }

   public void updatePanelWithGroupMembers (GroupMemberCellInfo[] groupMembers, int groupLeader) {
      // Clear out any old info
      memberContainer.DestroyAllChildrenExcept(panelHoveringZone.gameObject);
      _memberCells.Clear();
      GroupManager.self.groupMemberArrowContainer.DestroyChildren();
      _memberArrows.Clear();

      this.groupLeader = groupLeader;
      if (groupMembers.Length > MAX_MEMBER_CELLS) {
         // Large groups only display the local player portrait (temporary)
         foreach (GroupMemberCellInfo cellInfo in groupMembers) {
            if (cellInfo.userId == Global.player.userId) {
               instantiatePortraitAndArrow(cellInfo);
            }
         }
      } else {
         foreach (GroupMemberCellInfo cellInfo in groupMembers) {
            instantiatePortraitAndArrow(cellInfo);
         }
      }

      Instantiate(columnBottomPrefab, memberContainer.transform, false);
   }

   private void instantiatePortraitAndArrow (GroupMemberCellInfo cellInfo) {
      // Instantiate the cell
      GroupMemberCell cell = Instantiate(memberCellPrefab, memberContainer.transform, false);
      cell.setCellForGroupMember(cellInfo);
      _memberCells.Add(cell);
   }

   public void updateCellTooltip (int userId, string userName, int XP, string areaKey) {
      // Search for the cell
      foreach (GroupMemberCell cell in _memberCells) {
         if (cell.getUserId() == userId) {
            cell.updateTooltip(userName, XP, areaKey);
         }
      }
   }

   public void OnKickPlayerButtonClickedOn (int playerToKick) {
      PanelManager.self.showConfirmationPanel("Are you sure you want to kick player from the group?",
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
         PanelManager.self.hideCurrentPanel();
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
      if (PanelManager.self.countdownScreen.isShowing() || Global.player == null) {
         return;
      }

      if (Global.player.hasAttackers()) {
         if (Global.player.isInCombat()) {
            int timeUntilCanLeave = (int) (NetEntity.IN_COMBAT_STATUS_DURATION - Global.player.getTimeSinceAttacked());
            PanelManager.self.noticeScreen.show("Cannot leave group until out of combat for " + (int)NetEntity.IN_COMBAT_STATUS_DURATION + " seconds. \n(" + timeUntilCanLeave + " seconds left)");
            return;
         }
      }

      if (!Global.player.isGhost && (GroupInstanceManager.isTreasureSiteArea(Global.player.areaKey) || GroupInstanceManager.isAnyLeagueArea(Global.player.areaKey) || GroupInstanceManager.isPvpArenaArea(Global.player.areaKey))) {
         // Outside of safe areas, start a leave countdown
         PanelManager.self.countdownScreen.cancelButton.onClick.RemoveAllListeners();
         PanelManager.self.countdownScreen.onCountdownEndEvent.RemoveAllListeners();
         PanelManager.self.countdownScreen.cancelButton.onClick.AddListener(() => PanelManager.self.countdownScreen.hide());
         PanelManager.self.countdownScreen.onCountdownEndEvent.AddListener(() => onLeaveCountdownEnd());
         PanelManager.self.countdownScreen.customText.text = "Leaving Group in";

         // Start the countdown
         PanelManager.self.countdownScreen.seconds = LEAVING_COUNTDOWN_SECONDS;
         PanelManager.self.countdownScreen.show();
      } else {
         // In safe areas or in ghost mode, ask confirmation and leave without countdown
         PanelManager.self.showConfirmationPanel("Are you sure you want to leave the group?", () => onLeaveCountdownEnd());
      }

      // Close the current voyage panel if it is open
      if (PanelManager.self.get(Panel.Type.ReturnToCurrentVoyagePanel).isShowing()) {
         PanelManager.self.hideCurrentPanel();
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

      foreach (GroupMemberCell cell in _memberCells) {
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

      foreach (GroupMemberCell cell in _memberCells) {
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

      foreach (GroupMemberArrow arrow in _memberArrows) {
         arrow.activate();
      }
   }

   public void hide () {
      if (canvasGroup.IsShowing()) {
         canvasGroup.Hide();
      }

      foreach (GroupMemberArrow arrow in _memberArrows) {
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
   private List<GroupMemberCell> _memberCells = new List<GroupMemberCell>();

   // The list of group member arrows used to indicate the members location
   private List<GroupMemberArrow> _memberArrows = new List<GroupMemberArrow>();

   #endregion
}
