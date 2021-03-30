using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class FriendListManager : MonoBehaviour {

   #region Public Variables

   // The number of seconds between pending friendship request checks
   public static float PENDING_REQUESTS_CHECK_INTERVAL = 5 * 60f;

   // The friendship info cached data
   public List<FriendshipInfo> cachedFriendshipInfoList = new List<FriendshipInfo>();

   // Self
   public static FriendListManager self;

   #endregion

   void Awake () {
      D.adminLog("FriendListManager.Awake...", D.ADMIN_LOG_TYPE.Initialization);
      self = this;
      _pendingRequestsLastCheckTime = DateTime.UtcNow;
      D.adminLog("FriendListManager.Awake: OK", D.ADMIN_LOG_TYPE.Initialization);
   }

   public void startFriendListManagement () {
      // Regularly check pending friendship requests and send a notification if the user is connected to this server
      InvokeRepeating(nameof(sendPendingFriendshipNotifications), 30f, PENDING_REQUESTS_CHECK_INTERVAL);
   }

   // Called once after setting up Global.player to get initial data
   public void refreshFriendsData () {
      if (Global.player == null) {
         return;
      }

      Global.player.rpc.Cmd_RequestFriendsListFromServer();
   }

   public void sendPendingFriendshipNotifications () {
      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Get all the users that pending friendship requests, received since the last check time
         List<int> userIdsList = DB_Main.getUserIdsHavingPendingFriendshipRequests(_pendingRequestsLastCheckTime);

         // Back to Unity Thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Check if the user is connected to this server and send a notification
            NetEntity entity;
            foreach (int userId in userIdsList) {
               entity = EntityManager.self.getEntity(userId);
               if (entity != null) {
                  entity.Target_ReceiveFriendshipRequestNotification(entity.connectionToClient);
               }
            }

            _pendingRequestsLastCheckTime = DateTime.UtcNow;
         });
      });
   }

   public void sendFriendshipInvite (string friendUserName) {
      Global.player.rpc.Cmd_RequestUserIdForFriendshipInvite(friendUserName);
   }

   public void sendFriendshipInvite (int friendUserId, string friendName) {
      // Make sure the value of the friend user id is captured
      int friendUserIdForButtonEvent = friendUserId;

      // Show a confirmation panel
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => confirmSendFriendshipInvite(friendUserIdForButtonEvent));
      PanelManager.self.confirmScreen.show("Are you sure you want to ask " + friendName + " to be your friend?");
   }

   public void confirmSendFriendshipInvite (int friendUserId) {
      PanelManager.self.confirmScreen.hide();
      Global.player.rpc.Cmd_SendFriendshipInvite(friendUserId);
   }

   public void confirmDeleteFriend (int friendUserId) {
      PanelManager.self.confirmScreen.hide();
      Global.player.rpc.Cmd_DeleteFriendship(friendUserId);
   }

   public bool isFriend (int userId) {
      if (!Global.player) {
         return false;
      }

      foreach (FriendshipInfo info in FriendListManager.self.cachedFriendshipInfoList) {
         if ((info.userId == userId && info.friendUserId == Global.player.userId) || (info.friendUserId == userId && info.userId == Global.player.userId)) {
            return true;
         }
      }
      return false;
   }

   public List<string> getFriendNames () {
      List<string> friendNames = new List<string>();
      foreach (FriendshipInfo friend in cachedFriendshipInfoList) {
         friendNames.Add(friend.friendName);
      }

      return friendNames;
   }

   #region Private Variables

   // The last time the pending friendship requests were checked
   private DateTime _pendingRequestsLastCheckTime = DateTime.UtcNow;

   #endregion
}
