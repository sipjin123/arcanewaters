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
using Cinemachine;

public class Area : MonoBehaviour
{
   #region Public Variables

   // The special type of the area
   public enum SpecialType { None = 0, Voyage = 1, TreasureSite = 2, Town = 3, Private = 4, League = 5, LeagueLobby = 6, LeagueSeaBoss = 7, PvpArena = 8, GuildMap = 9 }

   // Hardcoded area keys
   public static string STARTING_TOWN = "Tutorial Town";
   public static string DESERT_TOWN = "Desert Town Lite";
   public static string PINE_TOWN = "Pine Biome Town";
   public static string SNOW_TOWN = "Snow Town Lite";
   public static string LAVA_TOWN = "Lava Town";
   public static string MUSHROOM_TOWN = "Shroom Bay";

   // Dock spawns of each home town
   public static string STARTING_TOWN_DOCK_SPAWN = "Tutorial Town Dock";
   public static string DESERT_TOWN_DOCK_SPAWN = "dock";
   public static string PINE_TOWN_DOCK_SPAWN = "pine_town_dock";
   public static string SNOW_TOWN_DOCK_SPAWN = "Starting Spawn";
   public static string LAVA_TOWN_DOCK_SPAWN = "PortSpawn";
   public static string MUSHROOM_TOWN_DOCK_SPAWN = "Shroom Town";

   // The main towns in each biome
   public static Dictionary<Biome.Type, string> homeTownForBiome = new Dictionary<Biome.Type, string>() {
      {Biome.Type.Forest, STARTING_TOWN },
      {Biome.Type.Desert, DESERT_TOWN },
      {Biome.Type.Pine, PINE_TOWN },
      {Biome.Type.Snow, SNOW_TOWN },
      {Biome.Type.Lava, LAVA_TOWN },
      {Biome.Type.Mushroom, MUSHROOM_TOWN }
   };

   // The dock spawn points in each home town
   public static Dictionary<Biome.Type, string> dockSpawnForBiome = new Dictionary<Biome.Type, string>() {
      {Biome.Type.Forest, STARTING_TOWN_DOCK_SPAWN },
      {Biome.Type.Desert, DESERT_TOWN_DOCK_SPAWN },
      {Biome.Type.Pine, PINE_TOWN_DOCK_SPAWN },
      {Biome.Type.Snow, SNOW_TOWN_DOCK_SPAWN },
      {Biome.Type.Lava, LAVA_TOWN_DOCK_SPAWN },
      {Biome.Type.Mushroom, MUSHROOM_TOWN_DOCK_SPAWN }
   };

   // The key determining the type of area this is
   public string areaKey;

   // The key of the base map of the area, same as areaKey if no base map is used
   public string baseAreaKey;

   // When the area is a shop, keep also at hand the town's name
   public string townAreaKey = null;

   // The Camera Bounds associated with this area
   public PolygonCollider2D cameraBounds;

   // The Virtual Camera for this Area
   public Cinemachine.CinemachineVirtualCamera vcam;

   // Whether this area is a sea area
   public bool isSea = false;

   // The z coordinate of the water layer
   public float waterZ = 0f;

   // The supposed z coordinate of the building layer
   public const float BUILDING_Z = 0;

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

   // The ship data, separated by guild
   public List<ExportedPrefab001> privateerShipDataFields = new List<ExportedPrefab001>();
   public List<ExportedPrefab001> pirateShipDataFields = new List<ExportedPrefab001>();

   // Sea Monsters to be loaded by the server
   public List<ExportedPrefab001> seaMonsterDataFields = new List<ExportedPrefab001>();

   // Boss Spawners to be loaded by the server
   public List<ExportedPrefab001> bossSpawnerDataFields = new List<ExportedPrefab001>();

   // Pvp tower to be loaded by the server
   public List<ExportedPrefab001> towerDataFields = new List<ExportedPrefab001>();

   // Pvp shipyard to be loaded by the server
   public List<ExportedPrefab001> shipyardDataFields = new List<ExportedPrefab001>();

   // Pvp base to be loaded by the server
   public List<ExportedPrefab001> baseDataFields = new List<ExportedPrefab001>();

   // Pvp waypoints to be loaded by the server
   public List<ExportedPrefab001> waypointsDataFields = new List<ExportedPrefab001>();

   // Pvp monster spawner to be loaded by the server
   public List<ExportedPrefab001> pvpMonsterSpawnerDataFields = new List<ExportedPrefab001>();

   // The list of pvp loot spawners in this instance
   public List<ExportedPrefab001> pvpLootSpawners = new List<ExportedPrefab001>();

   // The list of pvp capture target holders to be loaded by the server
   public List<ExportedPrefab001> pvpCaptureTargetHolders = new List<ExportedPrefab001>();

   // Parent of generic prefabs
   public Transform prefabParent;

   // Networked entity parents
   public Transform npcParent, enemyParent, oreNodeParent, secretsParent, treasureSiteParent, seaMonsterParent, botShipParent, userParent;

   // The cloud manager
   public CloudManager cloudManager;

   // The cloud shadow manager
   public CloudShadowManager cloudShadowManager;

   // The value that determines if the screen is too wide for the area
   public static float WIDE_RESOLUTION_VALUE = 1920;

   // Container for checking types of cell(a stack of tiles in the same XY position)
   public CellTypesContainer cellTypes;

   // The build layer TODO: (NOTE: The current layer being imported is actually a typo, not sure yet where to update the layer name to correct this)
   public const string BUILDING_LAYER = "bulding";

   #endregion

   public void registerNetworkPrefabData (List<ExportedPrefab001> npcDatafields, List<ExportedPrefab001> enemyDatafields,
      List<ExportedPrefab001> oreDataFields, List<ExportedPrefab001> treasureSiteDataFields,
      List<ExportedPrefab001> shipDataFields, List<ExportedPrefab001> seaMonsterDataFields, List<ExportedPrefab001> bossSpawnerDataFields, 
      List<ExportedPrefab001> pvpTowerDataFields, List<ExportedPrefab001> pvpBaseDataFields, List<ExportedPrefab001> pvpShipyardDataFields, 
      List<ExportedPrefab001> pvpWaypoints, List<ExportedPrefab001> pvpMonsterSpawnerFields, List<ExportedPrefab001> pvpLootSpawners, 
      List<ExportedPrefab001> pvpCaptureTargetHolders) {
      this.npcDatafields = npcDatafields;
      this.enemyDatafields = enemyDatafields;
      this.oreDataFields = oreDataFields;
      this.treasureSiteDataFields = treasureSiteDataFields;
      this.shipDataFields = shipDataFields;
      this.seaMonsterDataFields = seaMonsterDataFields;
      this.bossSpawnerDataFields = bossSpawnerDataFields;
      this.towerDataFields = pvpTowerDataFields;
      this.shipyardDataFields = pvpShipyardDataFields;
      this.baseDataFields = pvpBaseDataFields;
      this.waypointsDataFields = pvpWaypoints;
      this.pvpMonsterSpawnerDataFields = pvpMonsterSpawnerFields;
      this.pvpLootSpawners = pvpLootSpawners;
      this.pvpCaptureTargetHolders = pvpCaptureTargetHolders;

      if (CommandCodes.get(CommandCodes.Type.NPC_DISABLE) || Util.isForceServerLocalWithAutoDbconfig()) {
         this.npcDatafields.Clear();
      }

      // Keep separate lists of pirate and privateer bot ships
      foreach (ExportedPrefab001 dataField in shipDataFields) {
         int guildId = 1;
         foreach (DataField field in dataField.d) {
            if (field.k.CompareTo(DataField.SHIP_GUILD_ID) == 0) {
               guildId = int.Parse(field.v.Split(':')[0]);
            }
         }

         if (guildId == BotShipEntity.PRIVATEERS_GUILD_ID) {
            privateerShipDataFields.Add(dataField);
         } else if (guildId == BotShipEntity.PIRATES_GUILD_ID) {
            pirateShipDataFields.Add(dataField);
         } else {
            D.debug(string.Format($"A bot ship in area {areaKey} has an unsupported guild id: {guildId}"));
         }
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

      // Store the mountain tilemap layers
      _mountainTilemapLayers.Clear();
      foreach (TilemapLayer layer in _tilemapLayers) {
         if (layer.name.ToLower().StartsWith("mountain")) {
            _mountainTilemapLayers.Add(layer);
         }
      }

      // Retrieve the z coordinate of the water tilemap
      foreach (TilemapLayer layer in getTilemapLayers()) {
         if (layer.name.ToLower().EndsWith("water")) {
            waterZ = layer.tilemap.transform.position.z;
            break;
         }
      }

      // Make sure that building Z layer is higher than the player ship layer
      foreach (TilemapLayer layer in getTilemapLayers()) {
         if (layer.name.ToLower().Contains(BUILDING_LAYER)) {
            layer.tilemap.transform.position = new Vector3(
               layer.tilemap.transform.position.x,
               layer.tilemap.transform.position.y,
               layer.tilemap.transform.position.z + BUILDING_Z);
         }
      }

      // Store a reference to all the warps in this area
      List<Warp> warpEntities = new List<Warp>(GetComponentsInChildren<Warp>());
      foreach (Warp warp in warpEntities) {
         _warps.Add(warp);
      }

      // Store a reference to all the action triggers "warping to league"
      List<GenericActionTrigger> genericActionTriggers = new List<GenericActionTrigger>(GetComponentsInChildren<GenericActionTrigger>());
      foreach (GenericActionTrigger trigger in genericActionTriggers) {
         if (trigger.actionName == GenericActionTrigger.WARP_TO_LEAGUE_ACTION) {
            _warpToLeagueTrigger.Add(trigger);
         }
      }

      // Store all references to temporary controllers
      _tempControllers = new List<TemporaryController>(GetComponentsInChildren<TemporaryController>());

      // If the area is interior, find the town where it is located
      if (isInterior) {
         foreach (Warp warp in _warps) {
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

      vcam.GetComponent<MyCamera>().setInternalOrthographicSize();

      // Store it in the Area Manager
      AreaManager.self.storeArea(this);
   }

   public void registerWarpFromSecretEntrance (Warp warp) {
      _warps.Add(warp);
   }

   public void OnDestroy () {
      if (!ClientManager.isApplicationQuitting) {
         AstarPath.active.data.RemoveGraph(_graph);
      }

      // Destroy any existing crops on the client, if the farm area is destroyed
      if (!NetworkServer.active && AreaManager.self.tryGetCustomMapManager(areaKey, out CustomMapManager customMapManager)) {
         if (customMapManager is CustomFarmManager) {
            foreach (Crop crop in FindObjectsOfType<Crop>()) {
               Destroy(crop.gameObject);
            }
         }
      }
   }

   public List<TilemapLayer> getTilemapLayers () {
      return _tilemapLayers;
   }

   public bool hasLandTile (Vector3 worldPos) {
      Vector3Int cellPos = worldToCell(worldPos);

      foreach (TilemapLayer layer in _mountainTilemapLayers) {
         TileBase tile = layer.tilemap.GetTile(cellPos);

         if (tile != null) {
            return true;
         }
      }

      return false;
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
      if (_graph == null) {
         configurePathfindingGraph();
      }

      return _graph;
   }

   public List<Warp> getWarps () {
      return _warps;
   }

   public List<GenericActionTrigger> getLeagueWarpTriggers () {
      return _warpToLeagueTrigger;
   }

   public TemporaryController getTemporaryControllerAtPosition (Vector2 localPosition) {
      return _tempControllers
         .FirstOrDefault(c => ((Vector2) c.transform.localPosition - localPosition).sqrMagnitude < 0.01f);
   }

   public static bool isHouse (string areaKey) {
      return areaKey.ToLower().Contains("house");
   }

   public static string getName (string areaKey) {
      // If this is a custom map, figure out the special display name
      if (AreaManager.self.tryGetCustomMapManager(areaKey, out CustomMapManager customMapManager)) {
         // Check if it is someone else's farm, prepend the name if so
         int userId = CustomMapManager.getUserId(areaKey);
         if (Global.player != null && userId != Global.player.userId) {
            string userName = EntityManager.self.getEntity(userId)?.entityName ?? "Unknown";
            return $"{ userName }'s { customMapManager.typeDisplayName }";
         }

         return customMapManager.typeDisplayName;
      }

      // Check if we have this map cached
      Map map = AreaManager.self.getMapInfo(areaKey);
      if (map != null) {
         // Display a special name for league maps
         if (map.specialType == SpecialType.League || map.specialType == SpecialType.LeagueSeaBoss) {
            return "Leagues";
         } else if (map.specialType == SpecialType.LeagueLobby) {
            return "League Lobby";
         } else if (map.displayName != null) {
            return Util.toTitleCase(map.displayName);
         }
      }

      // Otherwise, use default name
      return Util.toTitleCase(areaKey);
   }

   public static SoundManager.Type getBackgroundMusic (string areaKey, Biome.Type biome) {
      if (AreaManager.self.tryGetCustomMapManager(areaKey, out CustomMapManager customMapManager)) {
         if (customMapManager is CustomFarmManager) {
            return SoundManager.Type.Farm_Music;
         }
      }

      if (AreaManager.self.isInteriorArea(areaKey)) {
         return SoundManager.Type.Interior;
      } else if (VoyageManager.isLeagueSeaBossArea(areaKey)) {
         return SoundManager.Type.Sea_Lava;
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

   public Vector2 getAreaSizeWorld () {
      // If we don't have tilemaps assigned yet, we can assume that the map is at least 64x64
      Vector2Int tileSize = new Vector2Int(64, 64);

      if (_tilemapLayers == null) {
         D.warning("Cannot calculate area size because tilemaps have not been assigned yet.");
      } else {
         tileSize = new Vector2Int(_tilemapLayers.Max(t => t.tilemap.size.x), _tilemapLayers.Max(t => t.tilemap.size.y));
      }

      return new Vector2(tileSize.x * _grid.transform.localScale.x, tileSize.y * _grid.transform.localScale.y);
   }

   public Vector2 getAreaHalfSizeWorld () {
      return getAreaSizeWorld() * 0.5f;
   }

   private void configurePathfindingGraph () {
      _graph = AstarPath.active.data.AddGraph(typeof(GridGraph)) as GridGraph;
      _graph.center = transform.position;
      _firstTilemap = GetComponentInChildren<Tilemap>();
      _graph.SetDimensions(_firstTilemap.size.x, _firstTilemap.size.y, _firstTilemap.cellSize.x * GetComponentInChildren<Grid>().transform.localScale.x);
      _graph.rotation = new Vector3(-90.0f, 0.0f, 0.0f);
      _graph.collision.use2D = true;
      _graph.collision.Initialize(_graph.transform, 1.0f);

      if (VoyageManager.isLeagueSeaBossArea(areaKey)) {
         // In sea boss areas, expand the unwalkable area around land so that the boss doesn't overlap it
         _graph.collision.type = ColliderType.Sphere;
         _graph.collision.diameter = 6f;
      } else if (AreaManager.self.isSeaArea(areaKey)) {
         _graph.collision.type = ColliderType.Sphere;
         _graph.collision.diameter = 0.15f;
      } else {
         // For non-sea maps, use collider sphere to avoid NPCs moving through colliders
         _graph.collision.type = ColliderType.Sphere;

         // 1.5 diameter (1 block of empty space + cut corners) for 0.16 grid size
         _graph.collision.diameter = 9.375f * _firstTilemap.cellSize.x * GetComponentInChildren<Grid>().transform.localScale.x;
      }
      _graph.collision.mask = LayerMask.GetMask("GridColliders");
      _graph.Scan();
   }

   public void enableSeaShops () {
      PvpShopEntity[] pvpShopEntities = prefabParent.GetComponentsInChildren<PvpShopEntity>(true);
      foreach (PvpShopEntity shopEntity in pvpShopEntities) {
         shopEntity.gameObject.SetActive(true);
      }
   }

   public void rescanGraph () {
      if (_graph != null) {
         _graph.Scan();
      }
   }

   #region Private Variables

   // Stores the Tilemaps for this area
   protected List<TilemapLayer> _tilemapLayers = new List<TilemapLayer>();

   // Stores the mountain tilemaps for this area
   protected List<TilemapLayer> _mountainTilemapLayers = new List<TilemapLayer>();

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

   // The list of warps in this area
   [SerializeField]
   protected List<Warp> _warps = new List<Warp>();

   // The lits of action triggers "warping to league"
   protected List<GenericActionTrigger> _warpToLeagueTrigger = new List<GenericActionTrigger>();

   // The list of temporary controllers in this rea
   protected List<TemporaryController> _tempControllers = new List<TemporaryController>();

   #endregion
}
