using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Linq;
using MapCreationTool;
using MapCreationTool.Serialization;
using System;

public class WorldMapSpotsGenerationTool : MonoBehaviour
{
   #region Public Variables

   // The name of the tool
   public const string TOOL_NAME = "World Map Spots Generation Tool";

   #endregion

   #region Public Methods

   [MenuItem("Util/Generate World Map Spots")]
   public static void generateWorldMapSpots () {
      // Prompt the user
      if (!EditorUtility.DisplayDialog($"{TOOL_NAME}", "Do you want to generate the World Map Spots (League Entrances, POIs, Discoveries, Warps, etc...) ?", "Yes", "No")) {
         Debug.Log("World Map Spots Generation Tool: Operation cancelled by the user.");
         return;
      }

      // Clear World Map Spots
      clearWorldMapSpotsAllAreas();

      // Generate World Map Spots
      uploadWorldMapSpotsAllAreas();

      Debug.Log($"{TOOL_NAME}: Spots Generation Completed.");
      EditorUtility.ClearProgressBar();
      EditorUtility.DisplayDialog($"{TOOL_NAME}", "Spots Generation Completed.", "Yes");
   }

   public static void clearWorldMapSpotsAllAreas () {
      DB_Main.clearWorldMapSpots();
   }

   public static void uploadWorldMapSpotsAllAreas (bool silent = false) {
      List<string> mapList = WorldMapManager.getAllAreasList().ToList();
      Dictionary<int, DiscoveryData> discoveriesList = DB_Main.getDiscoveriesList().ToDictionary(d => d.discoveryId);

      int mapListCount = mapList.Count;
      int counter = 0;

      foreach (string map in mapList) {
         string msg = $"{TOOL_NAME}: Uploading the world map spots for map: '{map}'...";
         
         Debug.Log(msg);
         uploadWorldMapSpots(map, discoveriesList);
         
         counter++;

         if (!silent && EditorUtility.DisplayCancelableProgressBar($"{TOOL_NAME}", msg, (float) counter / mapListCount)) {
            break;
         }
      }
   }

   public static void uploadWorldMapSpots (string areaKey, Dictionary<int, DiscoveryData> discoveriesList) {
      if (string.IsNullOrWhiteSpace(areaKey)) {
         return;
      }

      MapInfo mapInfo = JsonUtility.FromJson<MapInfo>(DB_Main.getMapInfo(areaKey));

      // Deserialize the map
      ExportedProject001 downloadedMap = MapImporter.deserializeMapData(mapInfo, areaKey);

      if (downloadedMap == null) {
         return;
      }

      List<WorldMapSpot> spots = new List<WorldMapSpot>();

      foreach (ExportedPrefab001 prefab in downloadedMap.prefabs) {
         if (prefab == null) {
            continue;
         }

         WorldMapSpot spot = null;
         if (prefab.i == 900 || prefab.i == 126 || prefab.i == 128) {
            spot = new WorldMapSpot();
            spot.areaWidth = downloadedMap.size.x;
            spot.areaHeight = downloadedMap.size.y;
            // The prefab position is relative to the center of the area,
            // but for convenience we store the position of the pin relative
            // to the top left corner instead
            spot.areaX = prefab.x + downloadedMap.size.x / 2;
            spot.areaY = prefab.y - downloadedMap.size.y / 2;
            // World Map position
            if (WorldMapManager.isWorldMapArea(areaKey)) {
               WorldMapAreaCoords areaCoords = WorldMapManager.getAreaCoords(areaKey);
               spot.worldX = areaCoords.x;
               spot.worldY = areaCoords.y;
            }
         }

         if (prefab.i == 900) {
            // Warp             
            spot.type = WorldMapSpot.SpotType.Warp;

            if (prefab.d != null) {
               DataField dataField = prefab.d.FirstOrDefault(d => d.k == DataField.TARGET_MAP_INFO_KEY);

               if (dataField != null) {
                  Map m = dataField.objectValue<Map>();
                  spot.target = m.name;
                  spot.displayName = m.displayName;
                  spot.specialType = (int) m.specialType;
               }

               DataField dfSpawnTarget = prefab.d.FirstOrDefault(d => d.k == DataField.WARP_TARGET_SPAWN_KEY);

               if (dfSpawnTarget != null) {
                  spot.spawnTarget = dfSpawnTarget.v.Trim(' ');
               }

               // If the spot leads to a maze, adjust the spot's information
               if (spot.target.ToLower().StartsWith("xx_maze")) {
                  try {
                     string suffix = WorldMapManager.getAreaSuffix(new WorldMapAreaCoords(spot.worldX, spot.worldY));
                     spot.target = $"{suffix}_maze";
                     spot.displayName = JsonUtility.FromJson<MapInfo>(DB_Main.getMapInfo(spot.target)).displayName;
                  } catch (Exception ex) {
                     Debug.LogError($"{ TOOL_NAME}: Exception encountered. Message: {ex.Message}");
                  }
               }
            }
         
         } else if (prefab.i == 126) {
            // League entrance
            spot.type = WorldMapSpot.SpotType.League;
         
         } else if (prefab.i == 128) {
            // Discovery
            spot.type = WorldMapSpot.SpotType.Discovery;
            
            if (prefab.d != null) {
               DataField dfDiscoveryTypeId = prefab.d.FirstOrDefault(d => d.k == DataField.DISCOVERY_TYPE_ID);
               if (dfDiscoveryTypeId != null) {
                  // Found the discovery information
                  if (int.TryParse(dfDiscoveryTypeId.v, out int discoveryTypeId)) {
                     if (discoveriesList.TryGetValue(discoveryTypeId, out DiscoveryData discoveryData)) {
                        spot.displayName = discoveryData.name;
                     }
                  }
               }
            }
         }

         if (spot != null) {
            spots.Add(spot);
         }
      }

      if (spots != null && spots.Count > 0) {
         DB_Main.uploadWorldMapSpots(spots);
         Debug.Log($"{TOOL_NAME}: Spots for World Map Area '{areaKey}' successfully uploaded.");
      }
   }

   #endregion

   #region Private Variables

   #endregion
}
