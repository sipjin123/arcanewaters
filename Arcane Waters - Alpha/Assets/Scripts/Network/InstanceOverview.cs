using UnityEngine;
using System.Collections.Generic;

public class InstanceOverview
{
   #region Public Variables

   // Id of this instance
   public int id;

   // Server port where this instance is in
   public int port;

   // Area key of this instance
   public string area = "";

   // Player count in this instance
   public int pCount = 0;

   // The max player count allowed in this instance
   public int maxPlayerCount = 0;

   // Group instance info, if this instance has it
   public GroupInstance groupInstance;

   // Difficulty rating of this instance
   public int difficulty;

   // Total enemies that were spawned and killed
   public int totalEnemyCount;

   // Alive enemies
   public int aliveEnemyCount;

   // Is this instance pvp
   public bool isPvp;

   // When was this instance created
   public long creationDate;

   // The biome
   public Biome.Type biome;

   #endregion

   #region Private Variables

   #endregion
}
