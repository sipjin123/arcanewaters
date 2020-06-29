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

   // The online icon
   public GameObject onlineIcon;

   // The offline icon
   public GameObject offlineIcon;

   // The level text
   public Text level;

   #endregion

   public void setRowForFriendshipInfo (FriendshipInfo entry) {
      _friendUserId = entry.friendUserId;
      friendName.text = entry.friendName;
      friendAreaName.text = Area.getName(entry.friendAreaKey);
      if (entry.isOnline) {
         onlineIcon.SetActive(true);
         offlineIcon.SetActive(false);
      } else {
         onlineIcon.SetActive(false);
         offlineIcon.SetActive(true);
      }
      level.text = LevelUtil.levelForXp(entry.friendXP).ToString();
   }

   public void onChatButtonPress () {
      FriendListPanel.self.onChatButtonPress(_friendUserId, friendName.text);
   }

   public void onSendMessageButtonPress () {
      FriendListPanel.self.onSendMessageButtonPress(friendName.text);
   }

   public void onDeleteFriendButtonPress () {
      FriendListPanel.self.onDeleteFriendButtonPress(_friendUserId, friendName.text);
   }

   #region Private Variables

   // The ID of the friend displayed by this row
   private int _friendUserId;

   #endregion
}
