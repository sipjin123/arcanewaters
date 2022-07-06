using System;
using UnityEngine;

[Serializable]
public class WorldMapSpot
{
   #region Public Variables

   // Area width
   public int areaWidth;

   // Area height
   public int areaHeight;

   // Area position x
   public int worldX;

   // Area position y
   public int worldY;

   // X coord of the relative position within the area
   public float areaX;

   // Y coord of the relative position within the area
   public float areaY;

   // X coord of the site within the target area
   public float subAreaX;

   // Y coord of the site within the target area
   public float subAreaY;

   // The key of the internal area
   public string subAreaKey;

   // The target of the site
   public string target = "";

   // The spawn target of the site
   public string spawnTarget = "";

   // The id of the discovery
   public int discoveryId;

   // Computed discovery status
   public bool discovered;

   // Display name for the site
   public string displayName = "";

   // Site type
   public SpotType type;

   // Special Type
   public int specialType;

   // Site Types
   public enum SpotType
   {
      // None
      None = 0,

      // Warp
      Warp = 1,

      // League
      League = 2,

      // Discovery
      Discovery = 3,

      // Waypoint
      Waypoint = 4,

      // Player
      Player = 5
   }

   public string serialize () {
      return JsonUtility.ToJson(this);
   }

   #endregion
}
