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

   // The countdown container
   public GameObject countdownBox;

   // The countdown text when leaving the group
   public Text leavingCountdownText;

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

      // Hide the leaving countdown
      countdownBox.SetActive(false);

      // Hide the x button
      xButton.gameObject.SetActive(false);
   }

   public void Update () {
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

      // Check if the leave group countdown is running
      if (countdownBox.activeSelf) {
         // Check if the player is in combat
         if (Global.player.hasAttackers()) {
            // Stop the countdown
            countdownBox.SetActive(false);
         } else {
            // Decrease the countdown
            _countdown -= Time.deltaTime;

            // Update the countdown text
            leavingCountdownText.text = Mathf.CeilToInt(_countdown).ToString();

            // Check if we reached the end of the countdown
            if (_countdown <= 0) {
               // Hide the countdown
               countdownBox.SetActive(false);

               // Request the server to remove the user from the group
               Global.player.rpc.Cmd_RemoveUserFromVoyageGroup();
            }
         }
      }
   }

   public void updatePanelWithGroupMembers (List<int> groupMembers) {
      // If the player does not belong to a group, or there are no group members, hide the panel
      if (Global.player.voyageGroupId == -1 || groupMembers.Count <= 0) {
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
      if (countdownBox.activeSelf || Global.player.hasAttackers()) {
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

      // Set the countdown start value
      _countdown = LEAVING_COUNTDOWN_SECONDS;

      // Start the countdown
      countdownBox.SetActive(true);
   }

   public void OnCancelLeaveGroupButtonClickedOn () {
      // Hide the countdown
      countdownBox.SetActive(false);
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

   // The number of seconds the player must wait when leaving the group
   private static int LEAVING_COUNTDOWN_SECONDS = 5;

   // The countdown when leaving the group
   private float _countdown = 0;

   #endregion
}
