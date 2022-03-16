using UnityEngine;

public class WorldMapGeoCoords
{
   #region Public Variables

   // X coord of the world area
   public int worldX;

   // Y coord of the world area
   public int worldY;

   // X coord within the area
   public float areaX;

   // Y coord within the area
   public float areaY;

   // X coord within an internal area
   public float subAreaX;

   // Y coord within an internal area
   public float subAreaY;

   // The key of the internal area
   public string subAreaKey;

   #endregion

   public WorldMapGeoCoords () {

   }

   public WorldMapGeoCoords (int worldX, int worldY, float areaX, float areaY, float x, float y) {
      this.worldX = worldX;
      this.worldY = worldY;
      this.areaX = areaX;
      this.areaY = areaY;
      this.subAreaX = x;
      this.subAreaY = y;
   }
}
