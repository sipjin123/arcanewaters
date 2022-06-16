using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class MailManager : GenericGameManager {
   #region Public Variables

   // The maximum number of items attached to a mail
   public static int MAX_ATTACHED_ITEMS = 5;

   // The maximum number of characters in a message
   public static int MAX_MESSAGE_LENGTH = 2000;

   // The number of seconds between unread mail checks
   public static float UNREAD_MAIL_CHECK_INTERVAL = 10;

   // The amount of days after which a mail can be deleted. Non-positive values make mails eternal
   public static int MAX_MAIL_LIFETIME_DAYS = 30;

   // Allow users to send mails to themselves?
   public static bool ALLOW_SELF_MAILING = false;

   // The account used to send system mails
   public static int SYSTEM_ACCOUNT_ID = 215165;

   // The subject used to send system mails
   public static string SYSTEM_USERNAME = "Arcane Waters";

   // Self
   public static MailManager self;

   #endregion

   protected override void Awake () {
      base.Awake();
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
                  entity.Target_ReceiveUnreadMailNotification(entity.connectionToClient, true);
               } 
            }

            _unreadMailsLastCheckTime = DateTime.UtcNow;
         });
      });
   }

   public static int getMailSendingCost () {
      return Math.Max(0, MAIL_SENDING_COST);
   }

   public static void sendSystemMail (int recipientUserId, string subject, string message, int[] attachedItemsIds, int[] attachedItemsCount) {
      // Use the player to send a system mail
      RPCManager.createSystemMail(recipientUserId, subject, message, attachedItemsIds, attachedItemsCount);
   }

   #region Private Variables

   // The last time the new mails were checked
   private DateTime _unreadMailsLastCheckTime = DateTime.UtcNow;

   // The cost for sending a mail
   private static int MAIL_SENDING_COST = 10;

   #endregion
}
