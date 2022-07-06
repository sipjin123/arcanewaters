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

   // The type of map download for logging
   public enum MapDownloadType { None = 0, Nubis = 1, Cache = 2, DBMain = 3 };

   // The queuedMapCreation
   public List<LiveMapData> queuedMapCreation = new List<LiveMapData>();

   #endregion

   private void Awake () {
      self = this;
   }

   public void createLiveMap (string areaKey) {
      createLiveMap(areaKey, areaKey, Biome.Type.None);
   }

   public void createLiveMap (string areaKey, string baseMapAreaKey, Biome.Type groupInstanceBiome) {
      LiveMapData newLiveMapData = new LiveMapData {
         areaKey = areaKey,
         mapInfo = new MapInfo(baseMapAreaKey, null, -1),
         mapPos = getNextMapPosition(),
         mapCustomData = null,
         biome = groupInstanceBiome
      };

      // Set this to false to revert to old system without the queueing
      bool useMapQueueing = true;
      if (useMapQueueing) {
         if (queuedMapCreation.Count > 0) {
            queuedMapCreation.Add(newLiveMapData);
            string message = "Added new content to queue {" + areaKey + "}{" + newLiveMapData.biome + "}{" + queuedMapCreation.Count + "}";
            if (Util.isCloudBuild()) {
               D.debug(">>>==<<<" + message);
            } else {
               D.editorLog(message, Color.yellow);
            }
         } else {
            string message = "Creating new map {" + newLiveMapData.areaKey + "}{" + newLiveMapData.biome + "}{" + queuedMapCreation.Count + "}";
            if (Util.isCloudBuild()) {
               D.debug("===>>>" + message);
            } else {
               D.editorLog(message, Color.green);
            }
            queuedMapCreation.Add(newLiveMapData);
            createLiveMap(newLiveMapData.areaKey, newLiveMapData.mapInfo, newLiveMapData.mapPos, newLiveMapData.mapCustomData, newLiveMapData.biome);
         }
      } else {
         createLiveMap(newLiveMapData.areaKey, newLiveMapData.mapInfo, newLiveMapData.mapPos, newLiveMapData.mapCustomData, newLiveMapData.biome);
      }
   }

   public void createLiveMap (string areaKey, MapInfo mapInfo, Vector3 mapPosition, MapCustomizationData customizationData, Biome.Type biome, MapManager.MapDownloadType mapDownloadType = MapDownloadType.None) {
      double startTime = NetworkTime.time;
      D.editorLog("Creating Live Map {" + startTime.ToString("f1") + "}{" + areaKey + "}", Color.yellow);

      // If the area already exists, don't create it again
      if (AreaManager.self.hasArea(areaKey)) {
         List<LiveMapData> queuedMap = queuedMapCreation.FindAll(_ => _.areaKey == areaKey
         && _.mapPos == mapPosition
         && _.biome == biome);
         if (queuedMap.Count > 0) {
            queuedMapCreation.Remove(queuedMap[0]);
            continueQueue();
         }
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

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Only read the DB on server and hosts
         if (Mirror.NetworkServer.active) {
            // Read the map data
            if (mapInfo.gameData == null) {
               string rawMapInfo = DB_Main.getMapInfo(mapInfo.mapName);
               if (rawMapInfo.Length > 0) {
                  mapInfo = JsonUtility.FromJson<MapInfo>(rawMapInfo);
                  if (mapDownloadType == MapDownloadType.None) {
                     mapDownloadType = MapDownloadType.DBMain;
                  }
               } else {
                  // For non cloud builds, attempt to fetch nubis data if database fetch does not succeed
                  if (!Util.isCloudBuild()) {
                     mapInfo = null;
                     D.debug("Failed to fetch map data");
                  }
               }
            }

            // Fetch map customization data if required
            int ownerId = CustomMapManager.getMapChangesOwnerId(areaKey);
            if (ownerId != 0 && customizationData == null) {
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
               processNubisData(startTime, areaKey, mapPosition, customizationData, biome);
            } else if (string.IsNullOrEmpty(mapInfo.gameData)) {
               D.error($"Could not find gameData from map { areaKey } in the database. Ensure that the map has a published version available.");
            } else {
               // Deserialize the map
               ExportedProject001 exportedProject = MapImporter.deserializeMapData(mapInfo, areaKey);

               if (exportedProject != null) {
                  StartCoroutine(CO_InstantiateMapData(startTime, mapInfo, exportedProject, areaKey, mapPosition, customizationData, biome, mapDownloadType));
               }
            }
         });
      });
   }

   private async void processNubisData (double startTime, string areaKey, Vector3 mapPosition, MapCustomizationData customizationData, Biome.Type groupInstanceBiome) {
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
            StartCoroutine(CO_InstantiateMapData(startTime, mapInfo, exportedProject, areaKey, mapPosition, customizationData, groupInstanceBiome, MapDownloadType.Nubis));
         }
      }
   }

   private void continueQueue () {
      string message = "";
      if (queuedMapCreation.Count > 0) {
         LiveMapData nextQueuedMap = queuedMapCreation[0];
         message = "MAP_QUEUE: Processing Next QUEUE: {" + nextQueuedMap.areaKey + "}{" + nextQueuedMap.biome + "}{" + nextQueuedMap.mapPos + "}{" + queuedMapCreation.Count + "}";
         if (Util.isCloudBuild()) {
            D.debug("--->>>" + message);
         } else {
            D.editorLog(message, Color.green);
         }
         createLiveMap(nextQueuedMap.areaKey, nextQueuedMap.mapInfo, nextQueuedMap.mapPos, nextQueuedMap.mapCustomData, nextQueuedMap.biome);
      }
   }

   private IEnumerator CO_InstantiateMapData (double startTime, MapInfo mapInfo, ExportedProject001 exportedProject, string areaKey, Vector3 mapPosition, MapCustomizationData customizationData, Biome.Type biome, MapDownloadType mapDownloadType = MapDownloadType.None) {
      PanelManager.self.loadingScreen.setProgress(LoadingScreen.LoadingType.MapCreation, 0.1f);
      yield return null;

      AssetSerializationMaps.ensureLoaded();
      MapTemplate result = Instantiate(AssetSerializationMaps.mapTemplate, mapPosition, Quaternion.identity);
      result.name = areaKey;

      double functionStartTime = NetworkTime.time;

      if (exportedProject.biome == Biome.Type.None) {
         // Biome should never be None, redownload map data using nubis and overwrite the cached map data
         D.error("Map Log: Invalid biome type NONE in map data. Redownloading map data using Nubis");
         D.adminLog("Redownload and Create map {" + areaKey + "}! Invalid Biome, current process time: {" + (NetworkTime.time - startTime).ToString("f1") + " secs}" +
            "{" + mapDownloadType + "} EndTime:{" + NetworkTime.time.ToString("f1") + "}", D.ADMIN_LOG_TYPE.Performance);
         downloadAndCreateMap(areaKey, areaKey, mapInfo.version, mapPosition, customizationData, biome);
      } else {
         // Create the area
         Area area = result.area;

         // Overwrite the default area biome if another is given
         if (biome != Biome.Type.None) {
            exportedProject.biome = biome;
         } else {
            biome = exportedProject.biome;
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

         area.initializeTileAttributeMatrix(bounds);

         // Launch matrix fill concurrently
         UnityThreading.Task fillMatrixTask = UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            // Fill up tile attribute matrix
            foreach (ExportedLayer001 layer in exportedProject.layers) {
               area.bkg_fillTileAttributesMatrix(layer);
            }
         });

         yield return null;

         // Create visual tiles
         List<TilemapLayer> tilemaps = new List<TilemapLayer>();
         int tileProcessedInFrame = 0;
         foreach (ExportedLayer001 layer in exportedProject.layers.OrderByDescending(layer => layer.z)) {
            int processedTiles = 0;

            // Punish the proccessed counter a little for starting a new layer
            tileProcessedInFrame += _setTilesTileBuffer.Length / 4;

            // Create the tilemap gameobject
            var tilemap = Instantiate(AssetSerializationMaps.tilemapTemplate, result.tilemapParent);
            tilemap.transform.localPosition = new Vector3(0, 0, layer.z);
            tilemap.gameObject.name = layer.name + " " + layer.sublayer;

            while (processedTiles < layer.tiles.Length) {
               // Balance load among multiple frames
               if (tileProcessedInFrame >= _setTilesTileBuffer.Length) {
                  tileProcessedInFrame = 0;
                  yield return new WaitForEndOfFrame();
               }

               // If we have a lot of tiles, lets use precreated buffer, otherwise create containers dynamically
               TileBase[] tiles = _setTilesTileBuffer;
               Vector3Int[] positions = _setTilesPositionBuffer;
               if (layer.tiles.Length - processedTiles < _setTilesTileBuffer.Length) {
                  tiles = new TileBase[layer.tiles.Length - processedTiles];
                  positions = new Vector3Int[layer.tiles.Length - processedTiles];
               }

               // Fill the buffers
               for (int i = 0; i < tiles.Length; i++) {
                  tiles[i] = AssetSerializationMaps.getTile(
                     layer.tiles[processedTiles].i, layer.tiles[processedTiles].j, biome);
                  positions[i] = new Vector3Int(layer.tiles[processedTiles].x, layer.tiles[processedTiles].y, 0);

                  processedTiles++;
                  tileProcessedInFrame++;
               }

               // Update z axis sorting for building layer
               if (layer.name.Contains(Area.BUILDING_LAYER)) {
                  for (int i = 0; i < positions.Length; i++) {
                     Vector3Int pos = positions[i];
                     positions[i] = new Vector3Int(pos.x, pos.y, ZSnap.getZ(pos));
                  }
               }

               // Ensure the 'sprite' of animated tiles is set
               foreach (TileBase tileBase in tiles) {
                  AnimatedTile aTile = tileBase as AnimatedTile;
                  if (aTile != null) {
                     if (aTile.sprite == null && aTile.m_AnimatedSprites.Length > 0) {
                        aTile.sprite = aTile.m_AnimatedSprites[0];
                     }
                  }
               }

               // Set the tiles in the tilemap
               tilemap.SetTiles(positions, tiles);
            }

            // Add the layer to the list
            tilemaps.Add(new TilemapLayer {
               tilemap = tilemap,
               fullName = layer.name + " " + layer.sublayer ?? "",
               name = layer.name ?? "",
               type = layer.type
            });
         }

         PanelManager.self.loadingScreen.setProgress(LoadingScreen.LoadingType.MapCreation, 0.4f);

         yield return null;

         // Lets wait for matrix tile fill to finish up
         while (!fillMatrixTask.HasEnded && !fillMatrixTask.IsFailed && !fillMatrixTask.IsSucceeded) {
            yield return null;
         }

         if (exportedProject.tileCollisionShapes == null || exportedProject.tileCollisionShapes.Length == 0) {
            // Use legacy method if collision shapes weren't baked

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

            result.area.setColliderChunks(mapColliderChunks);
         } else {
            result.staticColliderParent.localPosition = new Vector3(
               -exportedProject.size.x * 0.5f * 0.16f,
               -exportedProject.size.y * 0.5f * 0.16f,
               0);

            List<PolygonCollider2D> staticColliders = new List<PolygonCollider2D>();

            // Create static collider shapes
            foreach (TileCollisionShape shape in exportedProject.tileCollisionShapes) {
               // Skip shape if it has no paths
               if (shape.paths.Length == 0) {
                  continue;
               }

               PolygonCollider2D col = Instantiate(AssetSerializationMaps.staticWorldColliderTemplate, result.staticColliderParent);
               col.transform.localPosition = Vector3.zero;
               staticColliders.Add(col);

               col.pathCount = shape.paths.Length;
               for (int i = 0; i < shape.paths.Length; i++) {
                  col.SetPath(i, shape.paths[i].points);
               }
            }

            result.area.setStaticColliders(staticColliders);
         }

         PanelManager.self.loadingScreen.setProgress(LoadingScreen.LoadingType.MapCreation, 0.5f);

         result.area.setTilemapLayers(tilemaps);

         // Disable map borders
         result.leftBorder.gameObject.SetActive(false);
         result.rightBorder.gameObject.SetActive(false);
         result.topBorder.gameObject.SetActive(false);
         result.bottomBorder.gameObject.SetActive(false);

         yield return null;
         yield return StartCoroutine(MapImporter.CO_InstantiatePrefabs(mapInfo, exportedProject, result.prefabParent, result.npcParent, result.area));
         yield return null;

         PanelManager.self.loadingScreen.setProgress(LoadingScreen.LoadingType.MapCreation, 0.7f);

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

         MapImporter.setCameraBounds(result, bounds);
         MapImporter.addEdgeColliders(result, bounds);

         // Initialize the area
         yield return null;
         area.initialize();
         yield return null;

         PanelManager.self.loadingScreen.setProgress(LoadingScreen.LoadingType.MapCreation, 0.9f);

         // Destroy the template component
         Destroy(result);

         // Add the frame that will wrap the area
         addGenericFrame(area, bounds);

         area.vcam.GetComponent<MyCamera>().setInternalOrthographicSize();

         onAreaCreationIsFinished(area, biome);

         PanelManager.self.loadingScreen.setProgress(LoadingScreen.LoadingType.MapCreation, 1f);

         string zabbixData = "";
         if (Mirror.NetworkServer.active) {
            string currentCpuUsage = PerformanceUtil.getZabbixCpuUsage().ToString("f1");
            string currentRamUsage = PerformanceUtil.getZabbixRamUsage().ToString("f1");
            zabbixData = "CPU:{" + currentCpuUsage + "} Ram:{" + currentRamUsage + "}";
         }
         D.adminLog("Proceed to Map Creation {" + areaKey + "}, current process time: {" + (NetworkTime.time - startTime).ToString("f1") + " secs}" +
            "Type:{" + mapDownloadType + "} EndTime:{" + NetworkTime.time.ToString("f1") + "}" + zabbixData, D.ADMIN_LOG_TYPE.Performance);

         float elapsedTime = (float) (NetworkTime.time - functionStartTime);
         D.debug("[Timing] MapManager CO_InstantiateMapData for area: " + areaKey + " took " + elapsedTime.ToString("F2") + "seconds.");

         List<LiveMapData> queuedMap = queuedMapCreation.FindAll(_ => _.areaKey == areaKey);
         if (queuedMap.Count > 0) {
            queuedMapCreation.Remove(queuedMap[0]);
            if (Mirror.NetworkServer.active) {
               if (ServerNetworkingManager.self.doesLocalServerHaveQueuedArea(areaKey)) {
                  D.adminLog("Removed area being generated for area {" + areaKey + "}", D.ADMIN_LOG_TYPE.Performance);
                  ServerNetworkingManager.self.server.areaBeingGenerated.Remove(areaKey);
               }
            }
            string message = "MAP_QUEUE: Cleared map queue for area {" + areaKey + "}{" + biome + "}{" + queuedMapCreation.Count + "}";
            if (Util.isCloudBuild()) {
               D.debug("<<<===" + message);
            } else {
               D.editorLog(message, Color.green);
            }

            continueQueue();
         } else {
            if (queuedMapCreation.Count > 0) {
               string message = "MAP_QUEUE: Failed to Clear map queue for area {" + areaKey + "}{" + biome + "}{" + mapPosition + "}{" + queuedMapCreation.Count + "}!!";
               if (Util.isCloudBuild()) {
                  D.debug(">>>xxx<<<" + message);
               } else {
                  D.editorLog(message, Color.red);
               }
               foreach (var temp in queuedMapCreation) {
                  D.editorLog("--" + temp.areaKey + " " + temp.biome + " " + temp.mapInfo.displayName, Color.white);
               }
            }
         }
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
      CustomizablePrefab newPrefab = null;
      bool isUserSpecificKey = CustomMapManager.isUserSpecificAreaKey(area.areaKey);
      bool isGuildSpecificKey = CustomMapManager.isGuildSpecificAreaKey(area.areaKey) || CustomMapManager.isGuildHouseAreaKey(area.areaKey);
      if (!hasFoundAnyChanges && isUserSpecificKey && Global.player != null && !changes.deleted) {
         int userOwner = CustomMapManager.getUserId(area.areaKey);
         if (userOwner != Global.player.userId) {
            newPrefab = createPrefab(area, biome, changes, true);
         }
      } else if (!hasFoundAnyChanges && isGuildSpecificKey && Global.player != null && !changes.deleted) {
         if (Global.player.guildId == CustomMapManager.getGuildId(area.areaKey)) {
            newPrefab = createPrefab(area, biome, changes, true);
         }
      }
      if (Mirror.NetworkClient.active && newPrefab != null) {
         if (MapCustomizationManager.tryGetCurentLocalManager(out MapCustomizationManager man)) {
            man.startTracking(newPrefab);
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
         createLiveMap(areaKey, new MapInfo(baseMapAreaKey, mapData, version), mapPosition, customizationData, biome, MapManager.MapDownloadType.Nubis);
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

   // Buffers we use for setting tiles, size controls how many tiles we'll process in 1 frame
   private TileBase[] _setTilesTileBuffer = new TileBase[2048];
   private Vector3Int[] _setTilesPositionBuffer = new Vector3Int[2048];

   #endregion
}
