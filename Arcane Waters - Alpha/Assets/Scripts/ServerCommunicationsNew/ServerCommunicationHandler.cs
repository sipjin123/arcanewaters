using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Networking;
using System;
using System.Text;
using Random = UnityEngine.Random;
using System.Linq;

namespace ServerCommunicationHandlerv2 {
   public class ServerCommunicationHandler : MonoBehaviour {
      #region Public Variables

      // Self
      public static ServerCommunicationHandler self;

      // The device name of our server
      public string ourDeviceName;

      // Our server ip
      public string ourIp;

      // Our server port
      public int ourPort;

      // The prefab to spawn
      public Server serverPrefab;

      // Cached info of our local server data
      public ServerData ourServerData = new ServerData();

      // Server data of all servers
      public List<ServerData> serverDataList = new List<ServerData>();

      // The servers that needs updating
      public List<ServerData> serversToUpdate = new List<ServerData>();

      // The valid time interval to determine if server is active
      public static int SERVER_ACTIVE_TIME = 3;

      // Reference to the shared server data handler
      public SharedServerDataHandler sharedServerDataHandler;

      #endregion

      #region Initialization

      private void Awake () {
         self = this;
      }

      public void initializeServers () {
         initializeLocalServer();
      }

      private void initializeLocalServer () {
         ourDeviceName = SystemInfo.deviceName;
         ourIp = MyNetworkManager.self.networkAddress;
         ServerData newServerData = new ServerData {
            ip = ourIp,
            port = ourPort,
            deviceName = ourDeviceName
         };

         // Initialize data
         newServerData.voyageList = new List<Voyage>();
         newServerData.connectedUserIds = new List<int>();
         newServerData.openAreas = new List<string>();
         newServerData.latestUpdateTime = DateTime.UtcNow;

         // Clear old data
         ServerDataWriter.self.clearExistingData(newServerData);

         // Keep reference to self and add our server to the list
         ourServerData = newServerData;
         addNewServer(newServerData, true);

         // Continuously write data to the text file, checks other servers and if they are not active then the inactive server will be removed
         InvokeRepeating("confirmServerActivity", 1, 1);

         // Continuously read the data of the other active servers
         InvokeRepeating("fetchOtherServerData", 1, 1);

         // Set shared data handler to continuously process voyage and chat related features
         sharedServerDataHandler.initializeHandler();
      }

      #endregion

      private void confirmServerActivity () {
         List<ServerData> fetchedServerData = new List<ServerData>();

         // Frequently update the local server text data
         ourServerData.latestUpdateTime = DateTime.UtcNow;
         ServerDataWriter.self.updateServerData(ourServerData);

         // Fetch the latest modification time of the servers text files
         foreach (ServerData serverData in serverDataList) {
            if (!isOurServer(serverData)) {
               ServerData newServerData = ServerDataWriter.self.getLastServerUpdate(serverData);
               fetchedServerData.Add(newServerData);
            }
         }

         foreach (ServerData serverData in fetchedServerData) {
            // Check if the other servers are active recently
            TimeSpan fileAlterInterval = DateTime.UtcNow - serverData.latestUpdateTime;
            if (fileAlterInterval.TotalSeconds > SERVER_ACTIVE_TIME) {
               D.editorLog("Removing Server Entry due to inactivity: " + serverData.port + " Time Delay: " + fileAlterInterval.TotalSeconds, Color.red);

               // Remove the server data entry from the list
               ServerData selectedServerData = serverDataList.Find(_ => _.port == serverData.port);
               if (selectedServerData != null) {
                  serverDataList.Remove(selectedServerData);
               }

               // Remove the server entry from the list
               Server selectedServer = ServerNetwork.self.servers.ToList().Find(_ => _.port == serverData.port);
               if (selectedServer != null) {
                  ServerNetwork.self.removeServer(selectedServer);
               }
            }
         }
      }

      #region UserManagement

      public void addPlayer (int userId) {
         if (!ourServerData.connectedUserIds.Contains(userId)) {
            ourServerData.connectedUserIds.Add(userId);

            // Sync server data and servers
            overwriteNetworkServer(ourServerData, true);
         }
      }

      public void removePlayer (int userId) {
         if (ourServerData.connectedUserIds.Contains(userId)) {
            ourServerData.connectedUserIds.Remove(userId);

            // Sync server data and servers
            overwriteNetworkServer(ourServerData, true);
         }
      }

      public void claimPlayer (int userId) {
         if (!ourServerData.claimedUserIds.Contains(userId)) {
            ourServerData.claimedUserIds.Add(userId);

            // Sync server data and servers
            overwriteNetworkServer(ourServerData, true);
         }
      }

      public void releasePlayerClaim (int userId) {
         if (ourServerData.claimedUserIds.Contains(userId)) {
            ourServerData.claimedUserIds.Remove(userId);

            // Sync server data and servers
            overwriteNetworkServer(ourServerData, true);
         }
      }

      #endregion

      #region Server Data Features

      private void addNewServer (ServerData serverData, bool isLocal) {
         serverDataList.Add(serverData);
         Server server = Instantiate(serverPrefab, Vector3.zero, Quaternion.identity);
         server.gameObject.name = "Server_" + serverData.port;
         DontDestroyOnLoad(server.gameObject);

         server.deviceName = ourDeviceName;
         server.ipAddress = ourIp;
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

         if (ServerNetwork.self != null) {
            if (isLocal) {
               ServerNetwork.self.server = server;
               ServerNetwork.self.server.setAsLocalServer();
            } else {
               ServerNetwork.self.servers.Add(server);
            }
         }
      }

      public void fetchOtherServerData () {
         // Fetch all server update info
         List<ServerData> activeServers = ServerDataWriter.self.getServers();
         serversToUpdate = new List<ServerData>();

         // Handle Server Data update and registry
         foreach (ServerData newServerData in activeServers) {
            if (newServerData.port != ourPort) {
               ServerData existingServerData = serverDataList.Find(_ => _.port == newServerData.port);
               if (existingServerData != null) {
                  serversToUpdate.Add(existingServerData);
               } else {
                  processNewServerDataEntry(newServerData);
               }
            }
         }
         processServerContent(serversToUpdate);
      }

      private void processNewServerDataEntry (ServerData newServerData) {
         ServerData fetchedServerData = ServerDataWriter.self.getLastServerUpdate(newServerData);
         TimeSpan timeSpan = DateTime.UtcNow - fetchedServerData.latestUpdateTime;

         // Will acknowledge server is active if the server text file was updated in less than 3 seconds
         if (timeSpan.TotalSeconds < SERVER_ACTIVE_TIME) {
            // Add server data if not yet existing in server data list
            serversToUpdate.Add(newServerData);
            if (!serverDataList.Exists(_ => _.port == newServerData.port)) {
               D.editorLog("This server is recently active, Caching now: " + newServerData.port, Color.green);
               addNewServer(newServerData, false);
            }
         }
      }

      public void processServerContent (List<ServerData> serverList) {
         // Fetch the content of the recently updated servers
         List<ServerData> serverListContent = ServerDataWriter.self.getServerContent(serverList);

         foreach (ServerData newServerData in serverListContent) {
            ServerData existingData = serverDataList.Find(_ => _.port == newServerData.port);
            if (existingData != null) {
               if (existingData.port != ourPort) {
                  // Assign the content of the recently updated servers
                  existingData.voyageList = newServerData.voyageList;
                  existingData.connectedUserIds = newServerData.connectedUserIds;
                  existingData.openAreas = newServerData.openAreas;
                  existingData.latestUpdateTime = newServerData.latestUpdateTime;
                  overwriteNetworkServer(newServerData);
               }
            } 
         }
      }

      private void overwriteNetworkServer (ServerData newData, bool localServer = false) {
         if (ServerNetwork.self != null) {
            Server existingServer = ServerNetwork.self.servers.ToList().Find(_ => _.port == newData.port);
            if (existingServer != null) {
               existingServer.openAreas = newData.openAreas.ToArray();
               existingServer.voyages = newData.voyageList;
               existingServer.connectedUserIds = new HashSet<int>(newData.connectedUserIds);
               existingServer.claimedUserIds = new HashSet<int>(newData.claimedUserIds);

               if (localServer) {
                  ServerNetwork.self.server.openAreas = newData.openAreas.ToArray();
                  ServerNetwork.self.server.voyages = newData.voyageList;
                  ServerNetwork.self.server.connectedUserIds = new HashSet<int>(newData.connectedUserIds);
                  ServerNetwork.self.server.claimedUserIds = new HashSet<int>(newData.claimedUserIds);
               }
            } else {
               StartCoroutine(CO_ResetServerData(newData));
               D.editorLog("Missing server, it is not existing yet: " + newData.port, Color.green);
            }
         }
      }

      private IEnumerator CO_ResetServerData (ServerData newData) {
         yield return new WaitForSeconds(.1f);
         overwriteNetworkServer(newData);
      }

      #endregion

      public void refreshVoyageInstances (List<Voyage> voyageList) {
         ourServerData.voyageList = voyageList;

         // Sync server data and servers
         overwriteNetworkServer(ourServerData, true);
      }

      public ServerData getServerData (int port) {
         return serverDataList.Find(_ => _.port == port);
      }

      public bool isOurServer (ServerData serverData) {
         return (serverData.port == ourPort);
      }

      #region Private Variables

      #endregion
   }
}