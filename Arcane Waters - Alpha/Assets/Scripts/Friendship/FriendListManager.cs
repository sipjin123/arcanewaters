using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class FriendListManager : MonoBehaviour {

   #region Public Variables

   // Self
   public static FriendListManager self;

   #endregion

   void Awake () {
      self = this;
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

   #region Private Variables

   #endregion
}
