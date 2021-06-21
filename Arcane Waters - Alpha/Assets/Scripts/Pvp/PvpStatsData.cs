using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

[Serializable]
public class PvpStatsData {
   // List of player stats
   public List<PvpPlayerStat> playerStats;

   // If has been initialized
   public bool isInitialized = false;
}

[Serializable]
public class PvpPlayerStat {
   // User id
   public int userId;

   // The user name
   public string playerName;

   // How many opposing player kill count
   public int playerKills;

   // How many time this player died
   public int playerDeaths;

   // How many assists this player has
   public int playerAssists;

   // How many neutral sea monsters killed
   public int playerMonsterKills;

   // How many ai ships sunk
   public int playerShipKills;

   // How many buildings destroyed by this user
   public int playerStructuresDestroyed;

   // The team this player belongs to
   public int playerTeam;

   public PvpPlayerStat (int userId, string playerName, int playerTeam) {
      this.userId = userId;
      this.playerName = playerName;
      this.playerTeam = playerTeam;
   }
}