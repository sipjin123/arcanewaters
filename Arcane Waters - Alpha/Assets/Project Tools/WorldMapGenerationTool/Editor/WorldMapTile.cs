[System.Serializable]
public class WorldMapTile
{
   #region Public Variables

   // The x coordinate of the tile within the sector
   public int x;

   // The y coordinate of the tile within the sector
   public int y;

   // The type of the tile
   public TileType t;

   // Tile types
   public enum TileType
   {
      // None
      None = 0,

      // Water
      Water = 1,

      // Land
      Land = 2
   }

   #endregion

   #region Private Variables

   #endregion
}