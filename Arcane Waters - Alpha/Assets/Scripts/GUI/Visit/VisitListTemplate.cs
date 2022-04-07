﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class VisitListTemplate : MonoBehaviour {
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

   public void setRowForFriendshipInfo (FriendshipInfo entry, bool isCustomMapSet) {
      _friendUserId = entry.friendUserId;
      friendName.text = entry.friendName;
      friendAreaName.text = Area.getName(entry.friendAreaKey);
      isOnline = entry.isOnline;
      if (entry.isOnline) {
         onlineIcon.SetActive(true);
         offlineIcon.SetActive(false);
      } else {
         onlineIcon.SetActive(false);
         offlineIcon.SetActive(true);
      }

      // Can't visit if user hasn't picked his maps
      if (!isCustomMapSet) {
         visitButtonObj.SetActive(false);
         disabledVisitButtonObj.SetActive(true);

         disabledVisitButtonObj.name = "Visit Button Disabled No Maps";
      } else if (entry.friendHouseMapId <= 0 || entry.friendFarmMapId <= 0) {
         // Can't visit if friend hasn't picked his maps
         visitButtonObj.SetActive(false);
         disabledVisitButtonObj.SetActive(true);

         disabledVisitButtonObj.name = "Visit Button Disabled No Target Maps";
      } else {
         visitButtonObj.SetActive(true);
         disabledVisitButtonObj.SetActive(false);
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
      Global.player.Cmd_PlayerVisitPrivateInstanceFarm(friendName.text, "");
      VisitListPanel.self.close();
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
