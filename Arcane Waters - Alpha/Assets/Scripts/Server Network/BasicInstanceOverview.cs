using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class BasicInstanceOverview
{
   #region Public Variables

   // The instance id
   public int instanceId = 0;

   // The player count
   public int playerCount = 0;

   // The maximum number of players
   public int maxPlayerCount = 0;

   // The area key
   public string areaKey;

   // Gets set to true when the instance was created for a player in single player mode
   public bool isSinglePlayer = false;

   // The server where this instance is hosted
   public int serverPort;

   #endregion
}

