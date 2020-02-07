using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class VoyageGroupPanel : ClientMonoBehaviour
{
   #region Public Variables

   // Our Canvas Group
   public CanvasGroup canvasGroup;

   // The container for the group member rows
   public GameObject memberContainer;

   // The prefab we use for creating member cells
   public VoyageGroupMemberCell memberCellPrefab;

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
   }

   public void Update () {
      if (Global.player == null || !Global.player.isLocalPlayer) {
         return;
      }

      // If the player doesn't belong to any group, hide the panel
      if (Global.player.voyageGroupId == -1) {
         hide();
         return;
      }

      // Show the panel
      show();

      // Get the visible group members
      List<int> visibleMembers = VoyageManager.self.getVisibleVoyageGroupMembers();

      // Update the user rows depending on which ones are visible by this client
      foreach (VoyageGroupMemberCell cell in _memberCells) {
         if (visibleMembers.Contains(cell.getUserId())) {
            cell.enable();
         } else {
            cell.disable();
         }
      }
   }

   public void updatePanelWithGroupMembers (List<UserObjects> memberInfos) {
      // If the player does not belong to a group, or there are no group members, hide the panel
      if (Global.player.voyageGroupId == -1 || memberInfos.Count <= 0) {
         hide();
         return;
      }

      // Show the panel
      show();

      // Clear out any old info
      memberContainer.DestroyChildren();
      _memberCells.Clear();

      // Instantiate the cells
      foreach(UserObjects memberInfo in memberInfos) {
         VoyageGroupMemberCell cell = Instantiate(memberCellPrefab, memberContainer.transform, false);
         cell.setCellForGroupMember(memberInfo);
         _memberCells.Add(cell);
      }
   }

   public void OnLeaveGroupButtonClickedOn () {
      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => confirmLeaveGroup());

      // Show a confirmation panel
      PanelManager.self.confirmScreen.show("Are you sure you want to leave the voyage group?");
   }

   public void confirmLeaveGroup () {
      // Hide the confirm panel
      PanelManager.self.confirmScreen.hide();

      // Request the server to remove the user from the group
      Global.player.rpc.Cmd_RemoveUserFromVoyageGroup();
   }

   public void show () {
      if (!gameObject.activeSelf) {
         this.canvasGroup.alpha = 1f;
         this.canvasGroup.blocksRaycasts = true;
         this.canvasGroup.interactable = true;
         this.gameObject.SetActive(true);
      }
   }

   public void hide () {
      if (gameObject.activeSelf) {
         this.canvasGroup.alpha = 0f;
         this.canvasGroup.blocksRaycasts = false;
         this.canvasGroup.interactable = false;
         this.gameObject.SetActive(false);
      }
   }

   public bool isShowing () {
      return this.gameObject.activeSelf && canvasGroup.alpha > 0f;
   }

   #region Private Variables

   // The list of member cells
   private List<VoyageGroupMemberCell> _memberCells = new List<VoyageGroupMemberCell>();

   #endregion
}
