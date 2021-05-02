using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Text;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class NotificationPanel : MonoBehaviour
{
   #region Public Variables

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // The notification message text
   public Text text;

   // The button used to confirm and hide the panel
   public Button confirmButton;

   // The text button
   public Text buttonText;

   // The toggle indicating if the notification should be disabled in the future
   public Toggle disableNotificationToggle;

   // The container of the disable toggle
   public GameObject disableToggleContainer;

   // The animator component
   public Animator animator;

   #endregion

   public void Update () {
      // If the notification is showing and the player warps, hide it
      if (Global.player == null || PanelManager.self.loadingScreen.isShowing() || PanelManager.self.get(Panel.Type.WorldMap).isShowing()) {
         hide();
         return;
      }

      if (!isShowing() && NotificationManager.self.hasNotifications()) {
         show(NotificationManager.self.getFirst());
      }
   }

   public void show (Notification notification) {
      if (NotificationManager.self.isNotificationDisabled(Global.player.userId, notification.type)) {
         NotificationManager.self.removeFirst();
         return;
      }

      // Initialize the disable toggle
      disableNotificationToggle.SetIsOnWithoutNotify(false);
      disableToggleContainer.SetActive(notification.canBeDisabled());
      
      text.text = notification.getMessage();
      buttonText.text = notification.getButtonText();

      // Add actions to perform when the user confirms the notification
      confirmButton.onClick.RemoveAllListeners();
      confirmButton.onClick.AddListener(notification.action);
      confirmButton.onClick.AddListener(() => updateNotificationConfig(notification.type));

      if (notification.shouldCloseAtConfirm) {
         confirmButton.onClick.AddListener(NotificationManager.self.removeFirst);
         confirmButton.onClick.AddListener(hide);
      }

      show();

      // Start the animation to reveal the panel
      animator.SetTrigger("reveal");
   }

   private void show () {
      canvasGroup.Show();
   }

   private void updateNotificationConfig (Notification.Type type) {
      // Check if the notification must be disabled in the future
      if (Global.player != null && disableNotificationToggle.isOn) {
         NotificationManager.self.disableNotification(Global.player.userId, type);
      }
   }

   public void hide () {
      if (canvasGroup.IsShowing()) {
         canvasGroup.Hide();
      }
   }

   public bool isShowing () {
      return canvasGroup.IsShowing();
   }

   #region Private Variables

   #endregion
}