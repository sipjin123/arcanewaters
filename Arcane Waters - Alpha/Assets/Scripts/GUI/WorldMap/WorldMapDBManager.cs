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
      // Fetches the pins previously uploaded to the database, and stores them locally
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         WorldMapManager.setCachedWorldMapPins(DB_Main.fetchWorldMapPins().ToList());
      });
   }

   public static void BKG_UploadWorldMapPinsInAllAreas () {
      if (!NetworkServer.active) {
         return;
      }

      List<string> maps = WorldMapManager.getOpenWorldAreasList();

      foreach (string mapKey in maps) {
         D.debug($"Uploading the world map pins for map: '{mapKey}'...");
         BKG_UploadWorldMapPinsInArea(mapKey);
      }
   }

   public static void BKG_UploadWorldMapPinsInArea (string areaKey) {
      if (!NetworkServer.active) {
         return;
      }

      MapInfo mapInfo = JsonUtility.FromJson<MapInfo>(DB_Main.getMapInfo(areaKey));

      UnityThreadHelper.UnityDispatcher.Dispatch(() => {
         // Deserialize the map
         ExportedProject001 exportedProject = MapImporter.deserializeMapData(mapInfo, areaKey);
         List<Warp> foundWarps = new List<Warp>();
         List<WorldMapPanelPinInfo> pins = new List<WorldMapPanelPinInfo>();

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

               WorldMapPanelPinInfo pin = new WorldMapPanelPinInfo {
                  areaWidth = exportedProject.size.x,
                  areaHeight = exportedProject.size.y,

                  // The prefab position is relative to the center of the area,
                  // but for convenience we store the position of the pin relative
                  // to the top left corner instead
                  x = prefab.x + exportedProject.size.x / 2,
                  y = prefab.y - exportedProject.size.y / 2
               };

               if (VoyageManager.isOpenWorld(areaKey)) {
                  Vector2Int areaCoords = WorldMapManager.computeOpenWorldAreaCoords(areaKey);
                  pin.areaX = areaCoords.x;
                  pin.areaY = areaCoords.y;
               }

               if (warpGO.TryGetComponent(out Warp warp)) {
                  pin.pinType = WorldMapPanelPin.PinTypes.Warp;
                  pin.spawnTarget = warp.spawnTarget;
                  pin.target = warp.areaTarget;
                  pin.displayName = Area.getName(warp.areaTarget);
                  pin.specialType = (int) warp.targetInfo.specialType;
               }

               pins.Add(pin);
               Destroy(warpGO);
            }

            // League Entrances
            if (original.TryGetComponent(out GenericActionTrigger gat)) {
               WorldMapPanelPinInfo pin = new WorldMapPanelPinInfo {
                  areaWidth = exportedProject.size.x,
                  areaHeight = exportedProject.size.y,
                  x = prefab.x + exportedProject.size.x / 2,
                  y = prefab.y - exportedProject.size.y / 2
               };

               if (VoyageManager.isOpenWorld(areaKey)) {
                  Vector2Int areaCoords = WorldMapManager.computeOpenWorldAreaCoords(areaKey);
                  pin.areaX = areaCoords.x;
                  pin.areaY = areaCoords.y;
               }

               pin.pinType = WorldMapPanelPin.PinTypes.League;
               pins.Add(pin);
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

               WorldMapPanelPinInfo pin = new WorldMapPanelPinInfo {
                  areaWidth = exportedProject.size.x,
                  areaHeight = exportedProject.size.y,
                  x = prefab.x + exportedProject.size.x / 2,
                  y = prefab.y - exportedProject.size.y / 2
               };

               if (VoyageManager.isOpenWorld(areaKey)) {
                  Vector2Int areaCoords = WorldMapManager.computeOpenWorldAreaCoords(areaKey);
                  pin.areaX = areaCoords.x;
                  pin.areaY = areaCoords.y;
               }

               pin.pinType = WorldMapPanelPin.PinTypes.Discovery;
               pin.discoveryId = discoverySpot.targetDiscoveryID;
               pins.Add(pin);
               Destroy(discoverySpotGO);
            }
         }

         D.debug($"In '{areaKey}': {foundWarps.Count} warps found.");

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            if (pins != null && pins.Count > 0) {
               DB_Main.uploadWorldMapPins(pins);
               D.debug($"Uploading the world map pins for map: '{areaKey}': DONE");
            }
         });
      });
   }

   #region Private Variables

   #endregion
}
