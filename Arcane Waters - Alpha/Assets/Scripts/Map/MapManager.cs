using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool;
using MapCreationTool.Serialization;
using UnityEngine.Networking;
using UnityEngine.Tilemaps;
using System.Linq;
using System;
using MapCustomization;

public class MapManager : MonoBehaviour
{
   #region Public Variables

   // The size of the squared chunk of tilemap colliders
   public static int TILEMAP_COLLIDERS_CHUNK_SIZE = 32;

   // Convenient self reference
   public static MapManager self;

   // The x coordinates for the Open World
   public const string OPEN_WORLD_MAP_COORDS_X = "ABCDEFGHIJKLMNO";

   // The y coordinates for the Opoen World
   public const string OPEN_WORLD_MAP_COORDS_Y = "012345678";

   // Open World Map Prefix
   public const string OPEN_WORLD_MAP_PREFIX = "world_map_";

   #endregion

   private void Awake () {
      self = this;
   }

   public void createLiveMap (string areaKey) {
      createLiveMap(areaKey, areaKey, Biome.Type.None);
   }

   public void createLiveMap (string areaKey, string baseMapAreaKey, Biome.Type voyageBiome) {
      createLiveMap(areaKey, new MapInfo(baseMapAreaKey, null, -1), getNextMapPosition(), null, voyageBiome);
   }

   public void createLiveMap (string areaKey, MapInfo mapInfo, Vector3 mapPosition, MapCustomizationData customizationData, Biome.Type biome) {
      // If the area already exists, don't create it again
      if (AreaManager.self.hasArea(areaKey)) {
         D.debug("The area already exists and won't be created again: " + areaKey);
         return;
      }

      // If the area is under creation, don't create it again
      if (isAreaUnderCreation(areaKey)) {
         D.debug("Cant create live map due to area under creation: " + areaKey);
         return;
      }

      // On clients, check if another area is currently being created
      if (!Mirror.NetworkServer.active && _areasUnderCreation.Count > 0) {
         // Only one area can be created at a time, so schedule this one for when the other finishes
         _nextAreaKey = areaKey;
         _nextMapInfo = mapInfo;
         _nextMapPosition = mapPosition;
         _nextMapCustomizationData = customizationData;
         _nextBiome = biome;
         return;
      }

      // Save the area as under creation
      _areasUnderCreation.Add(areaKey, mapPosition);

      // Find out if we are creating an owned map, if so, get owner id
      int ownerId = -1;
      if (!areaKey.Equals(mapInfo.mapName)) {
         if (AreaManager.self.tryGetCustomMapManager(areaKey, out CustomMapManager customMapManager)) {
            if (CustomMapManager.isUserSpecificAreaKey(areaKey)) {
               ownerId = CustomMapManager.getUserId(areaKey);
            }
         }
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Only read the DB on server and hosts
         if (Mirror.NetworkServer.active) {
            // Read the map data
            if (mapInfo.gameData == null) {
               string rawMapInfo = DB_Main.getMapInfo(mapInfo.mapName);
               if (rawMapInfo.Length > 0) {
                  mapInfo = JsonUtility.FromJson<MapInfo>(rawMapInfo);
               } else {
                  // For non cloud builds, attempt to fetch nubis data if database fetch does not succeed
                  if (!Util.isCloudBuild()) {
                     mapInfo = null;
                     D.debug("Failed to fetch map data");
                  }
               }
            }

            // Fetch map customization data if required
            if ((ownerId != -1 || (CustomMapManager.isGuildSpecificAreaKey(areaKey)) || CustomMapManager.isGuildHouseAreaKey(areaKey)) && customizationData == null) {
               if (mapInfo != null) {
                  int baseMapId = DB_Main.getMapId(mapInfo.mapName);
                  customizationData = DB_Main.exec((cmd) => DB_Main.getMapCustomizationData(cmd, baseMapId, ownerId));
               } else {
                  D.debug("Null map info!");
               }
            }
         }

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (mapInfo == null) {
               D.debug($"Could not find entry for map { areaKey } in the database, proceeding to Nubis fetch");

               // If db_main fails to return due to connection issues, attempt nubis connection
               processNubisData(areaKey, mapPosition, customizationData, biome);
            } else if (string.IsNullOrEmpty(mapInfo.gameData)) {
               D.error($"Could not find gameData from map { areaKey } in the database. Ensure that the map has a published version available.");
            } else {
               // Deserialize the map
               ExportedProject001 exportedProject = MapImporter.deserializeMapData(mapInfo, areaKey);

               if (exportedProject != null) {
                  StartCoroutine(CO_InstantiateMapData(mapInfo, exportedProject, areaKey, mapPosition, customizationData, biome));
               }
            }
         });
      });
   }

   private async void processNubisData (string areaKey, Vector3 mapPosition, MapCustomizationData customizationData, Biome.Type voyageBiome) {
      D.editorLog("Attempting to fetch using Nubis Data for area {" + areaKey + "}", Color.green);

      // Request the map from Nubis Cloud
      string mapData = await NubisClient.call<string>(nameof(DB_Main.getMapInfo), areaKey);
      if (mapData != null) {
         D.editorLog("Done fetching Nubis data: " + mapData.Length, Color.green);
      } else {
         D.debug("Nubis Failed for area {" + areaKey + "}");
      }
      if (mapData == null || (string.IsNullOrWhiteSpace(mapData) && mapData.Length < 10)) {
         D.debug("Error in retrieving map data from NUBIS: (" + areaKey + ")");
      } else {
         MapInfo mapInfo = JsonUtility.FromJson<MapInfo>(mapData);

         // Deserialize the map
         ExportedProject001 exportedProject = MapImporter.deserializeMapData(mapInfo, areaKey);

         if (exportedProject != null) {
            StartCoroutine(CO_InstantiateMapData(mapInfo, exportedProject, areaKey, mapPosition, customizationData, voyageBiome));
         }
      }
   }

   private IEnumerator CO_InstantiateMapData (MapInfo mapInfo, ExportedProject001 exportedProject, string areaKey, Vector3 mapPosition, MapCustomizationData customizationData, Biome.Type biome) {
      AssetSerializationMaps.ensureLoaded();
      MapTemplate result = Instantiate(AssetSerializationMaps.mapTemplate, mapPosition, Quaternion.identity);
      result.name = areaKey;

      if (exportedProject.biome == Biome.Type.None) {
         // Biome should never be None, redownload map data using nubis and overwrite the cached map data
         D.error("Map Log: Invalid biome type NONE in map data. Redownloading map data using Nubis");
         downloadAndCreateMap(areaKey, areaKey, mapInfo.version, mapPosition, customizationData, biome);
      } else {
         // Create the area
         Area area = result.area;

         // Overwrite the default area biome if another is given
         if (biome != Biome.Type.None) {
            exportedProject.biome = biome;
         }

         // Set area properties
         area.areaKey = areaKey;
         area.baseAreaKey = mapInfo.mapName;
         area.version = mapInfo.version;
         area.biome = biome;
         area.mapTileSize = exportedProject.size;

         area.cloudManager.enabled = false;
         area.cloudShadowManager.enabled = false;

         if (exportedProject.editorType == EditorType.Sea) {
            area.isSea = true;
         } else if (exportedProject.editorType == EditorType.Interior) {
            area.isInterior = true;
         }

         // Calculate the map bounds
         Bounds bounds = MapImporter.calculateBounds(exportedProject);
         yield return null;

         List<TilemapLayer> tilemaps = new List<TilemapLayer>();
         int unrecognizedTiles = 0;

         // Create one layer per frame
         foreach (ExportedLayer001 layer in exportedProject.layers.OrderByDescending(layer => layer.z)) {
            MapImporter.instantiateTilemapLayer(tilemaps, mapInfo, layer, result.tilemapParent,
               result.collisionTilemapParent, exportedProject.biome, ref unrecognizedTiles);

            area.fillTileAttributesMatrix(layer);

            PanelManager.self.loadingScreen.setProgress(LoadingScreen.LoadingType.MapCreation, (0.1f / exportedProject.layers.Length) * tilemaps.Count);
            yield return null;
         }

         if (unrecognizedTiles > 0) {
            Utilities.warning($"Could not recognize { unrecognizedTiles } tiles of map { mapInfo.mapName }");
         }

         // Prepare the list of tilemap colliders
         List<MapChunk> mapColliderChunks = new List<MapChunk>();

         // Calculate the number of chunks
         int chunkCount = (int) ((bounds.max.x - bounds.min.x) * (bounds.max.y - bounds.min.y) / (TILEMAP_COLLIDERS_CHUNK_SIZE * TILEMAP_COLLIDERS_CHUNK_SIZE));

         // Create the tilemap colliders in chunks
         for (int i = (int) bounds.min.x; i < bounds.max.x; i += TILEMAP_COLLIDERS_CHUNK_SIZE) {
            for (int j = (int) bounds.min.y; j < bounds.max.y; j += TILEMAP_COLLIDERS_CHUNK_SIZE) {
               // Calculate the chunk rect
               int xMax = Mathf.Clamp(i + TILEMAP_COLLIDERS_CHUNK_SIZE, 0, (int) bounds.max.x);
               int yMax = Mathf.Clamp(j + TILEMAP_COLLIDERS_CHUNK_SIZE, 0, (int) bounds.max.y);
               RectInt rect = new RectInt(i, j, TILEMAP_COLLIDERS_CHUNK_SIZE, TILEMAP_COLLIDERS_CHUNK_SIZE);

               // Instantiate the colliders
               mapColliderChunks.Add(MapImporter.instantiateTilemapColliderChunk(result.area.areaKey, exportedProject, result.collisionTilemapParent,
                  exportedProject.biome, rect));

               PanelManager.self.loadingScreen.setProgress(LoadingScreen.LoadingType.MapCreation, 0.1f + (0.8f / chunkCount) * mapColliderChunks.Count);
               yield return null;
            }
         }

         result.area.setTilemapLayers(tilemaps);
         result.area.setColliderChunks(mapColliderChunks);

         // Disable map borders
         result.leftBorder.gameObject.SetActive(false);
         result.rightBorder.gameObject.SetActive(false);
         result.topBorder.gameObject.SetActive(false);
         result.bottomBorder.gameObject.SetActive(false);

         MapImporter.instantiatePrefabs(mapInfo, exportedProject, result.prefabParent, result.npcParent, result.area);
         yield return null;

         if (exportedProject.specialTileChunks != null) {
            MapImporter.addSpecialTileChunks(result, exportedProject.specialTileChunks);
            yield return null;
         }

         // Set up Flock Manager if it's a sea map, delete otherwise
         if (exportedProject.editorType == EditorType.Sea) {
            result.flockManager.spawnBox.size = bounds.size * 0.16f;
         } else {
            Destroy(result.flockManager.gameObject);
         }

         // Cut a hole in the background of the same size than the area
         Material backgroundMaterial = result.backgroundRenderer.material;
         backgroundMaterial.SetVector("_Center", result.backgroundRenderer.transform.position);
         backgroundMaterial.SetVector("_Size", bounds.size * 0.16f);

         yield return null;

         if (customizationData != null) {
            setCustomizations(area, exportedProject.biome, customizationData);
         }

         // Set up cell types container
         result.area.cellTypes = new CellTypesContainer(exportedProject.mapCellTypes, exportedProject.size, result.area);

         // Destroy the template component
         Destroy(result);

         // Initialize the area
         area.initialize();

         MapImporter.setCameraBounds(result, bounds);
         MapImporter.addEdgeColliders(result, bounds);

         // Add the frame that will wrap the area
         addGenericFrame(area, bounds);

         area.vcam.GetComponent<MyCamera>().setInternalOrthographicSize();

         onAreaCreationIsFinished(area, biome);
      }
   }

   private static void addGenericFrame (Area area, Bounds bounds) {
      GameObject genericFrame = Instantiate(PrefabsManager.self.genericFramePrefab, area.transform);

      // Nudge the frame forward, to avoid being clipped by the background's shader's clipping logic
      genericFrame.transform.localPosition = new Vector3(0, 0, -4.1f);

      // Resize frame to wrap the area
      if (genericFrame.TryGetComponent(out SpriteRenderer spriteRenderer)) {
         spriteRenderer.size = new Vector2(0.16f * bounds.size.x + 0.10f, 0.16f * bounds.size.y + 0.09f);
      }
   }

   public void onAreaCreationIsFinished (Area area, Biome.Type biome) {
      D.debug("Area creation is Finished: " + area.areaKey);

      // Remove the area from being under creation
      _areasUnderCreation.Remove(area.areaKey);

      // Set the area as available
      AreaManager.self.storeArea(area);

      // Hide loading screen once player is ready
      StartCoroutine(CO_HideLoadingScreen());

      area.vcam.VirtualCameraGameObject.SetActive(false);

      // Only remove old maps on the Clients
      if (!Mirror.NetworkServer.active && _lastMap != null && _lastMap.areaKey != area.areaKey) {
         destroyLastMap();
      }
      _lastMap = area;

      // Send signal to player to update virtual camera after area is created
      if (Global.player != null) {
         Global.player.updatePlayerCamera();
         PostSpotFader.self.recalibrateSpotPosition();
      }

      // Notify plantable tree manager that we have a new area
      PlantableTreeManager.self.areaCreationFinished(area.areaKey);

      // Wait for server to finish deploying entities before requesting npc quests in area from the server
      StartCoroutine(CO_ProcessNpcQuestInArea());

      // On clients, if an area is scheduled to be created next, start the process now
      if (!Mirror.NetworkServer.active && _nextAreaKey != null) {
         createLiveMap(_nextAreaKey, _nextMapInfo, _nextMapPosition, _nextMapCustomizationData, _nextBiome);
         _nextAreaKey = null;
      }

      if (area.isInterior && Global.player != null) {
         SoundEffectManager.self.playDoorSfx(SoundEffectManager.DoorAction.Close, biome, Global.player.transform.position);
      }

      // Invoke canvas checked in 5 seconds after map load
      Invoke("CanvasChecker", 5);
   }

   private void CanvasChecker () {
      // Disable canvases for server in batch mode
      if (Util.isBatch()) {
         foreach (var obj in FindObjectsOfType<Canvas>()) {
            // Disable main canvas game object
            obj.gameObject.SetActive(false);
         }
      }
   }

   private IEnumerator CO_ProcessNpcQuestInArea () {
      bool isCloudBuild = Util.isCloudBuild();

      // Skip this process if its a server or if its a development/nonCloud build
      if (!(Mirror.NetworkServer.active && isCloudBuild)) {
         // Check first if player exists
         while (Global.player == null) {
            yield return 0;
         }

         // Wait for instance to generate
         while (!InstanceManager.self.getClientInstance(Global.player.instanceId)) {
            yield return 0;
         }

         // Wait for instance to finish spawning the network entities
         Instance instance = InstanceManager.self.getClientInstance(Global.player == null ? -1 : Global.player.instanceId);
         while (!instance.isNetworkPrefabInstantiationFinished) {
            yield return 0;
         }

         Global.player.rpc.Cmd_RequestNPCQuestInArea();
      }
   }

   private IEnumerator CO_HideLoadingScreen () {
      if (Util.isServerNonHost()) {
         yield break;
      }

      PanelManager.self.loadingScreen.setProgress(LoadingScreen.LoadingType.MapCreation, 0.9f);

      // Check that player exists
      while (Global.player == null || Global.player.areaKey == null) yield return new WaitForEndOfFrame();

      // Check that player is registered in entity manager
      while (EntityManager.self.getEntity(Global.player.userId) == null) yield return new WaitForEndOfFrame();

      // Check if area is already created
      Area area = null;
      while ((area = AreaManager.self.getArea(Global.player.areaKey)) == null) yield return new WaitForEndOfFrame();

      // Check that player is added to that area
      while (area.userParent != Global.player.transform.parent) yield return new WaitForEndOfFrame();

      PanelManager.self.loadingScreen.hide(LoadingScreen.LoadingType.MapCreation);
   }

   public void setCustomizations (Area area, Biome.Type biome, MapCustomizationData customizationData) {
      try {
         Dictionary<int, CustomizablePrefab> prefabs = area.gameObject.GetComponentsInChildren<CustomizablePrefab>().ToDictionary(p => p.unappliedChanges.id, p => p);

         foreach (PrefabState state in customizationData.prefabChanges) {
            if (state.created) {
               createPrefab(area, biome, state, true);
            } else {
               if (prefabs.TryGetValue(state.id, out CustomizablePrefab pref)) {
                  pref.revertToMapEditor();
                  pref.unappliedChanges = state;
                  pref.submitUnappliedChanges();
               }
            }
         }
      } catch (Exception ex) {
         D.error($"Unable to set customizations for area { area.areaKey }:\n{ ex }");
      }
   }

   public void addCustomizations (Area area, Biome.Type biome, PrefabState changes) {
      bool hasFoundAnyChanges = false;
      foreach (CustomizablePrefab pref in area.gameObject.GetComponentsInChildren<CustomizablePrefab>()) {
         if (pref.unappliedChanges.id == changes.id) {
            hasFoundAnyChanges = true;
            pref.unappliedChanges = changes;
            pref.submitUnappliedChanges();

            if (!Mirror.NetworkServer.active && pref.customizedState.serializationId != changes.serializationId) {
               Debug.LogWarning("Different serialization ids for prefab " + pref.name);
            }
            break;
         }
      }

      // Check if there are any prefab changes that occurred that does not exist for visiting users
      bool isUserSpecificKey = CustomMapManager.isUserSpecificAreaKey(area.areaKey);
      bool isGuildSpecificKey = CustomMapManager.isGuildSpecificAreaKey(area.areaKey) || CustomMapManager.isGuildHouseAreaKey(area.areaKey);
      if (!hasFoundAnyChanges && isUserSpecificKey && Global.player != null) {
         int userOwner = CustomMapManager.getUserId(area.areaKey);
         if (userOwner != Global.player.userId) {
            createPrefab(area, biome, changes, true);
         }
      } else if (!hasFoundAnyChanges && isGuildSpecificKey && Global.player != null) {
         if (Global.player.guildId == CustomMapManager.getGuildId(area.areaKey)) {
            createPrefab(area, biome, changes, true);
         }
      }

      // *** This code is causing a duplicate prop to be created when MapCustomizationManager._newPrefab is not null and pressing the "Delete" key to delete the prop.
      // *** Not sure if this code is even needed.  The customization seems to work without it.
      //if (!found && changes.created) {
      //   createPrefab(area, biome, changes, true);
      //}
   }

   public bool tryGetPlantableTree (int id, out PlantableTreeInstanceData data) {
      data = null;

      if (tryGetPlantableTree(id, out PlantableTree tree)) {
         if (tree.data == null) {
            D.warning("Data is null for tree " + id);
            return false;
         }

         data = tree.data;
         return true;
      }

      return false;
   }

   public bool tryGetPlantableTree (int id, out PlantableTree tree) {
      tree = null;

      if (_plantableTrees.TryGetValue(id, out PlantableTree d)) {
         // Tree is null, perhaps area got destroyed
         if (d == null) {
            _plantableTrees.Remove(id);
            return false;
         }

         tree = d;
         return true;
      }

      return false;
   }

   public void updatePlantableTree (int id, Area area, PlantableTreeInstanceData data, PlantableTreeDefinition treeDefinition, bool client) {
      PlantableTree tree = null;

      if (data == null) {
         // Tree got deleted, destroy it
         if (_plantableTrees.ContainsKey(id)) {
            tree = _plantableTrees[id];
            if (tree != null) {
               Destroy(tree.gameObject);
            }
            _plantableTrees.Remove(id);
         }

         return;
      }

      // Check if we have an entry in our dictionary
      if (_plantableTrees.ContainsKey(id)) {
         tree = _plantableTrees[id];
      }

      // It may still be null, create a new one in that case
      bool justCreated = false;
      if (tree == null) {
         GameObject prefGO = AssetSerializationMaps.getPrefab(treeDefinition.prefabId, area.biome, false);
         PlantableTree pref = prefGO?.GetComponent<PlantableTree>();

         if (pref == null) {
            D.error("Missing plantable tree for " + treeDefinition.id);
            return;
         }
         tree = Instantiate(pref, area.plantableTreeParent);
         _plantableTrees[id] = tree;
         justCreated = true;
      }

      // Apply any changes that were made to the tree
      tree.applyState(justCreated, data, treeDefinition, client);
   }

   public CustomizablePrefab createPrefab (Area area, Biome.Type biome, PrefabState state, bool confirmedState) {
      GameObject prefGO = AssetSerializationMaps.getPrefab(state.serializationId, biome, false);
      CustomizablePrefab pref = prefGO?.GetComponent<CustomizablePrefab>();

      if (pref == null) {
         D.error($"Could not find prefab when adding customized prefab of id: { state.serializationId }");
         return null;
      }

      // Create the prefab
      CustomizablePrefab prefab = Instantiate(pref, area.prefabParent);
      prefab.name += " (customization system)";

      // Set prefab's data
      prefab.unappliedChanges = state;
      prefab.customizedState.id = state.id;
      prefab.customizedState.serializationId = state.serializationId;

      prefab.transform.localPosition = prefab.unappliedChanges.localPosition;
      prefab.GetComponent<ZSnap>()?.snapZ();

      // If changes are confirmed, mark them as confirmed
      if (confirmedState) {
         prefab.submitUnappliedChanges();
      }

      return prefab;
   }

   public Vector3 getNextMapPosition () {
      // Every time the server creates a new map, we move the map offset
      _mapOffset.x += 200f;

      return _mapOffset;
   }

   public bool isAreaUnderCreation (string areaKey) {
      return _areasUnderCreation.ContainsKey(areaKey);
   }

   public Vector2 getAreaUnderCreationPosition (string areaKey) {
      if (_areasUnderCreation.TryGetValue(areaKey, out Vector2 position)) {
         return position;
      }
      return new Vector2();
   }

   public async void downloadAndCreateMap (string areaKey, string baseMapAreaKey, int version, Vector3 mapPosition, MapCustomizationData customizationData, Biome.Type biome) {
      // Request the map from Nubis Cloud
      string mapData = await NubisClient.call<string>(nameof(DB_Main.fetchMapData), baseMapAreaKey, version);

      if (string.IsNullOrWhiteSpace(mapData)) {
         D.debug("Error in retrieving map data from NUBIS: {" + areaKey + "}");
      } else {
         // Store it for later reference
         MapCache.storeMapData(baseMapAreaKey, version, mapData);

         // TODO: Do not Remove until this issue is completely fixed
         D.debug("Map Log: Creating map data fetched from Nubis: (" + areaKey + ") Ver: " + version);

         // Spawn the Area using the map data
         createLiveMap(areaKey, new MapInfo(baseMapAreaKey, mapData, version), mapPosition, customizationData, biome);
      }
   }

   public void destroyLastMap () {
      if (_lastMap != null) {
         AreaManager.self.removeArea(_lastMap.areaKey);
         Destroy(_lastMap.gameObject);
         _lastMap = null;
      }
   }

   public static Vector2Int getOpenWorldMapCoords (string areaKey) {
      if (Util.isEmpty(areaKey) || areaKey.Length < 2 || !areaKey.StartsWith(OPEN_WORLD_MAP_PREFIX)) {
         return new Vector2Int(-1, -1);
      }

      char cx = areaKey[areaKey.Length - 2];
      char cy = areaKey[areaKey.Length - 1];

      int x = OPEN_WORLD_MAP_COORDS_X.IndexOf(cx);
      int y = OPEN_WORLD_MAP_COORDS_Y.IndexOf(cy);

      return new Vector2Int(x, y);
   }

   public static string computeOpenWorldMapSuffix (Vector2Int mapCoords) {
      if (!areValidOpenWorldMapCoords(mapCoords)) {
         return "";
      }

      char cx = OPEN_WORLD_MAP_COORDS_X[mapCoords.x];
      char cy = OPEN_WORLD_MAP_COORDS_Y[mapCoords.y];

      return $"{cx}{cy}".ToUpper();
   }

   public static bool computeNextOpenWorldMap (string areaKey, Direction direction, out string nextMapKey) {
      Vector2Int mapCoords = getOpenWorldMapCoords(areaKey);
      nextMapKey = "";

      if (!areValidOpenWorldMapCoords(mapCoords)) {
         return false;
      }

      switch (direction) {
         case Direction.North:
            mapCoords.y += 1;
            break;
         case Direction.East:
            mapCoords.x += 1;
            break;
         case Direction.South:
            mapCoords.y -= 1;
            break;
         case Direction.West:
            mapCoords.x -= 1;
            break;
      }

      if (!areValidOpenWorldMapCoords(mapCoords)) {
         return false;
      }

      string suffix = computeOpenWorldMapSuffix(mapCoords);
      nextMapKey = $"{OPEN_WORLD_MAP_PREFIX}{suffix}";
      return true;
   }

   private static bool areValidOpenWorldMapCoords (Vector2Int mapCoords) {
      if (mapCoords.x < 0 || mapCoords.x >= OPEN_WORLD_MAP_COORDS_X.Length || mapCoords.y < 0 || mapCoords.y >= OPEN_WORLD_MAP_COORDS_Y.Length) {
         return false;
      }

      return true;
   }

   #region Private Variables

   // The current map offset being used by the server
   protected Vector3 _mapOffset = new Vector3(500f, 500f);

   // The the last visited Map by the Client
   private Area _lastMap;

   // The parameters of the next area to be created (only used on clients)
   private string _nextAreaKey = null;
   private MapInfo _nextMapInfo;
   private Vector3 _nextMapPosition;
   private MapCustomizationData _nextMapCustomizationData;
   private Biome.Type _nextBiome;

   // The list of areas under creation and their position
   private Dictionary<string, Vector2> _areasUnderCreation = new Dictionary<string, Vector2>();

   // List of plantable trees we are managing
   private Dictionary<int, PlantableTree> _plantableTrees = new Dictionary<int, PlantableTree>();

   #endregion
}
