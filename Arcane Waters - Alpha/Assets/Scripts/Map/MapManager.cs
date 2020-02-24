using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool;
using MapCreationTool.Serialization;
using UnityEngine.Networking;

public class MapManager : MonoBehaviour {
   #region Public Variables

   // Convenient self reference
   public static MapManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   public void createLiveMaps (NetEntity source) {
      // Retrieve the map data from the database
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         Dictionary<string, MapInfo> maps = DB_Main.getLiveMaps();

         // Back to the Unity thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (string mapName in maps.Keys) {
               // Make sure it doesn't already exist
               if (AreaManager.self.getArea(mapName) != null && source != null) {
                  ServerMessageManager.sendError(ErrorMessage.Type.Misc, source, "Map: " + mapName + " has already been created!");
                  continue;
               }

               // Create the map here on the server
               Vector3 nextMapPosition = MapManager.self.getNextMapPosition();
               createLiveMap(mapName, maps[mapName], nextMapPosition);

               // Send confirmation back to the player who issued the command
               if (source != null) {
                  ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, source, "Spawning map: " + mapName);
               }
            }
         });
      });
   }

   public void createLiveMap (string areaKey, MapInfo mapInfo, Vector3 mapPosition) {
      D.log($"Preparing to create live map {areaKey} at {mapPosition} with version {mapInfo.version}");

      // If the area already exists, don't create it again
      if (AreaManager.self.hasArea(areaKey)) {
         D.warning($"Area {areaKey} already exists!");
         return;
      }

      // Instantiate the map using the map data
      Area area = MapImporter.instantiateMapData(mapInfo, areaKey, mapPosition);
      AreaManager.self.storeArea(area);
      area.vcam.VirtualCameraGameObject.SetActive(false);

      // Send signal to player to update virtual camera after area is created
      if (Global.player != null) {
         Global.player.updatePlayerCamera();
      }
   }

   public Vector3 getNextMapPosition() {
      // Every time the server creates a new map, we move the map offset
      _mapOffset.x += 200f;

      return _mapOffset;
   }

   public IEnumerator CO_DownloadAndCreateMap (string areaKey, int version, Vector3 mapPosition) {
      // Request the map from the web
      UnityWebRequest www = UnityWebRequest.Get("http://arcanewaters.com/maps.php?mapName=" + areaKey);
      yield return www.SendWebRequest();

      if (www.isNetworkError || www.isHttpError) {
         D.warning(www.error);
      } else {
         // Grab the map data from the request
         string mapData = www.downloadHandler.text;

         // Store it for later reference
         MapCache.storeMapData(areaKey, version, mapData);

         // Spawn the Area using the map data
         createLiveMap(areaKey, new MapInfo(areaKey, mapData, version), mapPosition);
      }
   }

   #region Private Variables

   // The current map offset being used by the server
   protected Vector3 _mapOffset = new Vector3(500f, 500f);

   #endregion
}
