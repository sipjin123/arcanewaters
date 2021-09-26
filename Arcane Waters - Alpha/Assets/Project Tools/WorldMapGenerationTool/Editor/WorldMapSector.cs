using System.Collections.Generic;

[System.Serializable]
public class WorldMapSector
{
   #region Public Variables

   // The index of the sector
   public int sectorIndex;

   // The x coordinate of the sector
   public int x;

   // The y coordinate of the sector
   public int y;

   // The width of the sector
   public int w;

   // The height of the sector
   public int h;

   // Object holding information about the parent map
   public WorldMapInfo map;

   // The tiles
   public List<WorldMapTile> tiles;

   #endregion

   #region Private Variables

   #endregion
}
