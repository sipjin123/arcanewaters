using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Tilemaps;
using System;
using MapCreationTool.Serialization;
using System.Linq;
using AStar;

public class Area : MonoBehaviour
{
   #region Public Variables

   // Hardcoded area keys
   public static string FARM = "Farm";
   public static string HOUSE = "House";
   public static string STARTING_TOWN = "Snow Town Lite";

   // The key determining the type of area this is
   public string areaKey;

   // When the area is a shop, keep also at hand the town's name
   public string townAreaKey;

   // The area biome
   public Biome.Type biome;

   // The Camera Bounds associated with this area
   public PolygonCollider2D cameraBounds;

   // The Virtual Camera for this Area
   public Cinemachine.CinemachineVirtualCamera vcam;

   // The AStarGrid for the Area
   public AStarGrid pathfindingGrid;

   // Whether this area is a sea area
   public bool isSea = false;

  // The z coordinate of the water layer
   public float waterZ = 0f;
   
   // Whether this area is a interior area
   public bool isInterior = false;

   // The Version number of this Area, as determined by the Map Editor tool
   public int version;

   // NPC Data fields to be loaded by the server
   public List<ExportedPrefab001> npcDatafields = new List<ExportedPrefab001>();

   // Enemy Data fields to be loaded by the server
   public List<ExportedPrefab001> enemyDatafields = new List<ExportedPrefab001>();

   // Treasure sites to be loaded by the server
   public List<ExportedPrefab001> treasureSiteDataFields = new List<ExportedPrefab001>();

   // Ore spots to be loaded by the server
   public List<ExportedPrefab001> oreDataFields = new List<ExportedPrefab001>();

   // Ships to be loaded by the server
   public List<ExportedPrefab001> shipDataFields = new List<ExportedPrefab001>();

   // Secret entrance to be loaded by the server
   public List<ExportedPrefab001> secretsEntranceDataFields = new List<ExportedPrefab001>();

   // Networked entity parents
   public Transform npcParent, enemyParent, oreNodeParent, secretsParent;

   #endregion

   public void registerNetworkPrefabData (List<ExportedPrefab001> npcDatafields, List<ExportedPrefab001> enemyDatafields,
      List<ExportedPrefab001> oreDataFields, List<ExportedPrefab001> treasureSiteDataFields, List<ExportedPrefab001> shipDataFields, List<ExportedPrefab001> secretsEntranceDataFields) {
      this.npcDatafields = npcDatafields;
      this.enemyDatafields = enemyDatafields;
      this.oreDataFields = oreDataFields;
      this.treasureSiteDataFields = treasureSiteDataFields;
      this.shipDataFields = shipDataFields;
      this.secretsEntranceDataFields = secretsEntranceDataFields;
   }

   public void initialize () {
      // Regenerate any tilemap colliders
      foreach (CompositeCollider2D compositeCollider in GetComponentsInChildren<CompositeCollider2D>()) {
         compositeCollider.GenerateGeometry();
      }

      // Store all of our Tilemaps if they haven't been set already
      if (_tilemapLayers == null) {
         _tilemapLayers = GetComponentsInChildren<Tilemap>(true)
            .Select(t => new TilemapLayer {
               tilemap = t,
               // There is no way to recover name here, therefore ignore layer to indicate error
               fullName = t.gameObject.name + " DEGENERATED",
               name = t.gameObject.name,
               type = MapCreationTool.LayerType.Regular
            }).ToList();
      }

      // Make note of the initial states
      foreach (MapChunk chunk in _colliderChunks) {
         foreach (TilemapCollider2D collider in chunk.getTilemapColliders()) {
            _initialStates[collider] = collider.enabled;
         }
      }

      // Retrieve the z coordinate of the water tilemap
      foreach (TilemapLayer layer in getTilemapLayers()) {
         if (layer.name.ToLower().EndsWith("water")) {
            waterZ = layer.tilemap.transform.position.z;
            break;
         }
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

      pathfindingGrid.displayGrid(transform.position, this);

      // Store it in the Area Manager
      AreaManager.self.storeArea(this);
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

   public static bool isShop (string areaKey) {
      return areaKey.Contains("Shop") || areaKey.Contains("Shipyard");
   }

   public static bool isMerchantShop (string areaKey) {
      return areaKey.StartsWith("MerchantShop");
   }

   public List<TilemapLayer> getTilemapLayers () {
      return _tilemapLayers;
   }

   public void setTilemapLayers (List<TilemapLayer> layers) {
      _tilemapLayers = layers;
   }

   public MapChunk getColliderChunkAtCell (Vector3Int cellPos) {
      foreach (MapChunk chunk in _colliderChunks) {
         if (chunk.contains(cellPos)) {
            return chunk;
         }
      }
      return null;
   }

   public void setColliderChunks (List<MapChunk> chunks) {
      _colliderChunks = chunks;
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
      return AreaManager.self.getAreaBiome(areaKey);
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
         "BurlTestMap"
      };
   }

   public static float getTilemapZ (string areaKey, string layerName) {
      float tilemapZ = 0;

      Area area = AreaManager.self.getArea(areaKey);
      if (area != null) {
         // Locate the tilemaps within the area
         foreach (TilemapLayer layer in area.getTilemapLayers()) {
            if (layer.name.ToLower().EndsWith(layerName)) {
               tilemapZ = layer.tilemap.transform.position.z;
               break;
            }
         }
      }

      return tilemapZ;
   }

   public void setColliders (bool newState) {
      foreach (MapChunk chunk in _colliderChunks) {
         foreach (TilemapCollider2D collider in chunk.getTilemapColliders()) {
            // If the collider was initially disabled, don't change it
            if (_initialStates[collider] == false) {
               continue;
            }

            collider.enabled = newState;
         }
      }
   }

   #region Private Variables

   // Stores the Tilemaps for this area
   protected List<TilemapLayer> _tilemapLayers = new List<TilemapLayer>();

   // The list of map collider chunks
   protected List<MapChunk> _colliderChunks = new List<MapChunk>();

   // The initial enable/disable state of the tilemap colliders
   protected Dictionary<TilemapCollider2D, bool> _initialStates = new Dictionary<TilemapCollider2D, bool>();

   #endregion
}
