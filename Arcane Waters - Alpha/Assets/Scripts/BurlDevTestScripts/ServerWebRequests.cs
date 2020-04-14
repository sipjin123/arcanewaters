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
using System.Linq;

public class ServerWebRequests : MonoBehaviour
{
   #region Public Variables

   // Self
   public static ServerWebRequests self;

   // The device name of our server
   public string myDeviceName;

   // Our server ip
   public string ourIp;

   // Our server port
   public int ourPort;

   // The prefab to spawn
   public Server serverPrefab;

   // Cached info of our local server data
   public ServerSqlData ourServerData = new ServerSqlData();

   // The chat list existing in the database
   public List<ChatInfo> chatInfoList = new List<ChatInfo>();

   // Server data of all servers
   public List<ServerSqlData> serverDataList = new List<ServerSqlData>();

   // Editor Flags
   public bool forceLocalDB;
   public bool manualSync;
   public bool autoStart;
   public bool overrideDeviceName;

   Vector2 scrollPosition;
   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      if (autoStart) {
         initializeServers();
      }
   }

   public void initializeServers () {
      initializeLocalServer();
      if (forceLocalDB) {
         DB_Main.setServer("127.0.0.1");
      }

      if (!manualSync) {
         InvokeRepeating("checkServerUpdates", 2, .1f);
      }
   }

   private void addNewServer (ServerSqlData serverData, bool isLocal) {
      serverDataList.Add(serverData);
      Server server = Instantiate(serverPrefab, Vector3.zero, Quaternion.identity); //PhotonNetwork.Instantiate("Server", Vector3.zero, Quaternion.identity, 0);
      server.gameObject.name = "Server_" + serverData.deviceName;

      server.deviceName = serverData.deviceName;
      server.ipAddress = serverData.ip;
      server.port = serverData.port;

      server.voyages = serverData.voyageList;
      server.openAreas = serverData.openAreas.ToArray();
      if (serverData.connectedUserIds.Count < 1) {
         server.connectedUserIds = new HashSet<int>();
      } else {
         server.connectedUserIds = new HashSet<int>(serverData.connectedUserIds);
      }

      if (ServerNetwork.self!= null) {
         if (isLocal) {
            ServerNetwork.self.server = server;
            ServerNetwork.self.server.setAsLocalServer();
         } else {
            ServerNetwork.self.serverList.Add(server);
            ServerNetwork.self.servers.Add(server);
         }
      }
   }

   private void initializeLocalServer () {
      if (!overrideDeviceName) {
         myDeviceName = SystemInfo.deviceName;
      }

      ServerSqlData newSqlData = new ServerSqlData {
         ip = ourIp,
         port = ourPort,
         deviceName = myDeviceName
      };

      // If server is ours and just initialized, clear data
      updateSpecificServer(newSqlData, false);

      // Keep reference to self
      ourServerData = newSqlData;

      addNewServer(newSqlData, true);
   }

   private void OnGUI () {
      GUILayout.BeginHorizontal();
      {
         scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(600), GUILayout.Height(300));
         GUILayout.BeginHorizontal();
         {
            foreach (ServerSqlData server in serverDataList) {
               GUILayout.BeginVertical();
               if (GUILayout.Button("Update Server: " + server.deviceName)) {
                  updateSpecificServer(server, true);
               }
               GUILayout.Space(20);
               foreach (int ids in server.connectedUserIds) {
                  GUILayout.Box("User: " + ids);
               }
               GUILayout.Space(20);
               foreach (Voyage voyages in server.voyageList) {
                  GUILayout.Box("Voyages: " + voyages.voyageId);
               }
               GUILayout.EndVertical();
            }
         }
         GUILayout.EndHorizontal();
         GUILayout.EndScrollView();
      }
      GUILayout.EndHorizontal();

      GUILayout.BeginHorizontal();
      {
         GUILayout.BeginVertical();
         {
            if (GUILayout.Button("Update local server")) {
               D.editorLog("Updating current server data", Color.green);
               updateSpecificServer(ourServerData, true);
            }
            if (GUILayout.Button("Create a voyage")) {
               createLocalVoyage();
            }
            if (GUILayout.Button("Create a user")) {
               createLocalPlayers();
            }
            if (GUILayout.Button("Create an area")) {
               createOpenArea();
            }
            if (GUILayout.Button("Create an invite")) {
               if (ourServerData.connectedUserIds.Count > 0 && ourServerData.voyageList.Count > 0) {
                  createLocalInvite();
               } else {
                  D.editorLog("No players or voyage for invite", Color.red);
               }
            }
         }
         GUILayout.EndVertical();
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
            if (GUILayout.Button("Update a Random Server")) {
               updateAnyRandomServer();
            }
            if (GUILayout.Button("Clear Server Data")) {
               serverDataList.Clear();
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

   private void updateSpecificServer (ServerSqlData serverData, bool generateRandomData, bool retainData = false) {
      if (!retainData) {
         if (generateRandomData) {
            serverData.voyageList = generateRandomVoyage();
            serverData.connectedUserIds = generateRandomUserIds();
            serverData.openAreas = generateRandomArea();
            serverData.voyageInvites = generateVoyageInviteForServer(serverData);
         } else {
            serverData.voyageList = new List<Voyage>();
            serverData.connectedUserIds = new List<int>();
            serverData.openAreas = new List<string>();
            serverData.voyageInvites = new List<VoyageInviteData>();
         }
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
      List<ServerSqlData> serversToUpdate = new List<ServerSqlData>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Fetch all server update info
         List<ServerSqlData> serverListUpdateTime = DB_Main.getServerUpdateTime();
         chatInfoList = DB_Main.getChat(ChatInfo.Type.Global, 0, false, 20);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (ServerSqlData newSqlData in serverListUpdateTime) {
               if (newSqlData.deviceName == myDeviceName) {
                  ServerSqlData existingSqlData = serverDataList.Find(_ => _.deviceName == newSqlData.deviceName && _.port == newSqlData.port && _.ip == newSqlData.ip);
                  if (existingSqlData != null) {
                     // Update our data to make sure we are in sync with the database
                     TimeSpan updateSpan = (existingSqlData.latestUpdate - newSqlData.latestUpdate);
                     if (updateSpan.TotalSeconds > 1) {
                        D.editorLog("Server is not sync! Resent Server data: " + existingSqlData.latestUpdate + " - " + newSqlData.latestUpdate, Color.green);
                        updateSpecificServer(ourServerData, false, true);
                     }
                  } 
               } else {
                  string currentKey = newSqlData.port + "_" + newSqlData.deviceName;

                  ServerSqlData existingSqlData = serverDataList.Find(_ => _.deviceName == newSqlData.deviceName && _.port == newSqlData.port && _.ip == newSqlData.ip);
                  if (existingSqlData != null) {
                     // Update the server data if it was modified
                     if (newSqlData.latestUpdate > existingSqlData.latestUpdate) {
                        D.editorLog("The server: " + newSqlData.deviceName + " has updated its data from: " + existingSqlData.latestUpdate + " to " + newSqlData.latestUpdate, Color.green);
                        serversToUpdate.Add(existingSqlData);
                     } 
                  } else {
                     // Add server data if non existent to server data list
                     D.editorLog("This server is New, Caching now: " + newSqlData.port + "_" + newSqlData.deviceName + " === " + DateTime.Parse(PlayerPrefs.GetString(currentKey, DateTime.UtcNow.ToString())), Color.green);
                   
                     serversToUpdate.Add(newSqlData);
                     addNewServer(newSqlData, false);
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
               ServerSqlData existingData = serverDataList.Find(_ => _.deviceName == newSqlData.deviceName && _.port == newSqlData.port && _.ip == newSqlData.ip);
               if (existingData != null) {
                  // Assign the content of the recently updated servers
                  existingData.voyageList = newSqlData.voyageList;
                  existingData.connectedUserIds = newSqlData.connectedUserIds;
                  existingData.openAreas = newSqlData.openAreas;
                  existingData.voyageInvites = newSqlData.voyageInvites;
                  if (existingData.latestUpdate != newSqlData.latestUpdate) {
                     D.editorLog("Overwriting dates from: " + existingData.latestUpdate, Color.yellow);
                     D.editorLog("Overwriting dates to: " + newSqlData.latestUpdate, Color.yellow);
                  }
                  existingData.latestUpdate = newSqlData.latestUpdate;
                  overwriteServerData(newSqlData);

                  // Keep our local cache to self updated
                  if (existingData.deviceName == myDeviceName && existingData.port == ourPort && existingData.ip == ourIp) {
                     ourServerData = existingData;
                  }
               } else {
                  // Fail safe code, assign the missing data incase it is missing
                  if (newSqlData.deviceName == myDeviceName && newSqlData.port == ourPort && existingData.ip == ourIp) {
                     ourServerData = newSqlData;
                  }
                  serverDataList.Add(newSqlData);
               }
            }
         });
      });
   }

   private void overwriteServerData (ServerSqlData newSqlData) {
      if (ServerNetwork.self != null) {
         Server existingServer = ServerNetwork.self.servers.ToList().Find(_ => _.deviceName == newSqlData.deviceName && _.ipAddress == newSqlData.ip && _.port == newSqlData.port);
         if (existingServer != null) {
            existingServer.openAreas = newSqlData.openAreas.ToArray();
            existingServer.voyages = newSqlData.voyageList;
            existingServer.connectedUserIds = new HashSet<int>(newSqlData.connectedUserIds);
            D.editorLog("Successful Update! " + newSqlData.deviceName, Color.green);
         } else {
            StartCoroutine(CO_ResetServerData(newSqlData));
            D.editorLog("Missing server, it is not existing yet: " + newSqlData.deviceName + " - " + newSqlData.ip, Color.green);
         }
      }
   }

   private IEnumerator CO_ResetServerData (ServerSqlData newSqlData) {
      yield return new WaitForSeconds(.25f);
      overwriteServerData(newSqlData);
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
      newData.openAreas = generateRandomArea();
      newData.connectedUserIds = generateRandomUserIds();
      newData.voyageInvites = generateVoyageInviteForServer(newData);

      return newData;
   }

   private void createLocalVoyage () {
      string voyageName = "voyage_" + Random.Range(1, 1000);
      int biomeCount = Enum.GetValues(typeof(Biome.Type)).Length;

      Voyage newVoyage = new Voyage {
         areaKey = voyageName,
         biome = (Biome.Type) Random.Range(1, biomeCount - 1),
         voyageId = Random.Range(1, 1000)
      };

      ourServerData.voyageList.Add(newVoyage);
      if (ServerNetwork.self != null) {
         ServerNetwork.self.server.voyages = ourServerData.voyageList;
      }
      updateSpecificServer(ourServerData, false, true);
   }

   private void createLocalPlayers () {
      int newUserId = Random.Range(1, 1000);
      ourServerData.connectedUserIds.Add(newUserId);
      if (ServerNetwork.self != null) {
         ServerNetwork.self.server.connectedUserIds.Add(newUserId);
      }
      updateSpecificServer(ourServerData, false, true);
   }

   private void createOpenArea () {
      string newArea = "area_" + Random.Range(1, 1000);
      ourServerData.openAreas.Add(newArea);
      if (ServerNetwork.self != null) {
         ServerNetwork.self.server.openAreas = ourServerData.openAreas.ToArray();
      }
      updateSpecificServer(ourServerData, false, true);
   }

   private void createLocalInvite () {
      ourServerData.voyageInvites = generateVoyageInviteForServer(ourServerData);
      updateSpecificServer(ourServerData, false, true);
   }

   private List<VoyageInviteData> generateVoyageInviteForServer (ServerSqlData serverSelected) {
      List<VoyageInviteData> newInviteList = new List<VoyageInviteData>();
      int inviteeId = serverSelected.connectedUserIds[0];
      string inviterName = "Player_";
      for (int i = 0; i < 5; i++) {
         inviterName += _alphabets[Random.Range(1, _alphabets.Length)];
      }
      int groupId = serverSelected.voyageList[0].voyageId;

      newInviteList.Add(new VoyageInviteData { 
         inviteeId = inviteeId,
         inviterName = inviterName,
         voyageGroupId = groupId
      });
      return newInviteList;
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