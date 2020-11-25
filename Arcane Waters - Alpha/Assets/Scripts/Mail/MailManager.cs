using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class MailManager : MonoBehaviour
{
   #region Public Variables

   // The maximum number of items attached to a mail
   public static int MAX_ATTACHED_ITEMS = 5;

   // The maximum number of characters in a message
   public static int MAX_MESSAGE_LENGTH = 2000;

   // The number of seconds between unread mail checks
   public static float UNREAD_MAIL_CHECK_INTERVAL = 5 * 60f;

   // Self
   public static MailManager self;

   #endregion

   public void Awake () {
      self = this;
      _unreadMailsLastCheckTime = DateTime.UtcNow;
   }

   public void startMailManagement () {
      // Regularly check new mails and send a notification if the user is connected to this server
      InvokeRepeating(nameof(sendUnreadMailNotifications), 30f, UNREAD_MAIL_CHECK_INTERVAL);
   }

   public void sendUnreadMailNotifications () {
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Get all the users that have unread mails, received since the last check time
         List<int> userIdsList = DB_Main.getUserIdsHavingUnreadMail(_unreadMailsLastCheckTime);

         // Back to Unity Thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Check if the user is connected to this server and send a notification
            NetEntity entity;
            foreach (int userId in userIdsList) {
               entity = EntityManager.self.getEntity(userId);
               if (entity != null) {
                  entity.Target_ReceiveUnreadMailNotification(entity.connectionToClient);
               } 
            }

            _unreadMailsLastCheckTime = DateTime.UtcNow;
         });
      });
   }

   #region Private Variables

   // The last time the new mails were checked
   private DateTime _unreadMailsLastCheckTime = DateTime.UtcNow;

   #endregion
}
