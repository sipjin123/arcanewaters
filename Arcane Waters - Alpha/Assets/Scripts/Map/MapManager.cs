﻿using UnityEngine;
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

   #endregion

   private void Awake () {
      self = this;
   }

   public void createLiveMap (string areaKey) {
      createLiveMap(areaKey, areaKey);
   }

   public void createLiveMap (string areaKey, string baseMapAreaKey) {
      createLiveMap(areaKey, new MapInfo(baseMapAreaKey, null, -1), getNextMapPosition(), null);
   }

   public void createLiveMap (string areaKey, MapInfo mapInfo, Vector3 mapPosition, MapCustomizationData customizationData) {
      // If the area already exists, don't create it again
      if (AreaManager.self.hasArea(areaKey)) {
         D.warning($"Area {areaKey} already exists!");
         return;
      }

      // If the area is under creation, don't create it again
      if (isAreaUnderCreation(areaKey)) {
         return;
      }

      // On clients, check if another area is currently being created
      if (!Mirror.NetworkServer.active && _areasUnderCreation.Count > 0) {
         // Only one area can be created at a time, so schedule this one for when the other finishes
         _nextAreaKey = areaKey;
         _nextMapInfo = mapInfo;
         _nextMapPosition = mapPosition;
         _nextMapCustomizationData = customizationData;
         return;
      }

      // Save the area as under creation
      _areasUnderCreation.Add(areaKey, mapPosition);

      // Show the loading screen on clients or host mode
      if (!Util.isServerNonHost()) {
         PanelManager.self.loadingScreen.show();
      }

      // Find out if we are creating an owned map, if so, get owner id
      int ownerId = -1;
      if (!areaKey.Equals(mapInfo.mapName)) {
         if (AreaManager.self.tryGetOwnedMapManager(areaKey, out OwnedMapManager ownedMapManager)) {
            if (OwnedMapManager.isUserSpecificAreaKey(areaKey)) {
               ownerId = OwnedMapManager.getUserId(areaKey);
            }
         }
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Read the map data
         if (mapInfo.gameData == null) {
            mapInfo = DB_Main.getMapInfo(mapInfo.mapName);
         }

         // Fetch map customization data if required
         if (ownerId != -1 && customizationData == null) {
            int baseMapId = DB_Main.getMapId(mapInfo.mapName);
            customizationData = DB_Main.getMapCustomizationData(baseMapId, ownerId);
         }

         // Deserialize the map
         ExportedProject001 exportedProject = MapImporter.deserializeMapData(mapInfo);

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            int mapVersion = 0;
            try {
               mapVersion = mapInfo.version;
            } catch {
               D.debug("Map info does not Exist!! Failed to fetch using DBMain: " + mapInfo);
               return;
            }
            D.debug($"Preparing to create live map {areaKey} at {mapPosition} with version {mapVersion}");

            StartCoroutine(CO_InstantiateMapData(mapInfo, exportedProject, areaKey, mapPosition, customizationData));
         });
      });
   }

   private IEnumerator CO_InstantiateMapData (MapInfo mapInfo, ExportedProject001 exportedProject, string areaKey, Vector3 mapPosition, MapCustomizationData customizationData) {
      MapImporter.ensureSerializationMapsLoaded();

      MapTemplate result = Instantiate(AssetSerializationMaps.mapTemplate, mapPosition, Quaternion.identity);
      result.name = areaKey;

      if (exportedProject.biome == Biome.Type.None) {
         D.warning("Invalid biome type NONE in map data. Setting to 'Forest'.");
         exportedProject.biome = Biome.Type.Forest;
      }

      // Create the area
      Area area = result.area;

      // Set area properties
      area.areaKey = areaKey;
      area.version = mapInfo.version;
      area.biome = exportedProject.biome;

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
      foreach (ExportedLayer001 layer in exportedProject.layers) {
         MapImporter.instantiateTilemapLayer(tilemaps, mapInfo, layer, result.tilemapParent,
            result.collisionTilemapParent, exportedProject.biome, ref unrecognizedTiles);
         PanelManager.self.loadingScreen.setPercentage(-0.4f + ((0.5f / exportedProject.layers.Length) * tilemaps.Count));
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
            mapColliderChunks.Add(MapImporter.instantiateTilemapColliderChunk(exportedProject, result.collisionTilemapParent,
               exportedProject.biome, rect));

            PanelManager.self.loadingScreen.setPercentage(0.1f + (1f / chunkCount) * mapColliderChunks.Count);
            yield return null;
         }
      }

      PanelManager.self.loadingScreen.setPercentage(1f);

      result.area.setTilemapLayers(tilemaps);
      result.area.setColliderChunks(mapColliderChunks);

      MapImporter.instantiatePrefabs(mapInfo, exportedProject, result.prefabParent, result.npcParent, result.area);
      yield return null;

      if (exportedProject.specialTileChunks != null) {
         MapImporter.addSpecialTileChunks(result, exportedProject.specialTileChunks);
         yield return null;
      }

      MapImporter.setCameraBounds(result, bounds);
      MapImporter.addEdgeColliders(result, bounds);
      yield return null;

      if (customizationData != null) {
         setCustomizations(area, customizationData);
      }

      // Destroy the template component
      Destroy(result);

      // Initialize the area
      area.initialize();

      onAreaCreationIsFinished(area);
   }

   public void onAreaCreationIsFinished (Area area) {
      // Remove the area from being under creation
      _areasUnderCreation.Remove(area.areaKey);

      // Set the area as available
      AreaManager.self.storeArea(area);

      area.vcam.VirtualCameraGameObject.SetActive(false);

      // Only remove old maps on the Clients
      if (!Mirror.NetworkServer.active && _lastMap != null && _lastMap.areaKey != area.areaKey) {
         AreaManager.self.removeArea(_lastMap.areaKey);
         Destroy(_lastMap.gameObject);
      }
      _lastMap = area;

      // Send signal to player to update virtual camera after area is created
      if (Global.player != null) {
         Global.player.updatePlayerCamera();
      }

      PanelManager.self.loadingScreen.hide();

      // On clients, if an area is scheduled to be created next, start the process now
      if (!Mirror.NetworkServer.active && _nextAreaKey != null) {
         createLiveMap(_nextAreaKey, _nextMapInfo, _nextMapPosition, _nextMapCustomizationData);
         _nextAreaKey = null;
      }
   }

   public void setCustomizations (Area area, MapCustomizationData customizationData) {
      try {
         Dictionary<int, CustomizablePrefab> prefabs = area.gameObject.GetComponentsInChildren<CustomizablePrefab>().ToDictionary(p => p.unappliedChanges.id, p => p);

         foreach (PrefabChanges pc in customizationData.prefabChanges) {
            if (prefabs.TryGetValue(pc.id, out CustomizablePrefab pref)) {
               pref.revertToMapEditor();
               pref.unappliedChanges = pc;
               pref.submitChanges();
            }
         }
      } catch (Exception ex) {
         D.error($"Unable to set customizations for area { area.areaKey }:\n{ ex }");
      }
   }

   public void addCustomizations (Area area, PrefabChanges prefabChanges) {
      foreach (CustomizablePrefab pref in area.gameObject.GetComponentsInChildren<CustomizablePrefab>()) {
         if (pref.unappliedChanges.id == prefabChanges.id) {
            pref.unappliedChanges = prefabChanges;
            pref.submitChanges();
         }
      }
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

   public IEnumerator CO_DownloadAndCreateMap (string areaKey, string baseMapAreaKey, int version, Vector3 mapPosition, MapCustomizationData customizationData) {
      // Request the map from Nubis Cloud
      string nubisDirectory = "http://" + Global.getAddress(MyNetworkManager.ServerType.AmazonVPC) + ":7900/"; 
      UnityWebRequest www = UnityWebRequest.Get(nubisDirectory + "fetch_map_data_v1?mapName=" + baseMapAreaKey);  
      yield return www.SendWebRequest();

      if (www.isNetworkError || www.isHttpError) {
         D.warning(www.error);
      } else {
         // Grab the map data from the request
         string mapData = www.downloadHandler.text;

         // Store it for later reference
         MapCache.storeMapData(baseMapAreaKey, version, mapData);

         // Spawn the Area using the map data
         createLiveMap(areaKey, new MapInfo(baseMapAreaKey, mapData, version), mapPosition, customizationData);
      }
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

   // The list of areas under creation and their position
   private Dictionary<string, Vector2> _areasUnderCreation = new Dictionary<string, Vector2>();

   #endregion
}
