using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.EventSystems;

public class AdminVoyageInfoRow : MonoBehaviour
{
   #region Public Variables

   // The user id
   public Text userIdText;

   // The user name
   public Text userNameText;

   #endregion

   public void setRowForUser (UserInfo userInfo) {
      userIdText.text = userInfo.userId.ToString();
      userNameText.text = userInfo.username;
   }

   #region Private Variables

   #endregion
}
