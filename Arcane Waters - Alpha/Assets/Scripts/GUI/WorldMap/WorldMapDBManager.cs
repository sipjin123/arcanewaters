using UnityEngine;
using System.Collections.Generic;
using MapCreationTool.Serialization;
using MapCreationTool;
using System.Linq;
using Mirror;

public class WorldMapDBManager : MonoBehaviour
{
   #region Public Variables

   // Self
   public static WorldMapDBManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   public void initialize () {
      // Fetches the spots previously uploaded to the database, and stores them locally
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         setWorldMapSpots(DB_Main.fetchWorldMapSpots().ToList());
      });
   }

   public static void BKG_UploadWorldMapSpotsInAllAreas () {
      if (!NetworkServer.active) {
         return;
      }

      List<string> maps = WorldMapManager.self.getAllAreasList();

      foreach (string mapKey in maps) {
         D.debug($"Uploading the world map spots for area: '{mapKey}'...");
         BKG_UploadWorldMapSpotsInArea(mapKey);
      }
   }

   public static void BKG_UploadWorldMapSpotsInArea (string areaKey) {
      if (!NetworkServer.active) {
         return;
      }

      MapInfo mapInfo = JsonUtility.FromJson<MapInfo>(DB_Main.getMapInfo(areaKey));

      UnityThreadHelper.UnityDispatcher.Dispatch(() => {
         // Deserialize the map
         ExportedProject001 exportedProject = MapImporter.deserializeMapData(mapInfo, areaKey);
         List<Warp> foundWarps = new List<Warp>();
         List<WorldMapSpot> spots = new List<WorldMapSpot>();

         if (exportedProject == null) {
            return;
         }

         foreach (ExportedPrefab001 prefab in exportedProject.prefabs) {
            GameObject original = AssetSerializationMaps.tryGetPrefabGame(prefab.i, exportedProject.biome);

            if (original == null) {
               continue;
            }

            Vector3 targetLocalPos = new Vector3(prefab.x, prefab.y, 0) * 0.16f + Vector3.back * 10;

            // Town Entrances
            if (original.TryGetComponent(out Warp originalWarp)) {
               GameObject warpGO = Instantiate(original, targetLocalPos, Quaternion.identity);
               foundWarps.Add(originalWarp);

               if (original.GetComponent<SecretEntranceHolder>() != null) {
                  SecretEntranceHolder secretEntranceObj = warpGO.GetComponent<SecretEntranceHolder>();
                  if (prefab.d != null) {
                     // Make sure obj has correct data
                     IMapEditorDataReceiver receiver = warpGO.GetComponent<IMapEditorDataReceiver>();
                     if (receiver != null && prefab.d != null) {
                        receiver.receiveData(prefab.d);
                     }
                  }
               }

               if (prefab.d != null) {
                  foreach (IMapEditorDataReceiver receiver in warpGO.GetComponents<IMapEditorDataReceiver>()) {
                     receiver.receiveData(prefab.d);
                  }
               }

               WorldMapSpot spot = new WorldMapSpot {
                  areaWidth = exportedProject.size.x,
                  areaHeight = exportedProject.size.y,

                  // The prefab position is relative to the center of the area,
                  // but for convenience we store the position of the pin relative
                  // to the top left corner instead
                  areaX = prefab.x + exportedProject.size.x / 2,
                  areaY = prefab.y - exportedProject.size.y / 2
               };

               if (WorldMapManager.self.isWorldMapArea(areaKey)) {
                  WorldMapAreaCoords areaCoords = WorldMapManager.self.getAreaCoords(areaKey);
                  spot.worldX = areaCoords.x;
                  spot.worldY = areaCoords.y;
               }

               if (warpGO.TryGetComponent(out Warp warp)) {
                  spot.type = WorldMapSpot.SpotType.Warp;
                  spot.spawnTarget = warp.spawnTarget;
                  spot.target = warp.areaTarget;
                  spot.displayName = Area.getName(warp.areaTarget);
                  spot.specialType = (int) warp.targetInfo.specialType;
               }

               spots.Add(spot);
               Destroy(warpGO);
            }

            // League Entrances
            if (original.TryGetComponent(out GenericActionTrigger gat)) {
               WorldMapSpot spot = new WorldMapSpot {
                  areaWidth = exportedProject.size.x,
                  areaHeight = exportedProject.size.y,
                  areaX = prefab.x + exportedProject.size.x / 2,
                  areaY = prefab.y - exportedProject.size.y / 2
               };

               if (WorldMapManager.self.isWorldMapArea(areaKey)) {
                  WorldMapAreaCoords areaCoords = WorldMapManager.self.getAreaCoords(areaKey);
                  spot.worldX = areaCoords.x;
                  spot.worldY = areaCoords.y;
               }

               spot.type = WorldMapSpot.SpotType.League;
               spots.Add(spot);
            }

            // Discoveries
            if (original.TryGetComponent(out DiscoverySpot originalDiscoverySpot)) {
               GameObject discoverySpotGO = Instantiate(original, targetLocalPos, Quaternion.identity);
               DiscoverySpot discoverySpot = discoverySpotGO.GetComponent<DiscoverySpot>();

               if (prefab.d != null) {
                  foreach (IMapEditorDataReceiver receiver in discoverySpotGO.GetComponents<IMapEditorDataReceiver>()) {
                     receiver.receiveData(prefab.d);
                  }
               }

               WorldMapSpot spot = new WorldMapSpot {
                  areaWidth = exportedProject.size.x,
                  areaHeight = exportedProject.size.y,
                  areaX = prefab.x + exportedProject.size.x / 2,
                  areaY = prefab.y - exportedProject.size.y / 2
               };

               if (WorldMapManager.self.isWorldMapArea(areaKey)) {
                  WorldMapAreaCoords areaCoords = WorldMapManager.self.getAreaCoords(areaKey);
                  spot.worldX = areaCoords.x;
                  spot.worldY = areaCoords.y;
               }

               spot.type = WorldMapSpot.SpotType.Discovery;
               spot.discoveryId = discoverySpot.targetDiscoveryID;
               spots.Add(spot);
               Destroy(discoverySpotGO);
            }
         }

         D.debug($"In '{areaKey}': {foundWarps.Count} warps found.");

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            if (spots != null && spots.Count > 0) {
               DB_Main.uploadWorldMapSpots(spots);
               D.debug($"Uploading the world map spots for area: '{areaKey}': DONE");
            }
         });
      });
   }

   public void setWorldMapSpots (List<WorldMapSpot> spots) {
      _worldMapSpots = spots;
   }

   public List<WorldMapSpot> getWorldMapSpots () {
      return _worldMapSpots;
   }

   #region Private Variables

   // Server cache for the world spots
   private List<WorldMapSpot> _worldMapSpots = new List<WorldMapSpot>();

   #endregion
}
