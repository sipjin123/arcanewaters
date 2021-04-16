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
   public static string ALL_NOTIFICATIONS_DISABLED = "All_Notifications_Disabled_";

   // Self
   public static NotificationManager self;

   #endregion

   public void Awake () {
      self = this;
   }

   public void add (Notification.Type type) {
      add(type, () => { });
   }

   public void add (Notification.Type type, UnityAction customAction) {
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

      _notifications.AddLast(new Notification(type, customAction));
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
      return PlayerPrefs.HasKey(ALL_NOTIFICATIONS_DISABLED + userId) || PlayerPrefs.HasKey(NOTIFICATIONS_DISABLED + userId + "_" + type.ToString());
   }

   public void disableNotification (int userId, Notification.Type type) {
      if (Notification.canBeDisabled(type)) {
         PlayerPrefs.SetInt(NOTIFICATIONS_DISABLED + userId + "_" + type.ToString(), 1);
      }
   }

   public void disableAllNotification (int userId) {
      PlayerPrefs.SetInt(ALL_NOTIFICATIONS_DISABLED + userId, 1);
   }

   public void enableAllNotifications (int userId) {
      PlayerPrefs.DeleteKey(ALL_NOTIFICATIONS_DISABLED + userId);
   }

   #region Private Variables

   // The notification stack
   private LinkedList<Notification> _notifications = new LinkedList<Notification>();

   #endregion
}