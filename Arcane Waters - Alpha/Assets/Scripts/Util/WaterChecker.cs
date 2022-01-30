using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Tilemaps;
using MapCreationTool.Serialization;

public class WaterChecker : ClientMonoBehaviour
{
   #region Public Variables

   // Whether we're currently in full water or not
   public bool isInFullWater = false;

   // Whether we're currently in partial water or not
   public bool isInPartialWater = false;

   // The back water ripple
   public GameObject waterRippleBack;

   // The front water ripple
   public GameObject waterRippleFront;

   // The last time that we were in water
   public float lastWaterTime = float.MinValue;

   // The Sound we use when wading in water
   //public AudioSource waterWadingSound;

   // The shadow obj
   public GameObject shadowObj;

   // Heights used for the water cover shader
   public float fullWaterHeight = 0.48f;
   public float waterHeight = 0.42f;

   #endregion

   void Start () {
      // Look up components
      _player = GetComponent<NetEntity>();

      // Repeatedly check for water
      InvokeRepeating("checkForWater", 0f, .1f);
   }

   public bool inWater () {
      return (isInFullWater || isInPartialWater);
   }

   public float getTimeSinceWater () {
      return Time.time - lastWaterTime;
   }

   public bool recentlyInWater () {
      return getTimeSinceWater() < 8f;
   }

   public static HashSet<string> getAllWaterTiles () {
      HashSet<string> allWaterTiles = new HashSet<string>();
      allWaterTiles.UnionWith(Exporter.fullWaterTiles);
      allWaterTiles.UnionWith(Exporter.partialWaterTiles);
      allWaterTiles.UnionWith(Exporter.waterFallTiles);

      return allWaterTiles;
   }

   private void Update () {
      bool isMoving = _player.getRigidbody().velocity.magnitude > .1f;

      // Move the water overlay up or down based on whether we're in water
      foreach (SpriteRenderer renderer in _player.getRenderers()) {
         // Default water alpha
         renderer.material.SetFloat("_WaterAlpha", 1f);

         if (isInFullWater) {
            renderer.material.SetFloat("_WaterHeight", fullWaterHeight);
            renderer.material.SetFloat("_WaterAlpha", 0f);
         } else if (isInPartialWater) {
            renderer.material.SetFloat("_WaterHeight", waterHeight);
         } else {
            renderer.material.SetFloat("_WaterHeight", 0f);
         }
      }

      // Show or hide the water ripples
      waterRippleBack.SetActive(isInFullWater);
      waterRippleFront.SetActive(isInFullWater);

      // Make note of the last time we were in water
      if (inWater()) {
         shadowObj.SetActive(false);
         lastWaterTime = Time.time;
      } else {
         shadowObj.SetActive(true);
      }

      // Adjust the water ading sound depending on if we're in water
      //waterWadingSound.volume = inWater() && isMoving ? 1f : 0f;
   }

   protected void checkForWater () {
      // Default
      isInFullWater = false;
      isInPartialWater = false;

      // If we're falling, we're not in water
      if (_player.fallDirection != 0) {
         return;
      }

      // Look up the Area and Grid that we're currently in
      Area area = AreaManager.self.getArea(_player.areaKey);
      if (area != null) {
         // Check if we have a precalculated cell types container, do dynamic check otherwise
         // TODO: just use cached check for everything in the future
         if (area.cellTypes.isInitialized) {
            cachedCheck(area.cellTypes);
         } else {
            dynamicCheck(area);
         }
      }
   }

   private void dynamicCheck (Area area) {
      Vector3Int cellPos = area.worldToCell(_player.sortPoint.transform.position);

      // Locate the Water tilemap within the area
      List<TilemapLayer> layers = area.getTilemapLayers();
      for (int i = layers.Count - 1; i >= 0; i--) {
         TileBase tile = layers[i].tilemap.GetTile(cellPos);
         if (tile != null && !Exporter.nonWaterBlockingTiles.Contains(tile.name)) {
            if (layers[i].name.ToLower().EndsWith("water")) {
               string currentTile = tile.name;
               isInFullWater = Exporter.fullWaterTiles.Contains(currentTile) || Exporter.waterFallTiles.Contains(currentTile);
               isInPartialWater = Exporter.partialWaterTiles.Contains(currentTile);
            }
            break;
         }
      }
   }

   public static bool dynamicCheckIsInWater (Area area, Vector3 position) {
      Vector3Int cellPos = area.worldToCell(position);
      bool isInWater = false;

      // Locate the Water tilemap within the area
      List<TilemapLayer> layers = area.getTilemapLayers();
      for (int i = layers.Count - 1; i >= 0; i--) {
         TileBase tile = layers[i].tilemap.GetTile(cellPos);
         if (tile != null && !Exporter.nonWaterBlockingTiles.Contains(tile.name)) {
            if (layers[i].name.ToLower().EndsWith("water")) {
               string currentTile = tile.name;
               isInWater |= Exporter.fullWaterTiles.Contains(currentTile) || Exporter.waterFallTiles.Contains(currentTile);
               isInWater |= Exporter.partialWaterTiles.Contains(currentTile);
            }
            break;
         }
      }

      return isInWater;
   }

   private void cachedCheck (CellTypesContainer cellTypes) {
      CellTypesContainer.MapCellType cellType = cellTypes.getCellType(_player.sortPoint.transform.position);

      isInFullWater = cellType == CellTypesContainer.MapCellType.FullWater;
      isInPartialWater = cellType == CellTypesContainer.MapCellType.PartialWater;
   }

   #region Private Variables

   // Our associated player
   protected NetEntity _player;

   #endregion;
}
