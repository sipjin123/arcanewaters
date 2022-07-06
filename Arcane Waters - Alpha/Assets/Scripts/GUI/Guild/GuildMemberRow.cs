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

   // Background images
   public Image[] backgroundImages;

   // Sprite for currently chosen row background
   public Sprite activeBackgroundSprite;

   // Sprite for inactive row background
   public Sprite inactiveBackgroundSprite;

   // Last time that guild member was logged-in
   public ulong lastActiveInMinutes;

   #endregion

   public void setRowForGuildMember (UserInfo userInfo, GuildRankInfo[] guildRanks) {
      _userId = userInfo.userId;
      _userName = userInfo.username;
      memberName.text = userInfo.username;
      memberLevel.text = LevelUtil.levelForXp(userInfo.XP).ToString();
      memberZone.text = Area.getName(userInfo.areaKey);

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

      shortenText(memberName);
      shortenText(memberRankName);
      shortenText(memberZone);

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
            memberOnlineStatus.text = "1m";
         } else if (timeDiff.TotalMinutes < 60) {
            memberOnlineStatus.text = (int) timeDiff.TotalMinutes + "m";
         } else if (timeDiff.TotalHours < 2) {
            memberOnlineStatus.text = "1h";
         } else if (timeDiff.TotalHours < 24) {
            memberOnlineStatus.text = (int) timeDiff.TotalHours + "h";
         } else if (timeDiff.TotalDays < 2) {
            memberOnlineStatus.text = "1m";
         } else {
            memberOnlineStatus.text = (int) timeDiff.TotalDays + "d";
         }
      }
   }

   private static void shortenText (Text text) {
      if (text.preferredWidth > text.GetComponent<LayoutElement>().preferredWidth) {
         text.text += "...";
         while (text.preferredWidth > text.GetComponent<LayoutElement>().preferredWidth) {
            text.text = text.text.Substring(0, text.text.Length - 4);
            text.text += "...";
         }
      }
   }

   public virtual void OnPointerClick (PointerEventData eventData) {
      if (eventData.button == PointerEventData.InputButton.Left) {
         bool newState = !highlightRow.activeSelf;

         // Disable all rows
         foreach (GuildMemberRow row in GuildPanel.self.getGuildMemberRows()) {
            row.highlightRow.SetActive(false);
            foreach (Image image in row.backgroundImages) {
               image.sprite = inactiveBackgroundSprite;
            }
         }

         // Change state of current row
         highlightRow.SetActive(newState);
         foreach(Image image in backgroundImages) {
            image.sprite = newState ? activeBackgroundSprite : inactiveBackgroundSprite;
         }

         // Update buttons interactivity
         GuildPanel.self.checkButtonPermissions();
      }

      if (eventData.button == PointerEventData.InputButton.Right) {
         D.adminLog("ContextMenu: Interact was performed via Guild Member:" +
            "{" + Global.player == null ? "" : (Global.player.userId + ":" + Global.player.entityName) + "}{" + _userId + ":" + _userName + "}", D.ADMIN_LOG_TYPE.Player_Menu);
         PanelManager.self.contextMenuPanel.showDefaultMenuForUser(_userId, _userName);
      }
   }

   public void onInviteClicked () {
      if (Global.player) {
         GroupManager.self.inviteUserToGroup(memberName.text);
      }
   }

   public int getUserId () {
      return _userId;
   }

   public string getUserName () {
      return _userName;
   }
   
   #region Private Variables

   // The ID of the guild member displayed by this row
   private int _userId;

   // The name of the guild member displayed by this row
   private string _userName;

   #endregion
}
