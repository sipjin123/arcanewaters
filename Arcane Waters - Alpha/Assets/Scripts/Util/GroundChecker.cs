using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Tilemaps;

public class GroundChecker : ClientMonoBehaviour {
   #region Public Variables

   // True if we're standing on grass
   public bool isOnGrass = false;

   // True if we're standing on stone
   public bool isOnStone = false;

   // True if we're standing on wood
   public bool isOnWood = false;

   #endregion

   void Start () {
      // Look up components
      _player = GetComponent<NetEntity>();

      // Repeatedly check the ground
      InvokeRepeating("checkTheGround", 0f, .1f);
   }

   protected void checkTheGround () {
      // Default to everything false
      isOnGrass = isOnStone = isOnWood = false;

      // If we're falling or in water, we're not on any type of ground
      if (_player.fallDirection != 0 || _player.waterChecker.inWater()) {
         return;
      }

      // Look up the Area and Grid that we're currently in
      Area area = AreaManager.self.getArea(_player.areaKey);
      if (area != null) {
         Grid grid = area.GetComponentInChildren<Grid>();
         Vector3Int cellPos = grid.WorldToCell(_player.sortPoint.transform.position);

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

               if (_stoneTiles.Contains(num)) {
                  isOnStone = true;
               }
               if (_woodTiles.Contains(num)) {
                  isOnWood = true;
               }
               if (_grassTiles.Contains(num)) {
                  isOnGrass = true;
               }
            }
         }
      }
   }

   #region Private Variables

   // Our associated player
   protected NetEntity _player;

   // The tile numbers for grass
   protected List<int> _grassTiles = new List<int>() { 0, 1 };

   // The tile numbers for stone
   protected List<int> _stoneTiles = new List<int>() { 11, 12, 13, 28, 29, 30, 45, 46, 47, 69, 143, 144, 145, 211, 212, 228, 229 };

   // The tile numbers for wood
   protected List<int> _woodTiles = new List<int>() { 62, 63, 64, 79, 80, 81, 96, 97, 98, 113, 114, 115, 160, 161, 162, 177, 178, 179, 194, 195, 196,
      183, 184, 185, 200, 201, 202 };

   #endregion;
}
