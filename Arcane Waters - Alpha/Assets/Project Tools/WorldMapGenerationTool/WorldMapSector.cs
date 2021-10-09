using System.Collections.Generic;
using System.Linq;
using System.Text;

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

   // Tile set as string
   public string tilesString;

   #endregion

   public WorldMapTile getTileAt (int x, int y) {
      try {
         char tileAsCharacter = tilesString[w * y + x];
         int tileAsInt = int.Parse(tileAsCharacter.ToString());
         WorldMapTile.TileType tileType = (WorldMapTile.TileType) tileAsInt;
         WorldMapTile tile = new WorldMapTile { x = x, y = y, t = tileType };
         return tile;
      } catch (System.Exception ex) {
         D.error(ex.Message);
      }

      return null;
   }

   public static WorldMapSector parse (string serializedSector) {
      try {
         string[] lines = serializedSector.Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

         // The first line contains the header of the sector, with important info, such as the index of the sector, the number of columns and rows, etc...
         string[] tokens = lines[0].Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);

         WorldMapSector sector = new WorldMapSector {
            sectorIndex = int.Parse(tokens[0]),
            x = int.Parse(tokens[1]),
            y = int.Parse(tokens[2]),
            w = int.Parse(tokens[3]),
            h = int.Parse(tokens[4]),
         };

         var sb = new StringBuilder();

         // The following lines represent the rows of tiles contained in the current sector
         foreach (string line in lines.Skip(1)) {
            sb.Append(line.Trim());
         }

         sector.tilesString = sb.ToString();
         return sector;
      } catch (System.Exception ex) {
         D.warning(ex.Message);
      }

      return null;
   }

   #region Private Variables

   #endregion
}
