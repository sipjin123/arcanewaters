using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Tilemaps;

public class WaterChecker : ClientMonoBehaviour {
   #region Public Variables

   // Whether we're currently in full water or not
   public bool isInFullWater = false;

   // Whether we're currently in partial water or not
   public bool isInPartialWater = false;

   // The name of the tile we're in
   public string currentTile = "";

   // The back water ripple
   public GameObject waterRippleBack;

   // The front water ripple
   public GameObject waterRippleFront;

   // The last time that we were in water
   public float lastWaterTime = float.MinValue;

   // The Sound we use when wading in water
   public AudioSource waterWadingSound;

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
      allWaterTiles.UnionWith(_fullWaterTiles);
      allWaterTiles.UnionWith(_partialWaterTiles);
      allWaterTiles.UnionWith(_waterFallTiles);

      return allWaterTiles;
   }

   private void Update () {
      bool isMoving = _player.getRigidbody().velocity.magnitude > .1f;

      // Move the water overlay up or down based on whether we're in water
      foreach (SpriteRenderer renderer in _player.getRenderers()) {
         // Default water alpha
         renderer.material.SetFloat("_WaterAlpha", 1f);

         if (isInFullWater) {
            renderer.material.SetFloat("_WaterHeight", .48f);
            renderer.material.SetFloat("_WaterAlpha", 0f);
         } else if (isInPartialWater) {
            renderer.material.SetFloat("_WaterHeight", .42f);
         } else {
            renderer.material.SetFloat("_WaterHeight", 0f);
         }         
      }

      // Show or hide the water ripples
      waterRippleBack.SetActive(isInFullWater);
      waterRippleFront.SetActive(isInFullWater);

      // Make note of the last time we were in water
      if (inWater()) {
         lastWaterTime = Time.time;
      }

      // Adjust the water ading sound depending on if we're in water
      waterWadingSound.volume = inWater() && isMoving ? 1f : 0f;
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
         Vector3Int cellPos = area.worldToCell(_player.sortPoint.transform.position);

         // Locate the Water tilemap within the area
         List<TilemapLayer> layers = area.getTilemapLayers();
         for (int i = layers.Count - 1; i >= 0; i--) {
            TileBase tile = layers[i].tilemap.GetTile(cellPos);
            if (tile != null) {
               if (layers[i].name.ToLower().EndsWith("water")) {
                  this.currentTile = tile.name;
                  isInFullWater = _fullWaterTiles.Contains(currentTile) || _waterFallTiles.Contains(currentTile);
                  isInPartialWater = _partialWaterTiles.Contains(currentTile);
               }
               break;
            }
         }
      }
   }

   #region Private Variables

   // Our associated player
   protected NetEntity _player;

   // The names of the full water tiles
   protected static HashSet<string> _fullWaterTiles = new HashSet<string>() {
      "water_15", "water_16", "water_17",
      "water_129", "water_132", "water_133", "water_134", "water_135", "water_136",
      "water_144", "water_146", "water_147", "water_148", "water_149",
      "water_156", "water_157", "water_158", "water_159", "water_160", "water_161",
      "water_168", "water_171", "water_174", "water_180", "water_186", "water_192",
      "water_195", "water_198",
   };

   // The names of the waterfall tiles
   protected static HashSet<string> _waterFallTiles = new HashSet<string>() {
      "water_60", "water_61", "water_62", "water_72", "water_73", "water_74",
      "water_84", "water_85", "water_86", "water_96", "water_108", "water_120"
   };

   // The names of the partial water tiles
   protected static HashSet<string> _partialWaterTiles = new HashSet<string>() {
      "water_3","water_12", "water_18", "water_27", "water_0", "water_6", "water_24", "water_30"
   };

   #endregion;
}
