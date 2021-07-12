using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class AssignedUserInfo
{
   #region Public Variables

   // The user id
   public int userId = 0;

   // The area where the user has been assigned
   public string areaKey = "";

   // The instance where the user has been assigned
   public int instanceId = 0;

   // The last time the user was seen online
   public long lastOnlineTime = 0;

   // Gets set to true when the player is in single player mode and all instances he spawns in are private
   public bool isSinglePlayer = false;

   #endregion
}
