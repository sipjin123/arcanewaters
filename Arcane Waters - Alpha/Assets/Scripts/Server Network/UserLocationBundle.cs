using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class UserLocationBundle
{
   #region Public Variables

   // The user id
   public int userId;

   // The steam id of the user
   public ulong steamId;

   // The server where the user is located
   public int serverPort;

   // The area where the user is located
   public string areaKey;

   // The instance where the user is located
   public int instanceId;

   // The user local position in the area
   public float localPositionX;
   public float localPositionY;

   // The group the user belongs to
   public int voyageGroupId;

   #endregion

   public Vector2 getLocalPosition () {
      return new Vector2(localPositionX, localPositionY);
   }
}

