using UnityEngine;

/// <summary>
/// This class handles associating map cells with types.
/// If you divide map into map cells (stacks of tiles with the same XY position),
/// This class can check for a custom type of that cell, ex. water tile, stone tile,
/// For various system that require such checking
/// </summary>
public class CellTypesContainer
{
   #region Public Variables

   // Type of map cell(A single X, Y position with possibly multiple tiles)
   public enum MapCellType { None = 0, PartialWater = 1, FullWater = 2, Grass = 3, Stone = 4, Wood = 5 }

   // Is the container correctly initialized
   public bool isInitialized = false;

   #endregion

   public CellTypesContainer (MapCellType[] mapCellTypes, Vector2Int mapSize, Area area) {
      if (mapCellTypes == null) {
         return;
      }

      _mapCellTypes = mapCellTypes;
      _mapSize = mapSize;
      _area = area;

      isInitialized = true;
   }

   public MapCellType getCellType (Vector3 worldPosition) {
      // If we have no cell types defined, return default
      if (!isInitialized) {
         return MapCellType.None;
      }

      // Get cell position
      Vector3Int cellPos = _area.worldToCell(worldPosition);

      // Rebase coordinates to corner of map
      cellPos += (Vector3Int) _mapSize / 2;

      // If position is out of bounds, return default
      if (cellPos.x < 0 || cellPos.y < 0 || cellPos.x >= _mapSize.x || cellPos.y >= _mapSize.y) {
         return MapCellType.None;
      }

      return _mapCellTypes[cellPos.x * _mapSize.y + cellPos.y];
   }

   #region Private Variables

   // Represents all cells in a map, starting from bottom-left corner going upwards first
   private MapCellType[] _mapCellTypes;

   // Size of map this is targeting
   private Vector2Int _mapSize;

   // The area we are targeting
   private Area _area;

   #endregion
}
