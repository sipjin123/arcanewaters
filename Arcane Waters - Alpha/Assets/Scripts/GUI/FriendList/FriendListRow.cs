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

   // The gameobject of the friend's guild icon
   public GameObject rootGuildIcon;

   // The guild icon of the friend
   public GuildIcon friendGuildIcon;

   // If this user is online
   public bool isOnline;

   // The button that is displayed if friend can be visited
   public GameObject visitButtonObj, disabledVisitButtonObj;

   #endregion

   public void setRowForFriendshipInfo (FriendshipInfo entry) {
      _friendUserId = entry.friendUserId;
      friendName.text = entry.friendName;
      friendAreaName.text = Area.getName(entry.friendAreaKey);
      isOnline = entry.isOnline;
      if (entry.isOnline) {
         onlineIcon.SetActive(true);
         offlineIcon.SetActive(false);
         visitButtonObj.SetActive(true);
         disabledVisitButtonObj.SetActive(false);
      } else {
         onlineIcon.SetActive(false);
         offlineIcon.SetActive(true);
         visitButtonObj.SetActive(false);
         disabledVisitButtonObj.SetActive(true);
      }
      level.text = LevelUtil.levelForXp(entry.friendXP).ToString();
      if (entry.friendGuildId > 0) {
         friendGuildIcon.setBackground(entry.friendIconBackground, entry.friendIconBackPalettes);
         friendGuildIcon.setBorder(entry.friendIconBorder);
         friendGuildIcon.setSigil(entry.friendIconSigil, entry.friendIconSigilPalettes);
         rootGuildIcon.SetActive(true);
      } else {
         rootGuildIcon.SetActive(false);
      }
   }

   public void visitUser () {
      if (isOnline) {
         FriendListPanel.self.close();
         Global.player.Cmd_PlayerVisit(friendName.text);
      }
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
