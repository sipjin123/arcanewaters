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
   public Transform npcParent, enemyParent, oreNodeParent, secretsParent, treasureSiteParent, seaMonsterParent;

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

   public static bool isHouse (string areaKey) {
      return areaKey.Contains("House");
   }

   public static bool isTown (string areaKey) {
      return areaKey.Contains("Town");
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

   public static string getName (string areaKey) {
      if (areaKey.Contains("Adventure")) {
         return "Adventure Shop";
      } else if (areaKey.Contains("Merchant")) {
         return "Merchant Shop";
      } else if (areaKey.Contains("Shipyard")) {
         return "Shipyard";
      }

      return areaKey;
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

   private void configurePathfindingGraph () {
      GridGraph graph = AstarPath.active.data.AddGraph(typeof(GridGraph)) as GridGraph;
      graph.center = transform.position;
      Tilemap firstTilemap = GetComponentInChildren<Tilemap>();
      graph.SetDimensions(firstTilemap.size.x, firstTilemap.size.y, firstTilemap.cellSize.x * GetComponentInChildren<Grid>().transform.localScale.x);
      graph.rotation = new Vector3(90.0f, 0.0f, 0.0f);
      graph.collision.use2D = true;
      graph.Scan();

      // Manually update the Graph as it doesn't detect our Tilemaps by default
      for (int y = 0; y < graph.Depth; y++) {
         for (int x = 0; x < graph.Width; x++) {
            // Get the map chunk at this grid position
            Vector3Int centeredCellPos = new Vector3Int(x - graph.Width / 2, (graph.Depth / 2 - 1) - y, 0);
            MapChunk chunk = getColliderChunkAtCell(centeredCellPos);

            // Iterate over the tilemap list of the chunk
            bool isWall = false;
            if (chunk != null) {
               foreach (Tilemap tilemap in chunk.getTilemaps()) {
                  TileBase tile = tilemap.GetTile(centeredCellPos);
                  if (tile == null) {
                     continue;
                  }

                  foreach (string collisionName in COLLIDING_TILEMAPS) {
                     if (tilemap.name.StartsWith(collisionName)) {
                        isWall = true;
                        break;
                     }
                  }

                  if (isWall) {
                     break;
                  }
               }
            }

            graph.nodes[y * graph.width + x].Walkable = !isWall;
         }
      }
   }

   #region Private Variables

   // The starting names of the tilemaps that will be considered for blocking Nodes
   private static readonly string[] COLLIDING_TILEMAPS = new string[] {
      "mountain", "shrub", "water", "fence", "bush", "stump", "stair", "prop"
   };

   // Stores the Tilemaps for this area
   protected List<TilemapLayer> _tilemapLayers = new List<TilemapLayer>();

   // The list of map collider chunks
   protected List<MapChunk> _colliderChunks = new List<MapChunk>();

   // The initial enable/disable state of the tilemap colliders
   protected Dictionary<TilemapCollider2D, bool> _initialStates = new Dictionary<TilemapCollider2D, bool>();

   // A reference to the tilemap grid
   protected Grid _grid = null;

   #endregion
}
