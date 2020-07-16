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

   // The online icon
   public GameObject onlineIcon;

   // The offline icon
   public GameObject offlineIcon;

   #endregion

   public void setRowForGuildMember (UserInfo userInfo) {
      _userId = userInfo.userId;
      _userName = userInfo.username;
      memberName.text = userInfo.username;
      memberLevel.text = LevelUtil.levelForXp(userInfo.XP).ToString();

      if (userInfo.isOnline) {
         onlineIcon.SetActive(true);
         offlineIcon.SetActive(false);
      } else {
         onlineIcon.SetActive(false);
         offlineIcon.SetActive(true);
      }
   }

   public virtual void OnPointerClick (PointerEventData eventData) {
      if (eventData.button == PointerEventData.InputButton.Right) {
         PanelManager.self.contextMenuPanel.showDefaultMenuForUser(_userId, _userName);
      }
   }

   #region Private Variables

   // The ID of the guild member displayed by this row
   private int _userId;

   // The name of the guild member displayed by this row
   private string _userName;

   #endregion
}
