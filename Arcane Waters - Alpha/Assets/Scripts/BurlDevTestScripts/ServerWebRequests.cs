using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Networking;
using System;
using System.Xml.Serialization;
using System.Text;
using System.Xml;
using Random = UnityEngine.Random;

public class ServerWebRequests : MonoBehaviour
{
   #region Public Variables

   // Our server port
   public int ourPort;

   // Server data of all servers
   public List<ServerSqlData> serverDataList = new List<ServerSqlData>();

   // Cached info of our local server data
   public ServerSqlData ourServerData = new ServerSqlData();

   // The device name of our server
   public string myDeviceName;

   // Editor Flags
   public bool forceLocalDB;
   public bool manualSync;

   #endregion

   private void Awake () {
      if (forceLocalDB) {
         DB_Main.setServer("127.0.0.1");
      }

      if (!manualSync) {
         InvokeRepeating("checkServerUpdates", 2, .1f);
      }
   }

   private void OnGUI () {
      GUILayout.BeginHorizontal();
      {
         GUILayout.BeginVertical();
         {
            if (GUILayout.Button("Manual Check Updates")) {
               checkServerUpdates();
            }
            if (GUILayout.Button("Get server Contents for all")) {
               getServerContent(serverDataList);
            }
            if (GUILayout.Button("Generate a new random server")) {
               createRandomServer();
            }
            if (GUILayout.Button("Update local server")) {
               D.editorLog("Updating current server data", Color.green);
               updateSpecificServer(ourServerData, true);
            }
            if (GUILayout.Button("Update a Random Server")) {
               updateAnyRandomServer();
            }
            if (GUILayout.Button("Clear Server Data")) {
               serverDataList.Clear();
            }
         }
         GUILayout.EndVertical();
         GUILayout.BeginVertical();
         {
            foreach (ServerSqlData server in serverDataList) {
               if (GUILayout.Button("Update Server: " + server.deviceName)) {
                  updateSpecificServer(server, true);
               }
            }
         }
         GUILayout.EndVertical();
      }
      GUILayout.EndHorizontal();
   }

   private void Update () {
      if (Input.GetKeyDown(KeyCode.Alpha1)) {
         checkServerUpdates();
      }

      if (Input.GetKeyDown(KeyCode.Alpha2)) {
         getServerContent(serverDataList);
      }

      if (Input.GetKeyDown(KeyCode.Alpha3)) {
         createRandomServer();
      }

      if (Input.GetKeyDown(KeyCode.Alpha4)) {
         D.editorLog("Updating current server data", Color.green);
         updateSpecificServer(ourServerData, true);
      }
      if (Input.GetKeyDown(KeyCode.Alpha5)) {
         updateAnyRandomServer();
      }
      if (Input.GetKeyDown(KeyCode.Escape)) {
         serverDataList.Clear();
      }
      if (Input.GetKeyDown(KeyCode.O)) {
         DB_Main.setServer("127.0.0.1");
      }
   }

   private void updateAnyRandomServer () {
      List<ServerSqlData> newestList = new List<ServerSqlData>(serverDataList);
      ServerSqlData ourServerContent = newestList.Find(_ => _.deviceName == ourServerData.deviceName);
      newestList.Remove(ourServerContent);

      ServerSqlData randomServer = newestList[Random.Range(0, newestList.Count)];
      updateSpecificServer(randomServer, true);
   }

   private void createRandomServer () {
      D.editorLog("Creating a random server", Color.green);
      ServerSqlData sqlData = createRandomData();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.setServerContent(sqlData);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            D.editorLog("Server was set: " + sqlData.deviceName, Color.blue);
         });
      });
   }

   private void updateSpecificServer (ServerSqlData serverData, bool generateRandomData) {
      if (generateRandomData) {
         serverData.voyageList = generateRandomVoyage();
         serverData.connectedUserIds = generateRandomUserIds();
         serverData.openAreas = generateRandomArea().ToArray();
      } else {
         serverData.voyageList = new List<Voyage>();
         serverData.connectedUserIds = new List<int>();
         serverData.openAreas = new string[0];
      }
      serverData.latestUpdate = DateTime.UtcNow;
      D.editorLog("Updating any random server data: " + serverData.deviceName + " - " + serverData.port, Color.green);

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.setServerContent(serverData);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            D.editorLog("Server was updated: " + serverData.deviceName + " -- " + System.DateTime.UtcNow.ToBinary(), Color.blue);
         });
      });
   }

   private void checkServerUpdates () {
      myDeviceName = SystemInfo.deviceName;
      List<ServerSqlData> serversToUpdate = new List<ServerSqlData>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Fetch all server update info
         List<ServerSqlData> serverListUpdateTime = DB_Main.getServerUpdateTime();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (ServerSqlData newSqlData in serverListUpdateTime) {
               if (newSqlData.deviceName == myDeviceName) {
                  ServerSqlData existingSqlData = serverDataList.Find(_ => _.deviceName == newSqlData.deviceName && _.port == newSqlData.port);
                  if (existingSqlData == null) {
                     // If server is ours and just initialized, clear data
                     updateSpecificServer(newSqlData, false);
                     serverDataList.Add(newSqlData);
                     ourServerData = newSqlData;

                     D.editorLog("Server is initializing local", Color.green);
                  } else {
                     // Update our data to make sure we are in sync with the database
                     if (newSqlData.latestUpdate > existingSqlData.latestUpdate) {
                        D.editorLog("Server is fetching modified local server data", Color.green);
                        serversToUpdate.Add(newSqlData);
                     }
                  }
               } else {
                  string currentKey = newSqlData.port + "_" + newSqlData.deviceName;

                  ServerSqlData existingSqlData = serverDataList.Find(_ => _.deviceName == newSqlData.deviceName && _.port == newSqlData.port);
                  if (existingSqlData != null) {
                     // Update the server data if it was modified
                     if (existingSqlData.latestUpdate < newSqlData.latestUpdate) {
                        D.editorLog("The server: " + newSqlData.deviceName + " has updated its data from: " + existingSqlData.latestUpdate + " to " + newSqlData.latestUpdate, Color.green);
                        serversToUpdate.Add(existingSqlData);
                     } 
                  } else {
                     // Add server data if non existent to server data list
                     D.editorLog("This server is New, Caching now: " + newSqlData.port + "_" + newSqlData.deviceName + " === " + DateTime.Parse(PlayerPrefs.GetString(currentKey, DateTime.UtcNow.ToString())), Color.green);
                     serverDataList.Add(newSqlData);
                     serversToUpdate.Add(newSqlData);
                  }
               }
            }
            getServerContent(serversToUpdate);
         });
      });
   }

   private void getServerContent (List<ServerSqlData> serverList) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Fetch the content of the recently updated servers
         List<ServerSqlData> serverListContent = DB_Main.getServerContent(serverList);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (ServerSqlData newSqlData in serverListContent) {
               ServerSqlData existingData = serverDataList.Find(_ => _.deviceName == newSqlData.deviceName && _.port == newSqlData.port);
               if (existingData != null) {
                  // Assign the content of the recently updated servers
                  existingData.voyageList = newSqlData.voyageList;
                  existingData.connectedUserIds = newSqlData.connectedUserIds;
                  existingData.openAreas = newSqlData.openAreas;
                  existingData.latestUpdate = newSqlData.latestUpdate;

                  // Keep our local cache to self updated
                  if (existingData.deviceName == myDeviceName && existingData.port == ourPort) {
                     ourServerData = existingData;
                  }
               } else {
                  // Fail safe code, assign the missing data incase it is missing
                  if (newSqlData.deviceName == myDeviceName && newSqlData.port == ourPort) {
                     ourServerData = newSqlData;
                  }
                  serverDataList.Add(newSqlData);
               }
            }
         });
      });
   }

   private ServerSqlData createRandomData () {
      ServerSqlData newData = new ServerSqlData();

      newData.deviceName = "device_";
      for (int i = 0; i < 5; i++) {
         int randomIndex = Random.Range(1, _alphabets.Length);
         newData.deviceName += _alphabets[randomIndex].ToString();
      }
      newData.ip = "127.0.0.1";
      newData.port = 7777;
      newData.latestUpdate = DateTime.UtcNow;

      newData.voyageList = generateRandomVoyage();
      newData.openAreas = generateRandomArea().ToArray();
      newData.connectedUserIds = generateRandomUserIds();

      return newData;
   }

   private List<Voyage> generateRandomVoyage () {
      List<Voyage> voyageList = new List<Voyage>();
      int randomCount = Random.Range(1, 5);

      for (int i = 0; i < randomCount; i++) {
         string voyageName = "voyage_" + Random.Range(1, 1000);
         int biomeCount = Enum.GetValues(typeof(Biome.Type)).Length;

         Voyage newVoyage = new Voyage {
            areaKey = voyageName,
            biome = (Biome.Type) Random.Range(1, biomeCount - 1),
            voyageId = Random.Range(1, 1000)
         };
         voyageList.Add(newVoyage);
      }

      return voyageList;
   }

   private List<string> generateRandomArea () {
      int randomCount = Random.Range(1, 5);
      List<string> areaList = new List<string>();
      
      for (int i = 0; i < randomCount; i++) {
         areaList.Add("area_" + Random.Range(1, 1000));
      }

      return areaList;
   }

   private List<int> generateRandomUserIds () {
      int randomCount = Random.Range(2, 5);
      List<int> userIdList = new List<int>();

      for (int i = 0; i < randomCount; i++) {
         userIdList.Add(Random.Range(1, 1000));
      }

      return userIdList;
   }

   #region Private Variables

   private string _alphabets = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

   #endregion
}