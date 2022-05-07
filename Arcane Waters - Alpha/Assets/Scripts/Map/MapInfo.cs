using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MapInfo {
   #region Public Variables

   // The name of the map
   public string mapName;

   // The game data
   public string gameData;

   // The version number
   public int version;

   // The display name of the map
   public string displayName;
      
   #endregion

   public MapInfo (string mapName, string gameData, int version) {
      this.mapName = mapName;
      this.gameData = gameData;
      this.version = version;
   }

   #region Private Variables
      
   #endregion
}
