using System.Collections.Generic;

[System.Serializable]
public class WorldMap
{
   #region Public Variables

   // The number of sectors
   public int sectorCount;

   // The number of columns
   public int columns;

   // The number of rows
   public int rows;

   // The sections
   public List<WorldMapSector> sectors;

   #endregion
}
