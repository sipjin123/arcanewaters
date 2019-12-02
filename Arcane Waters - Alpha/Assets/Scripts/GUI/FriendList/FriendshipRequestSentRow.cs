using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class FriendshipRequestSentRow : MonoBehaviour
{
   #region Public Variables

   // The name of the friend
   public Text friendName;

   #endregion

   public void setRowForFriendshipInfo (FriendshipInfo entry) {
      _friendUserId = entry.friendUserId;
      friendName.text = entry.friendName;
   }

   public void onCancelRequestButtonPress () {
      FriendListPanel.self.onCancelFriendshipRequestButtonPress(_friendUserId, friendName.text);
   }

   #region Private Variables

   // The ID of the friend displayed by this row
   private int _friendUserId;

   #endregion
}
