using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Tilemaps;

public class Area : MonoBehaviour {
   #region Public Variables

   // The types of area
   public enum Type { None = 0,
      StartingTown = 1, Farm = 2, Ocean1 = 3, House = 4, TreasurePine = 5,
      DesertTown = 6, SeaBottom = 7, SeaMiddle = 8, SeaTop = 9, MerchantShop_Desert = 10,
      MerchantShop_Forest = 11, AdventureShop_Forest = 12, AdventureShop_Desert = 13,
      Shipyard_Forest = 14,
   }

   // The type of area this is
   public Type areaType;

   // The Camera Bounds associated with this area
   public PolygonCollider2D cameraBounds;

   // The Virtual Camera for this Area
   public Cinemachine.CinemachineVirtualCamera vcam;

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
   }

   public static bool hasOre (Type areaType) {
      return AreaManager.self.getArea(areaType).GetComponent<OreArea>() != null;
   }

   public static bool isSea (Type areaType) {
      return (areaType.ToString().StartsWith("Ocean") || areaType.ToString().StartsWith("Sea"));
   }

   public static bool isHouse (Type areaType) {
      return areaType.ToString().Contains("House");
   }

   public static bool isTown (Type areaType) {
      return areaType.ToString().Contains("Town");
   }

   public static bool isTreasureSite (Type areaType) {
      return areaType.ToString().Contains("Treasure");
   }

   public List<Tilemap> getTilemaps () {
      return _tilemaps;
   }

   public static string getName (Type areaType) {
      if (areaType.ToString().StartsWith("Adventure")) {
         return "Adventure Shop";
      } else if (areaType.ToString().StartsWith("Merchant")) {
         return "Merchant Shop";
      } else if (areaType.ToString().StartsWith("Shipyard")) {
         return "Shipyard";
      }

      switch (areaType) {
         case Type.StartingTown:
            return "Serenity Village";
         case Type.DesertTown:
            return "Desert Oasis";
         case Type.Ocean1:
         case Type.SeaBottom:
            return "Emerald Shores";
         case Type.SeaMiddle:
            return "Desert Isles";
         case Type.SeaTop:
            return "Hidden Forest";
         case Type.TreasurePine:
            return "Forest Treasure";
         default:
            return areaType.ToString();
      }
   }

   public static Biome.Type getBiome (Type areaType) {
      switch (areaType) {
         case Type.AdventureShop_Forest:
         case Type.Farm:
         case Type.House:
         case Type.MerchantShop_Forest:
         case Type.Ocean1:
         case Type.SeaBottom:
         case Type.Shipyard_Forest:
         case Type.StartingTown:
            return Biome.Type.Tropical;
         case Type.AdventureShop_Desert:
         case Type.MerchantShop_Desert:
         case Type.DesertTown:
         case Type.SeaMiddle:
            return Biome.Type.Desert;
         case Type.SeaTop:
         case Type.TreasurePine:
            return Biome.Type.Pine;
         default:
            return Biome.Type.None;
      }
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
