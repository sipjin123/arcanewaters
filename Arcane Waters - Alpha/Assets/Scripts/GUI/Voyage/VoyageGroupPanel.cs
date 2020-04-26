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

   // When the mouse is over this defined zone, we consider that it hovers the panel
   public RectTransform panelHoveringZone;

   // The exit button
   public Button xButton;

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
      memberContainer.DestroyChildren();
   }

   public void Update () {
      // Hide the panel when there is no player or he doesn't belong to a group
      if (!VoyageManager.isInVoyage(Global.player)) {
         hide();
         return;
      }

      // Show the panel
      show();

      // Display the X button only if the mouse is over the defined zone
      if (RectTransformUtility.RectangleContainsScreenPoint(panelHoveringZone, Input.mousePosition)) {
         xButton.gameObject.SetActive(true);
      } else {
         xButton.gameObject.SetActive(false);
      }

      // If the player is in combat, disable the X button
      if (Global.player.hasAttackers()) {
         xButton.interactable = false;
      } else {
         xButton.interactable = true;
      }
   }

   public void updatePanelWithGroupMembers (List<int> groupMembers) {
      // If the player does not belong to a group, or there are no group members, hide the panel
      if (!VoyageManager.isInVoyage(Global.player) || groupMembers.Count <= 0) {
         // Clear out any old info
         memberContainer.DestroyChildren();

         // Hide the panel
         hide();
         return;
      }

      // Show the panel
      show();

      // Clear out any old info
      memberContainer.DestroyChildren();

      // Instantiate the cells
      foreach(int memberUserId in groupMembers) {
         VoyageGroupMemberCell cell = Instantiate(memberCellPrefab, memberContainer.transform, false);
         cell.setCellForGroupMember(memberUserId);
      }
   }

   public void OnLeaveGroupButtonClickedOn () {
      // Check if the player is already leaving the group or if it is in combat
      if (PanelManager.self.countdownScreen.isShowing() || Global.player.hasAttackers()) {
         return;
      }

      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => confirmLeaveGroup());

      // Show a confirmation panel
      PanelManager.self.confirmScreen.show("Are you sure you want to leave the voyage group?");
   }

   public void confirmLeaveGroup () {
      // Hide the confirm panel
      PanelManager.self.confirmScreen.hide();

      // Initialize the countdown screen
      PanelManager.self.countdownScreen.cancelButton.onClick.RemoveAllListeners();
      PanelManager.self.countdownScreen.onCountdownEndEvent.RemoveAllListeners();
      PanelManager.self.countdownScreen.cancelButton.onClick.AddListener(() => PanelManager.self.countdownScreen.hide());
      PanelManager.self.countdownScreen.onCountdownEndEvent.AddListener(() => onLeaveCountdownEnd());
      PanelManager.self.countdownScreen.customText.text = "Leaving Voyage Group in";

      // Start the countdown
      PanelManager.self.countdownScreen.seconds = LEAVING_COUNTDOWN_SECONDS;
      PanelManager.self.countdownScreen.show();
   }

   public void onLeaveCountdownEnd () {
      // Hide the countdown
      PanelManager.self.countdownScreen.hide();

      // Request the server to remove the user from the group
      Global.player.rpc.Cmd_RemoveUserFromVoyageGroup();
   }

   public void show () {
      this.canvasGroup.alpha = 1f;
      this.canvasGroup.blocksRaycasts = true;
      this.canvasGroup.interactable = true;
   }

   public void hide () {
      this.canvasGroup.alpha = 0f;
      this.canvasGroup.blocksRaycasts = false;
      this.canvasGroup.interactable = false;
   }

   public bool isShowing () {
      return this.gameObject.activeSelf && canvasGroup.alpha > 0f;
   }

   #region Private Variables

   // The number of seconds the player must wait when leaving the group
   private static int LEAVING_COUNTDOWN_SECONDS = 5;

   // The countdown when leaving the group
   private float _countdown = 0;

   #endregion
}
