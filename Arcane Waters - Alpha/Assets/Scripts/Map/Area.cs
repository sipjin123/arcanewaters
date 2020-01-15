using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Tilemaps;
using System;

public class Area : MonoBehaviour
{
   #region Public Variables

   // Hardcoded area keys
   public static string STARTING_TOWN = "StartingTown";
   public static string FARM = "Farm";
   public static string HOUSE = "House";
   public static string TREASURE_PINE = "TreasurePine";
   public static string DESERT_TOWN = "DesertTown";
   public static string MERCHANT_SHOP_DESERT = "MerchantShop_Desert";
   public static string FOREST_TOWN = "ForestTown";

   // The key determining the type of area this is
   public string areaKey;

   // When the area is a shop, keep also at hand the town's name
   public string townAreaKey;

   // The Camera Bounds associated with this area
   public PolygonCollider2D cameraBounds;

   // The Virtual Camera for this Area
   public Cinemachine.CinemachineVirtualCamera vcam;

   // Whether this area is a sea area
   public bool isSea = false;

   #endregion

   private void Start () {
      // Regenerate any tilemap colliders
      foreach (CompositeCollider2D compositeCollider in GetComponentsInChildren<CompositeCollider2D>()) {
         compositeCollider.GenerateGeometry();
      }

      // Store all of our Tilemaps
      _tilemaps = new List<Tilemap>(GetComponentsInChildren<Tilemap>(true));

      // Store all of our tilemap colliders
      _tilemapColliders = new List<TilemapCollider2D>(GetComponentsInChildren<TilemapCollider2D>());

      // Make note of the initial states
      foreach (TilemapCollider2D collider in _tilemapColliders) {
         _initialStates[collider] = collider.enabled;
      }

      // Check if area is a sea area
      if (!isSea) {
         isSea = areaKey.StartsWith("Ocean") || areaKey.StartsWith("Sea");
      }

      // If the area is a town, lists all the areas that can be accessed from it
      if (isTown(areaKey)) {
         foreach (Warp warp in GetComponentsInChildren<Warp>()) {

            // Finds the destination area for each warp
            Spawn spawn = SpawnManager.self.getSpawn(warp.areaTarget, warp.spawnTarget);
            Area destinationArea = spawn.GetComponentInParent<Area>();

            // If the destination area is a shop, set its town as this area
            if (destinationArea != null && isShop(areaKey)) {
               destinationArea.townAreaKey = areaKey;
            }
         }
      } else {
         // Otherwise, the town area is the area itself
         townAreaKey = areaKey;
      }
   }

   public static bool isHouse (string areaKey) {
      return areaKey.Contains("House");
   }

   public static bool isTown (string areaKey) {
      return areaKey.Contains("Town");
   }

   public static bool isTreasureSite (string areaKey) {
      return areaKey.Contains("Treasure");
   }

   public static bool isRandom (string areaKey) {
      return getRandomAreaKeys().Contains(areaKey);
   }

   public static bool isShop (string areaKey) {
      return areaKey.Contains("Shop") || areaKey.Contains("Shipyard");
   }

   public static bool isMerchantShop (string areaKey) {
      return areaKey.StartsWith("MerchantShop");
   }

   public static bool isInterior (string areaKey) {
      return isShop(areaKey) || isHouse(areaKey);
   }

   public List<Tilemap> getTilemaps () {
      return _tilemaps;
   }

   public static List<string> getRandomAreaKeys () {
      return new List<string>() { "SeaRandom_1", "SeaRandom_2", "SeaRandom_3" };
   }

   public static string getName (string areaKey) {
      if (areaKey.StartsWith("Adventure")) {
         return "Adventure Shop";
      } else if (areaKey.StartsWith("Merchant")) {
         return "Merchant Shop";
      } else if (areaKey.StartsWith("Shipyard")) {
         return "Shipyard";
      }

      switch (areaKey) {
         case "StartingTown":
            return "Serenity Village";
         case "DesertTown":
            return "Desert Oasis";
         case "Ocean1":
         case "SeaBottom":
            return "Emerald Shores";
         case "SeaMiddle":
            return "Desert Isles";
         case "SeaTop":
            return "Hidden Forest";
         case "TreasurePine":
            return "Forest Treasure";
         default:
            return areaKey;
      }
   }

   public static Biome.Type getBiome (string areaKey) {
      switch (areaKey) {
         case "AdventureShop_Forest":
         case "Farm":
         case "House":
         case "MerchantShop_Forest":
         case "Ocean1":
         case "SeaBottom":
         case "Shipyard_Forest":
         case "StartingTown":
            return Biome.Type.Forest;
         case "AdventureShop_Desert":
         case "MerchantShop_Desert":
         case "DesertTown":
         case "SeaMiddle":
            return Biome.Type.Desert;
         case "SeaTop":
         case "TreasurePine":
            return Biome.Type.Pine;
         default:
            return Biome.Type.None;
      }
   }

   public static SoundManager.Type getBackgroundMusic (string areaKey) {
      switch (areaKey) {
         case "House":
         case "Farm":
         case "StartingTown":
            return SoundManager.Type.Town_Forest;
         case "Ocean1":
         case "SeaBottom":
            return SoundManager.Type.Sea_Forest;
         case "SeaMiddle":
            return SoundManager.Type.Sea_Desert;
         case "DesertTown":
            return SoundManager.Type.Town_Desert;
         case "SeaTop":
            return SoundManager.Type.Sea_Pine;
         case "TreasurePine":
            return SoundManager.Type.Town_Pine;
         default:
            return SoundManager.Type.None;
      }
   }

   public static int getAreaId (string areaKey) {
      return areaKey.GetHashCode();
   }

   public static List<string> getAllAreaKeys () {
      return new List<string>() {
         "StartingTown",
         "Farm",
         "Ocean1",
         "House",
         "TreasurePine",
         "DesertTown",
         "SeaBottom",
         "SeaMiddle",
         "SeaTop",
         "MerchantShop_Desert",
         "MerchantShop_Forest",
         "AdventureShop_Forest",
         "AdventureShop_Desert",
         "Shipyard_Forest",
         "TonyTest",
         "CollisionTest",
         "SeaRandom_1",
         "SeaRandom_2",
         "SeaRandom_3",
         "BurlTestMap"
      };
   }

   public void setColliders (bool newState) {
      foreach (TilemapCollider2D collider in _tilemapColliders) {
         // If the collider was initially disabled, don't change it
         if (_initialStates[collider] == false) {
            continue;
         }

         collider.enabled = newState;
      }
   }

   #region Private Variables

   // Stores the Tilemaps for this area
   protected List<Tilemap> _tilemaps = new List<Tilemap>();

   // The Tilemap Colliders for this area
   protected List<TilemapCollider2D> _tilemapColliders = new List<TilemapCollider2D>();

   // The initial enable/disable state of the tilemap colliders
   protected Dictionary<TilemapCollider2D, bool> _initialStates = new Dictionary<TilemapCollider2D, bool>();

   #endregion
}
