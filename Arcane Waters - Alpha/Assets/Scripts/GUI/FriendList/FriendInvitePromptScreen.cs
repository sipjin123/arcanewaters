using UnityEngine;
using UnityEngine.UI;

public class FriendInvitePromptScreen : FullScreenSeparatePanel
{
   #region Public Variables

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // The accept button
   public Button acceptButton;

   // The refuse button
   public Button refuseButton;

   // The don't show again toggle
   public Toggle dontShowAgainToggle;

   // After this number of refusals, the showAgainToggle will show
   public uint maxRefusalsNumber = 3;

   #endregion

   public bool isShowing () {
      return this.gameObject.activeSelf && canvasGroup.alpha > 0f;
   }

   public void Update () {
      // Hide the panel during battle
      if (Global.player == null || Global.player.isInBattle()) {
         hide();
      }
   }

   public void show () {
      if (this.canvasGroup.alpha < 1f) {
         this.canvasGroup.alpha = 1f;
         this.canvasGroup.blocksRaycasts = true;
         this.canvasGroup.interactable = true;
      }
   }

   public void hide () {
      if (this.canvasGroup.alpha > 0f) {
         this.canvasGroup.alpha = 0f;
         this.canvasGroup.blocksRaycasts = false;
         this.canvasGroup.interactable = false;
      }
   }

   public void toggle (bool show) {
      if (show) {
         showPrompt();
      } else {
         hidePrompt();
      }
   }

   public void setCanShowAgain (bool canShowAgain) {
      _shouldShowAgain = canShowAgain;
   }

   private void hidePrompt () {
      gameObject.SetActive(false);
      hide();
   }

   private void showPrompt () {
      if (isShowing() || !_shouldShowAgain) {
         return;
      }

      // Associate a new function with the accept button
      acceptButton.onClick.RemoveAllListeners();
      acceptButton.onClick.AddListener(() => {
         BottomBar.self.toggleFriendListPanelAtTab(FriendListPanel.FriendshipPanelTabs.InvitesReceived);
      });

      // Associate a new function with the refuse button
      refuseButton.onClick.RemoveAllListeners();
      refuseButton.onClick.AddListener(() => {
         hidePrompt();
         _refusedCounter++;

         if (dontShowAgainToggle.isOn) {
            _shouldShowAgain = false;
            dontShowAgainToggle.isOn = false;
         }
      });

      // Check if we should display the toggle as well
      bool showToggle = _refusedCounter >= maxRefusalsNumber;
      dontShowAgainToggle.gameObject.SetActive(showToggle);

      // Show the prompt
      gameObject.SetActive(true);
      show();
   }

   #region Private Variables

   // How many times the user has refused so far?
   private uint _refusedCounter = 0;

   // Should the panel be permanently deactivated? This should happen if the player has already repeatedly refused to look at the Friends List
   private bool _shouldShowAgain = true;

   #endregion
}
