using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Tilemaps;
using System;
using MapCreationTool.Serialization;
using System.Linq;
using Pathfinding;

public class Area : MonoBehaviour
{
   #region Public Variables

   // The special type of the area
   public enum SpecialType { None = 0, Voyage = 1, TreasureSite = 2, Town = 3 }

   // Hardcoded area keys
   public static string FARM = "Farm";
   public static string HOUSE = "House";
   public static string STARTING_TOWN = "Shroom Bay";

   // The key determining the type of area this is
   public string areaKey;

   // When the area is a shop, keep also at hand the town's name
   public string townAreaKey = null;

   // The area biome
   public Biome.Type biome;

   // The Camera Bounds associated with this area
   public PolygonCollider2D cameraBounds;

   // The Virtual Camera for this Area
   public Cinemachine.CinemachineVirtualCamera vcam;

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

   // Sea Monsters to be loaded by the server
   public List<ExportedPrefab001> seaMonsterDataFields = new List<ExportedPrefab001>();
   
   // Networked entity parents
   public Transform npcParent, enemyParent, oreNodeParent, secretsParent, treasureSiteParent, seaMonsterParent, botShipParent, userParent;

   #endregion

   public void registerNetworkPrefabData (List<ExportedPrefab001> npcDatafields, List<ExportedPrefab001> enemyDatafields,
      List<ExportedPrefab001> oreDataFields, List<ExportedPrefab001> treasureSiteDataFields, 
      List<ExportedPrefab001> shipDataFields, List<ExportedPrefab001> secretsEntranceDataFields,
      List<ExportedPrefab001> seaMonsterDataFields) {
      this.npcDatafields = npcDatafields;
      this.enemyDatafields = enemyDatafields;
      this.oreDataFields = oreDataFields;
      this.treasureSiteDataFields = treasureSiteDataFields;
      this.shipDataFields = shipDataFields;
      this.secretsEntranceDataFields = secretsEntranceDataFields;
      this.seaMonsterDataFields = seaMonsterDataFields;

      if (CommandCodes.get(CommandCodes.Type.NPC_DISABLE) || Util.isForceServerLocalWithAutoDbconfig()) {
         this.npcDatafields.Clear();
      }
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

      // Store a reference to the grid
      _grid = GetComponentInChildren<Grid>();

      // Retrieve the z coordinate of the water tilemap
      foreach (TilemapLayer layer in getTilemapLayers()) {
         if (layer.name.ToLower().EndsWith("water")) {
            waterZ = layer.tilemap.transform.position.z;
            break;
         }
      }

      // If the area is interior, find the town where it is located
      if (isInterior) {
         foreach (Warp warp in GetComponentsInChildren<Warp>()) {
            if (!string.IsNullOrEmpty(warp.areaTarget) && !AreaManager.self.isInteriorArea(warp.areaTarget)) {
               townAreaKey = warp.areaTarget;
               break;
            }
         }
      }

      if (string.IsNullOrEmpty(townAreaKey)) {
         townAreaKey = areaKey;
      }

      configurePathfindingGraph();

      // Store it in the Area Manager
      AreaManager.self.storeArea(this);
   }

   public void OnDestroy () {
      if (!ClientManager.isApplicationQuitting) {
         AstarPath.active.data.RemoveGraph(_graph);
      }
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

   public Vector3Int worldToCell (Vector2 worldPos) {
      return _grid.WorldToCell(worldPos);
   }

   public GridGraph getGraph () {
      return _graph;
   }

   public static bool isHouse (string areaKey) {
      return areaKey.Contains("House");
   }

   public static bool isTown (string areaKey) {
      return areaKey.Contains("Town");
   }

   public static string getName (string areaKey) {
      if (areaKey.Contains("Adventure")) {
         return "Adventure Shop";
      } else if (areaKey.Contains("Merchant")) {
         return "Merchant Shop";
      } else if (areaKey.Contains("Shipyard")) {
         return "Shipyard";
      }

      return Util.toTitleCase(areaKey);
   }

   public static Biome.Type getBiome (string areaKey) {
      return AreaManager.self.getAreaBiome(areaKey);
   }

   public static SoundManager.Type getBackgroundMusic (string areaKey) {
      Biome.Type biome = getBiome(areaKey);

      if (AreaManager.self.isInteriorArea(areaKey)) {
         return SoundManager.Type.None;
      } else if (AreaManager.self.isSeaArea(areaKey)) {
         switch (biome) {
            case Biome.Type.Forest:
               return SoundManager.Type.Sea_Forest;
            case Biome.Type.Desert:
               return SoundManager.Type.Sea_Desert;
            case Biome.Type.Pine:
               return SoundManager.Type.Sea_Pine;
            case Biome.Type.Snow:
               return SoundManager.Type.Sea_Snow;
            case Biome.Type.Lava:
               return SoundManager.Type.Sea_Lava;
            case Biome.Type.Mushroom:
               return SoundManager.Type.Sea_Mushroom;
            default:
               return SoundManager.Type.None;
         }
      } else {
         switch (biome) {
            case Biome.Type.Forest:
               return SoundManager.Type.Town_Forest;
            case Biome.Type.Desert:
               return SoundManager.Type.Town_Desert;
            case Biome.Type.Pine:
               return SoundManager.Type.Town_Pine;
            case Biome.Type.Snow:
               return SoundManager.Type.Town_Snow;
            case Biome.Type.Lava:
               return SoundManager.Type.Town_Lava;
            case Biome.Type.Mushroom:
               return SoundManager.Type.Town_Mushroom;
            default:
               return SoundManager.Type.None;
         }
      }
   }

   public static int getAreaId (string areaKey) {
      return areaKey.GetHashCode();
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

   public Vector2 getAreaSize () {
      if (_firstTilemap == null) {
         return new Vector2(0, 0);
      }
      return new Vector2(_firstTilemap.size.x * _grid.transform.localScale.x, _firstTilemap.size.y * _grid.transform.localScale.y);
   }

   public Vector2 getAreaHalfSize () {
      return getAreaSize() * 0.5f;
   }

   private void configurePathfindingGraph () {
      _graph = AstarPath.active.data.AddGraph(typeof(GridGraph)) as GridGraph;
      _graph.center = transform.position;
      _firstTilemap = GetComponentInChildren<Tilemap>();
      _graph.SetDimensions(_firstTilemap.size.x, _firstTilemap.size.y, _firstTilemap.cellSize.x * GetComponentInChildren<Grid>().transform.localScale.x);
      _graph.rotation = new Vector3(-90.0f, 0.0f, 0.0f);
      _graph.collision.use2D = true;
      _graph.collision.Initialize(_graph.transform, 1.0f);
      _graph.collision.type = ColliderType.Ray;
      _graph.collision.mask = LayerMask.GetMask("GridColliders");
      _graph.Scan();
   }

   #region Private Variables

   // Stores the Tilemaps for this area
   protected List<TilemapLayer> _tilemapLayers = new List<TilemapLayer>();

   // The list of map collider chunks
   protected List<MapChunk> _colliderChunks = new List<MapChunk>();

   // The initial enable/disable state of the tilemap colliders
   protected Dictionary<TilemapCollider2D, bool> _initialStates = new Dictionary<TilemapCollider2D, bool>();

   // A reference to the tilemap grid
   protected Grid _grid = null;

   // The grid graph of the area
   protected GridGraph _graph = null;

   // Reference to first tilemap which represents base level of map
   protected Tilemap _firstTilemap = null;

   #endregion
}
