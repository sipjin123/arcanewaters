using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class FriendListRow : MonoBehaviour
{
   #region Public Variables

   // The name of the friend
   public Text friendName;

   // The name of the area where the friend is located
   public Text friendAreaName;

   #endregion

   public void setRowForFriendshipInfo (FriendshipInfo entry) {
      _friendUserId = entry.friendUserId;
      friendName.text = entry.friendName;
      friendAreaName.text = Area.getName(entry.friendAreaKey);
   }

   public void onChatButtonPress () {
      FriendListPanel.self.onChatButtonPress(_friendUserId, friendName.text);
   }

   public void onSendMessageButtonPress () {
      FriendListPanel.self.onSendMessageButtonPress(_friendUserId, friendName.text);
   }

   public void onDeleteFriendButtonPress () {
      FriendListPanel.self.onDeleteFriendButtonPress(_friendUserId, friendName.text);
   }

   #region Private Variables

   // The ID of the friend displayed by this row
   private int _friendUserId;

   #endregion
}
