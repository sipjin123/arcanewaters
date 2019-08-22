using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

[System.Serializable]
public struct MapSummary
{
   #region Public Variables

   // The address of the Server that this map is on
   public string serverAddress;

   // The port of the Server that this map is on
   public int serverPort;

   // The Area.Type associated with this map
   public Area.Type areaType;

   // The Biome.Type associated with this map
   public Biome.Type biomeType;

   // Current player count on the map instance
   public int playersCount;

   // Max player count on the map instance
   public int maxPlayersCount;

   #endregion

   public MapSummary (string serverAddress, int serverPort, Area.Type areaType, Biome.Type biomeType, int playersCount, int maxPlayersCount) {
      this.serverAddress = serverAddress;
      this.serverPort = serverPort;
      this.areaType = areaType;
      this.biomeType = biomeType;
      this.playersCount = playersCount;
      this.maxPlayersCount = maxPlayersCount;
   }

   #region Private Variables

   #endregion
}
