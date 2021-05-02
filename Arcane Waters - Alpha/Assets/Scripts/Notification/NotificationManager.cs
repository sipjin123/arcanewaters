using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using UnityEngine.Events;

public class NotificationManager : MonoBehaviour
{
   #region Public Variables

   // The keys used to save the config in PlayerPrefs
   public static string NOTIFICATIONS_DISABLED = "Notifications_Disabled_";
   public static string ALL_NOTIFICATIONS_DISABLED = "All_Notifications_Disabled";

   // Self
   public static NotificationManager self;

   #endregion

   public void Awake () {
      self = this;
   }

   public void add (Notification.Type type) {
      add(type, () => { });
   }

   public void add (Notification.Type type, UnityAction customAction, bool shouldCloseAtConfirm = true) {
      // Check if the notification is disabled
      if (Global.player != null && isNotificationDisabled(Global.player.userId, type)) {
         return;
      }

      // Check if the same notification is already in the stack
      foreach (Notification notification in _notifications) {
         if (notification.type == type) {
            return;
         }
      }

      _notifications.AddLast(new Notification(type, customAction, shouldCloseAtConfirm));
   }

   public void removeAllTypes (Notification.Type type) {
      // Close the notification panel if it is showing the current type
      if (hasNotifications() && getFirst().type == type) {
         PanelManager.self.notificationPanel.hide();
      }

      LinkedListNode<Notification> currentNode = _notifications.First;
      while (currentNode != null) {
         LinkedListNode<Notification> nextNode = currentNode.Next;
         if (currentNode.Value.type == type) {
            _notifications.Remove(currentNode.Value);
         }
         currentNode = nextNode;
      }
   }

   public Notification getFirst () {
      return _notifications.First.Value;
   }

   public void removeFirst () {
      if (_notifications.Count > 0) {
         _notifications.RemoveFirst();
      }
   }

   public bool hasNotifications () {
      return _notifications.Count > 0;
   }

   public void onUserLogOut () {
      _notifications.Clear();
      PanelManager.self.notificationPanel.hide();
   }

   public bool isNotificationDisabled (int userId, Notification.Type type) {
      if (Notification.canBeDisabled(type)) {
         return PlayerPrefs.HasKey(ALL_NOTIFICATIONS_DISABLED) || PlayerPrefs.HasKey(NOTIFICATIONS_DISABLED + userId + "_" + type.ToString());
      } else {
         return false;
      }
   }

   public void disableNotification (int userId, Notification.Type type) {
      if (Notification.canBeDisabled(type)) {
         PlayerPrefs.SetInt(NOTIFICATIONS_DISABLED + userId + "_" + type.ToString(), 1);
      }
   }

   public void toggleNotifications (bool isDisplayed) {
      if (isDisplayed) {
         PlayerPrefs.DeleteKey(ALL_NOTIFICATIONS_DISABLED);
      } else {
         PlayerPrefs.SetInt(ALL_NOTIFICATIONS_DISABLED, 1);
      }
   }

   public bool areAllNotificationsDisabled () {
      return PlayerPrefs.HasKey(ALL_NOTIFICATIONS_DISABLED);
   }

   #region Private Variables

   // The notification stack
   private LinkedList<Notification> _notifications = new LinkedList<Notification>();

   #endregion
}