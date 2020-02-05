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
         Dictionary<string, string> maps = DB_Main.getLiveMaps();

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

   public void createLiveMap (string areaKey, string mapData, Vector3 mapPosition) {
      D.log("Preparing to create live map: " + areaKey + " at: " + mapPosition);

      // If the map already exists, don't create it again
      if (_maps.ContainsKey(areaKey)) {
         D.warning("Map: " + areaKey + " already exists!");
         return;
      }

      // Instantiate the map using the map data
      Area area = MapImporter.instantiateMapData(mapData, areaKey, mapPosition);
      AreaManager.self.storeArea(area);
      area.vcam.VirtualCameraGameObject.SetActive(false);

      // Keep track of the maps we create
      _maps[area.areaKey] = mapData;
   }

   public Vector3 getNextMapPosition() {
      // Every time the server creates a new map, we move the map offset
      _mapOffset.x += 200f;

      return _mapOffset;
   }

   public string getMapData (string areaKey) {
      if (_maps.ContainsKey(areaKey)) {
         return _maps[areaKey];
      }

      return null;
   }

   public IEnumerator CO_DownloadMap (string areaKey, Vector3 position) {
      // Check if we already have the map
      if (_maps.ContainsKey(areaKey)) {
         D.log("Map Manager already has map for area: " + areaKey);
         yield break;
      }

      // Request the map from the web
      UnityWebRequest www = UnityWebRequest.Get("http://arcanewaters.com/maps.php?mapName=" + areaKey);
      yield return www.SendWebRequest();

      if (www.isNetworkError || www.isHttpError) {
         D.warning(www.error);
      } else {
         // Grab the map data from the request
         string mapData = www.downloadHandler.text;

         // Spawn the Area using the map data
         createLiveMap(areaKey, mapData, position);
      }
   }

   #region Private Variables

   // The current map offset being used by the server
   protected Vector3 _mapOffset = new Vector3(500f, 500f);

   // Keeps track of the map data we've received, indexed by their Area Key string
   protected Dictionary<string, string> _maps = new Dictionary<string, string>();

   #endregion
}
