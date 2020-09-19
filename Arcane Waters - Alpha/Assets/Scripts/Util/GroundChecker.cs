using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Tilemaps;
using MapCreationTool.Serialization;

public class GroundChecker : ClientMonoBehaviour
{
   #region Public Variables

   // True if we're standing on grass
   public bool isOnGrass = false;

   // True if we're standing on stone
   public bool isOnStone = false;

   // True if we're standing on wood
   public bool isOnWood = false;

   // True if we're standing on bridge
   public bool isOnBridge = false;

   #endregion

   void Start () {
      // Look up components
      _player = GetComponent<NetEntity>();

      // Repeatedly check the ground
      InvokeRepeating("checkTheGround", 0f, .1f);
   }

   protected void checkTheGround () {
      // Default to everything false
      isOnGrass = isOnStone = isOnWood = isOnBridge = false;

      // If we're falling or in water, we're not on any type of ground
      if (_player.fallDirection != 0 || _player.waterChecker.inWater()) {
         return;
      }

      // Look up the Area and Grid that we're currently in
      Area area = AreaManager.self.getArea(_player.areaKey);
      if (area != null) {
         // Check if we have a precalculated cell types container, do dynamic check otherwise
         // TODO: just use cached check for everything in the future
         if (area.cellTypes.isInitialized) {
            cachedCheck(area.cellTypes, area);
         } else {
            dynamicCheck(area);
         }
      }
   }

   private void dynamicCheck (Area area) {
      Vector3Int cellPos = area.worldToCell(_player.sortPoint.transform.position);

      // Locate the tilemaps within the area
      foreach (TilemapLayer layer in area.getTilemapLayers()) {
         Tile tile = layer.tilemap.GetTile<Tile>(cellPos);

         if (tile == null) {
            continue;
         }

         // If we're in an interior, the floor is always wood
         if (tile.sprite.name.StartsWith("interior")) {
            isOnWood = true;
         }

         // If we're in one of the outdoor areas, we need to check specific tile numbers
         if (tile.sprite.name.Contains("_tiles")) {
            string[] split = tile.name.Split('_');
            int num = int.Parse(split[split.Length - 1]);


            if (Exporter.bridgeTiles.Contains(tile.sprite.name)) {
               isOnBridge = true;
            }
            if (Exporter.stoneTiles.Contains(num)) {
               isOnStone = true;
            }
            if (Exporter.woodTiles.Contains(num)) {
               isOnWood = true;
            }
            if (Exporter.grassTiles.Contains(num)) {
               isOnGrass = true;
            }
         }
      }
   }

   private void cachedCheck (CellTypesContainer cellTypes, Area area) {
      CellTypesContainer.MapCellType cellType = cellTypes.getCellType(_player.sortPoint.transform.position);

      isOnStone = cellType == CellTypesContainer.MapCellType.Stone;
      isOnWood = cellType == CellTypesContainer.MapCellType.Wood;
      isOnGrass = cellType == CellTypesContainer.MapCellType.Grass;

      // TODO: Bridge type needs to be added to cached Cell containers, otherwise it'll have to be checked dynamically
      if (!isOnStone && !isOnWood && !isOnGrass) {
         Vector3Int cellPos = area.worldToCell(_player.sortPoint.transform.position);
         foreach (TilemapLayer layer in area.getTilemapLayers()) {
            Tile tile = layer.tilemap.GetTile<Tile>(cellPos);

            if (tile == null) {
               continue;
            }

            if (Exporter.bridgeTiles.Contains(tile.sprite.name)) {
               isOnBridge = true;
            }
         }
      }
   }

   #region Private Variables

   // Our associated player
   protected NetEntity _player;

   #endregion;
}
