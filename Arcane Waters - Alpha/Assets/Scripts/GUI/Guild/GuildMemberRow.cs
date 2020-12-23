using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class GuildMemberRow : MonoBehaviour, IPointerClickHandler
{
   #region Public Variables

   // The name of the member
   public Text memberName;

   // The level of the member
   public Text memberLevel;

   // The rank name of the member
   public Text memberRankName;

   // The zone location of the member
   public Text memberZone;

   // The status of the member - online or when was last active
   public Text memberOnlineStatus;

   // The group invite button
   public Button inviteButton;

   // The online icon
   public GameObject onlineIcon;

   // The offline icon
   public GameObject offlineIcon;

   // The object which highlights guild member row in Guild Panel
   public GameObject highlightRow;

   // Last time that guild member was logged-in
   public ulong lastActiveInMinutes;

   #endregion

   public void setRowForGuildMember (UserInfo userInfo, GuildRankInfo[] guildRanks) {
      _userId = userInfo.userId;
      _userName = userInfo.username;
      memberName.text = userInfo.username;
      memberLevel.text = LevelUtil.levelForXp(userInfo.XP).ToString();
      memberZone.text = userInfo.areaKey;

      if (userInfo.guildRankId == 0) {
         memberRankName.text = "Leader";
      } else {
         foreach (GuildRankInfo rankInfo in guildRanks) {
            if (rankInfo.id == userInfo.guildRankId) {
               memberRankName.text = rankInfo.rankName.ToLower();
               if (memberRankName.text.Length > 0) {
                  string tmp = memberRankName.text;
                  memberRankName.text = tmp[0].ToString().ToUpper() + tmp.Substring(1);
               }
               break;
            }
         }
      }

      if (userInfo.isOnline) {
         onlineIcon.SetActive(true);
         offlineIcon.SetActive(false);
         memberOnlineStatus.text = "Online";
      } else {
         onlineIcon.SetActive(false);
         offlineIcon.SetActive(true);
         System.TimeSpan timeDiff = System.DateTime.Now.Subtract(userInfo.lastLoginTime);
         lastActiveInMinutes = (ulong) timeDiff.TotalMinutes;
         if (timeDiff.TotalSeconds < 119) {
            memberOnlineStatus.text = "1 Minute";
         } else if (timeDiff.TotalMinutes < 60) {
            memberOnlineStatus.text = (int) timeDiff.TotalMinutes + " Minutes";
         } else if (timeDiff.TotalHours < 2) {
            memberOnlineStatus.text = "1 Hour";
         } else if (timeDiff.TotalHours < 24) {
            memberOnlineStatus.text = (int) timeDiff.TotalHours + " Hours";
         } else if (timeDiff.TotalDays < 2) {
            memberOnlineStatus.text = "1 Day";
         } else {
            memberOnlineStatus.text = (int) timeDiff.TotalDays + " Days";
         }
      }
   }

   public virtual void OnPointerClick (PointerEventData eventData) {
      if (eventData.button == PointerEventData.InputButton.Left) {
         bool newState = !highlightRow.activeSelf;
         GuildPanel.self.getGuildMemeberRows().ForEach(row => row.highlightRow.SetActive(false));
         highlightRow.SetActive(newState);

         // Update buttons interactivity
         GuildPanel.self.checkButtonPermissions();
      }

      if (eventData.button == PointerEventData.InputButton.Right) {
         PanelManager.self.contextMenuPanel.showDefaultMenuForUser(_userId, _userName);
      }
   }

   public void onInviteClicked () {
      if (Global.player) {
         VoyageGroupManager.self.inviteUserToVoyageGroup(memberName.text);
      }
   }

   public int getUserId () {
      return _userId;
   }

   #region Private Variables

   // The ID of the guild member displayed by this row
   private int _userId;

   // The name of the guild member displayed by this row
   private string _userName;

   #endregion
}
