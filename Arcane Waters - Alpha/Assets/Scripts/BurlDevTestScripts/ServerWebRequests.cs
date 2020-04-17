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
   public string ourDeviceName;

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

   // List of voyages to be created
   public List<PendingVoyageCreation> pendingVoyageCreations = new List<PendingVoyageCreation>();

   // List of voyage invitations
   public List<VoyageInviteData> pendingVoyageInvites = new List<VoyageInviteData>();

   // The latest chat info
   public ChatInfo latestChatinfo = new ChatInfo();

   // Editor Flags
   public bool forceLocalDB;
   public bool manualSync;
   public bool autoStart;
   public bool overrideDeviceName;
   public bool toggleGUI;

   // Determines if the voyages have been initialized
   public bool hasInitiatedVoyage;

   // The servers that needs updating
   public List<ServerSqlData> serversToUpdate = new List<ServerSqlData>();

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

   private void pingLocalServer () {
      List<ServerSqlData> fetchedServerData = new List<ServerSqlData>();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Frequently send to the database to confirm that this server is active
         ourServerData.lastPingTime = DateTime.UtcNow;
         DB_Main.sendServerPing(ourServerData);

         // Fetch the ping of the other servers
         foreach (ServerSqlData serverData in serverDataList) {
            if (!isOurServer(serverData)) {
               ServerSqlData newServerData = DB_Main.getLatestServerPing(serverData);
               fetchedServerData.Add(newServerData);
            }
         }
         
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            foreach (ServerSqlData serverData in fetchedServerData) {
               // Check if the other servers are active recently
               TimeSpan pingInterval = DateTime.UtcNow - serverData.lastPingTime;
               if (pingInterval.TotalSeconds > 10) {
                  D.editorLog("Removing Server Entry due to inactivity: " + serverData.deviceName + " Time Delay: " + pingInterval.TotalSeconds, Color.red);

                  // Remove the server data entry from the list
                  ServerSqlData selectedServerData = serverDataList.Find(_ => _.deviceName == serverData.deviceName && _.port == serverData.port && _.ip == serverData.ip);
                  if (selectedServerData != null) {
                     serverDataList.Remove(selectedServerData);
                  }

                  // Remove the server entry from the list
                  Server selectedServer = ServerNetwork.self.servers.ToList().Find(_ => _.deviceName == serverData.deviceName && _.port == serverData.port && _.ipAddress == serverData.ip);
                  if (selectedServer != null) {
                     ServerNetwork.self.removeServer(selectedServer);
                  }
               }
            }
         });
      });
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

      // Setup the user ids
      if (serverData.connectedUserIds.Count < 1) {
         server.connectedUserIds = new HashSet<int>();
      } else {
         server.connectedUserIds = new HashSet<int>(serverData.connectedUserIds);
      }
      if (serverData.claimedUserIds.Count < 1) {
         server.claimedUserIds = new HashSet<int>();
      } else {
         server.claimedUserIds = new HashSet<int>(serverData.claimedUserIds);
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
         ourDeviceName = SystemInfo.deviceName;
      }

      ServerSqlData newSqlData = new ServerSqlData {
         ip = ourIp,
         port = ourPort,
         deviceName = ourDeviceName
      };

      // If server is ours and just initialized, clear data
      updateSpecificServer(newSqlData, false);

      // Keep reference to self
      ourServerData = newSqlData;

      InvokeRepeating("pingLocalServer", 1, 1);

      addNewServer(newSqlData, true);
   }

   #region Simulator Updates and UI

   private void OnGUI () {
      if (GUI.Button(new Rect(Screen.width - 100, 0, 100, 30), "OpenGUI")) {
         toggleGUI = !toggleGUI;
      }
      
      if (GUI.Button(new Rect(Screen.width - 400, 0, 200, 30), "Override Best Server: " + ServerNetwork.self.overrideBestServerConnection)) {
         ServerNetwork.self.overrideBestServerConnection = !ServerNetwork.self.overrideBestServerConnection;
      }
      if (ServerNetwork.self.overrideBestServerConnection) {
         string svName = "None";
         try {
            svName = ServerNetwork.self.servers.ToList()[ServerNetwork.self.overrideConnectServerIndex].deviceName;
         } catch {

         }
         if (GUI.Button(new Rect(Screen.width - 400, 30, 200, 30), ServerNetwork.self.overrideConnectServerIndex + ": Set sv to 0: " + svName)) {
            ServerNetwork.self.overrideConnectServerIndex = 0;
         }
         if (GUI.Button(new Rect(Screen.width - 400, 60, 200, 30), ServerNetwork.self.overrideConnectServerIndex + ": Set sv to 1: " + svName)) {
            ServerNetwork.self.overrideConnectServerIndex = 1;
         }
      }

      GUI.Box(new Rect(Screen.width - 200, 30, 200, 30), "Port is: " + ourPort);
      GUI.Box(new Rect(Screen.width - 200, 60, 200, 30), "Name is: " + ourDeviceName);
      if (GUI.Button(new Rect(Screen.width - 250, 90, 250, 30), "Override Name: " + ourDeviceName +" : "+overrideDeviceName)) {
         overrideDeviceName = !overrideDeviceName;
      }
      if (GUI.Button(new Rect(Screen.width - 200, 120, 200, 30), "Send Glboal Msg")) {
         ServerNetwork.self.server.SendGlobalChat(_globalMessage, 0);
      }
      _globalMessage = GUI.TextField(new Rect(Screen.width - 400, 120, 200, 60), _globalMessage);

      if (!toggleGUI) {
         return;
      }

      GUILayout.BeginHorizontal();
      {
         _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Width(800), GUILayout.Height(400));
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
            GUILayout.Space(10);

            GUILayout.BeginHorizontal();
            GUILayout.Label("InviterId: ");
            _inviterId = GUILayout.TextField(_inviterId);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("InviteeId: ");
            _inviteeId = GUILayout.TextField(_inviteeId);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Voyage: ");
            _voyageGroupId = GUILayout.TextField(_voyageGroupId);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Create an invite")) {
               processVoyageInvite();
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

         GUILayout.BeginVertical();
         {
            GUILayout.Box("Pending Invites are: ");
            GUILayout.Space(5);
            foreach (VoyageInviteData pendingVoyage in pendingVoyageInvites) {
               GUILayout.Box("Confirm pending voyage : " + pendingVoyage.inviterId + " -> " + pendingVoyage.inviteeId + " {} " + pendingVoyage.voyageGroupId); 
            }
         }
         GUILayout.EndVertical();
         GUILayout.BeginVertical();
         {
            GUILayout.Box("Pending Creations are: ");
            GUILayout.Space(5);
            foreach (PendingVoyageCreation pendingVoyage in pendingVoyageCreations) {
               if (GUILayout.Button("Confirm pending voyage : " + pendingVoyage.id + " - " + pendingVoyage.areaKey)) {
                  confirmVoyageCreation(pendingVoyage);
               }
            }
         }
         GUILayout.EndVertical();
      }
      GUILayout.EndHorizontal();
   }

   #endregion

   private void processVoyageInvite () {
      int inviterId = int.Parse(_inviterId);
      int inviteeId = int.Parse(_inviteeId);
      int voyageId = int.Parse(_voyageGroupId);
      string inviterName = "UnNamed";
      try {
         inviterName = BodyManager.self.getBody(inviterId).entityName;
      } catch {
      }

      foreach (Server server in ServerNetwork.self.servers) {
         bool containsUser = server.connectedUserIds.ToList().Contains(inviteeId);

         VoyageInviteData newVoyageInvite = new VoyageInviteData(server, inviterId, inviterName, inviteeId, voyageId, InviteStatus.Pending, DateTime.UtcNow);
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            DB_Main.createVoyageInvite(newVoyageInvite);
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               D.editorLog("Server Created the invite", Color.green);
            });
         });
         break;
      }
   }

   public bool isOurServer (ServerSqlData serverData) {
      return (serverData.deviceName == ourDeviceName && serverData.ip == ourIp && serverData.port == ourPort);
   }

   private void updateSpecificServer (ServerSqlData serverData, bool generateRandomData, bool retainData = false) {
      if (!retainData) {
         if (generateRandomData) {
            serverData.voyageList = generateRandomVoyage();
            serverData.connectedUserIds = generateRandomUserIds();
            serverData.openAreas = generateRandomArea();
         } else {
            serverData.voyageList = new List<Voyage>();
            serverData.connectedUserIds = new List<int>();
            serverData.openAreas = new List<string>();
         }
      }
      serverData.latestUpdate = DateTime.UtcNow;
      serverData.lastPingTime = serverData.latestUpdate;
      D.editorLog("Updating any random server data: " + serverData.deviceName + " - " + serverData.port, Color.green);

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.setServerContent(serverData);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            D.editorLog("Server was updated: " + serverData.deviceName + " -- " + serverData.port, Color.blue);
         });
      });
   }
   
   private void processChatList () {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         chatInfoList = DB_Main.getChat(ChatInfo.Type.Global, 0, false, 20);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            List<ChatInfo> chatList = chatInfoList.OrderByDescending(_ => _.chatTime).ToList();
            latestChatinfo = chatList[0];
         });
      });
   }

   private void processVoyageCreation (string areaKey, int id) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.removeServerVoyageCreation(id);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            D.editorLog("Server should create voyage instance: " + areaKey, Color.green);
            ServerNetwork.self.server.CreateVoyageInstance(areaKey);
         });
      });
   }

   private void checkServerUpdates () {
      serversToUpdate = new List<ServerSqlData>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Fetch all server update info
         List<ServerSqlData> serverListUpdateTime = DB_Main.getServerUpdateTime();
         pendingVoyageCreations = DB_Main.getPendingVoyageCreations();
         ChatInfo latestChatInfo = DB_Main.getLatestChatInfo();
         pendingVoyageInvites = DB_Main.getAllVoyageInvites();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (PendingVoyageCreation voyageCreations in pendingVoyageCreations) {
               if (ServerNetwork.self != null) {
                  if (voyageCreations.serverName == ourDeviceName && voyageCreations.serverPort == ourPort && voyageCreations.serverIp == ourIp) {
                     processVoyageCreation(voyageCreations.areaKey, voyageCreations.id);
                  }
               }
            }

            if (latestChatinfo.chatId != 0) {
               List<ChatInfo> chatList = chatInfoList.OrderByDescending(_ => _.chatTime).ToList();
               if (latestChatInfo.chatTime > chatList[0].chatTime) {
                  processChatList();
               }
            } else {
               processChatList();
            }

            foreach (ServerSqlData newSqlData in serverListUpdateTime) {
               if (newSqlData.deviceName == ourDeviceName) {
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
                  ServerSqlData existingSqlData = serverDataList.Find(_ => _.deviceName == newSqlData.deviceName && _.port == newSqlData.port && _.ip == newSqlData.ip);
                  if (existingSqlData != null) {
                     // Update the server data if it was modified
                     if (newSqlData.latestUpdate > existingSqlData.latestUpdate) {
                        D.editorLog("The server: " + newSqlData.deviceName + " has updated its data from: " + existingSqlData.latestUpdate + " to " + newSqlData.latestUpdate, Color.green);
                        serversToUpdate.Add(existingSqlData);
                     } 
                  } else {
                     processNewEntry(newSqlData);
                  }
               }
            }
            getServerContent(serversToUpdate);
         });
      });
   }

   private void processNewEntry (ServerSqlData newSqlData) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         ServerSqlData pingedServerData = DB_Main.getLatestServerPing(newSqlData);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            TimeSpan timeSpan = DateTime.UtcNow - pingedServerData.lastPingTime;
            // Will acknowledge server is active if the database entry was updated in less than 10 seconds
            if (timeSpan.TotalSeconds < 10) {
               // Add server data if not yet existing in server data list
               D.editorLog("This server is New, Caching now: " + newSqlData.port + "_" + newSqlData.deviceName, Color.green);
               serversToUpdate.Add(newSqlData);
               addNewServer(newSqlData, false);
            } else {
               // Handle response Error for server inactivity
               //D.editorLog("Will not add this server (" + newSqlData.deviceName + ") it seems to be NOT active : Total Seconds gap is: " + ((int) timeSpan.TotalSeconds) + " Minutes is: " + ((int) timeSpan.TotalMinutes), Color.red);
            }
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

                  if (existingData.latestUpdate != newSqlData.latestUpdate) {
                     D.editorLog("Overwriting dates from: " + existingData.latestUpdate, Color.yellow);
                     D.editorLog("Overwriting dates to: " + newSqlData.latestUpdate, Color.yellow);
                  }
                  existingData.latestUpdate = newSqlData.latestUpdate;
                  overwriteServerData(newSqlData);

                  // Keep our local cache to self updated
                  if (existingData.deviceName == ourDeviceName && existingData.port == ourPort && existingData.ip == ourIp) {
                     ourServerData = existingData;
                  }
               } else {
                  // Fail safe code, assign the missing data incase it is missing
                  if (newSqlData.deviceName == ourDeviceName && newSqlData.port == ourPort && existingData.ip == ourIp) {
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
            existingServer.claimedUserIds = new HashSet<int>(newSqlData.claimedUserIds);
            D.editorLog("Successful Update! " + newSqlData.deviceName, Color.green);

            if (!hasInitiatedVoyage) {
               // Regularly check that there are enough voyage instances open and create more
               VoyageManager.self.regenerateVoyageInstances();
               hasInitiatedVoyage = true;
            }
         } else {
            StartCoroutine(CO_ResetServerData(newSqlData));
            D.editorLog("Missing server, it is not existing yet: " + newSqlData.deviceName + " - " + newSqlData.ip, Color.green);
         }
      }
   }

   private IEnumerator CO_ResetServerData (ServerSqlData newSqlData) {
      yield return new WaitForSeconds(.1f);
      overwriteServerData(newSqlData);
   }

   public void requestCreateVoyage (List<PendingVoyageCreation> voyageList) {
      foreach (PendingVoyageCreation voyageEntry in voyageList) {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            ServerSqlData serverData = serverDataList.Find(_ => _.deviceName == voyageEntry.serverName && _.ip == voyageEntry.serverIp && _.port == voyageEntry.serverPort);
            if (serverData != null) {
               DB_Main.setServerVoyageCreation(voyageEntry, voyageEntry.updateTime, voyageEntry.id);
            } else {
               D.editorLog("Non existing!", Color.yellow);
            }
         });
      }
   }

   public void confirmVoyageCreation (PendingVoyageCreation voyageEntry) {
      voyageEntry.isPending = false;
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.removeServerVoyageCreation(voyageEntry.id);
      });
   }

   #region Simulator Functions

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
         D.editorLog("Creater local user at: Local Server");
         ourServerData.connectedUserIds.Add(newUserId);
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

   public void createInvite (Server server, int groupId, int inviterId, string inviterName, int inviteeId) {
      D.editorLog("The server who owns this user is: " + server.deviceName + " - " + server.port, Color.green);
      VoyageInviteData newVoyageInvite = new VoyageInviteData(server, inviterId, inviterName, inviteeId, groupId, InviteStatus.Pending, DateTime.UtcNow);

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.createVoyageInvite(newVoyageInvite);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {

         });
      });
   }

   private List<VoyageInviteData> generateVoyageInviteForServer (ServerSqlData serverSelected) {
      List<VoyageInviteData> newInviteList = new List<VoyageInviteData>();
      int inviteeId = serverSelected.connectedUserIds[0];
      string inviterName = "Player_";
      for (int i = 0; i < 5; i++) {
         inviterName += _alphabets[Random.Range(1, _alphabets.Length)];
      }
      int groupId = serverSelected.voyageList[0].voyageId;

      newInviteList.Add(new VoyageInviteData(ServerNetwork.self.server, 0, inviterName, inviteeId, groupId, InviteStatus.Pending, DateTime.UtcNow)); 
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

   #endregion

   #region Private Variables

   // Randomized character source
   private string _alphabets = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

   // GUI scroll position
   private Vector2 _scrollPosition;

   // The message to be sent to all players for simulation purposes
   private string _globalMessage;

   // Invitation parameters for simulation
   private string _inviterId;
   private string _inviteeId;
   private string _voyageGroupId;

   #endregion
}