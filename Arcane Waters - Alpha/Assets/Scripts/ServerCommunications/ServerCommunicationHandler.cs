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

      // The maximum count of the processed invite cache, which is a list being referenced by other servers to determine if their invite request is processed
      public const int MAX_PROCESSED_INVITE_CACHE = 100;

      #endregion

      #region Initialization

      private void Awake () {
         self = this;
      }

      public void setPort (int port) {
         D.debug("Server Communication Handler local port is set to: " + port);
         ourPort = port;
      }

      public int getPort () {
         return ourPort;
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

         // Initialize server data writer if this is the main server
         if (ourPort == 7777) {
            ServerDataWriter.self.initializeMainServerInviteFile();
         } 
         InvokeRepeating("processServerDeclaredInvites", 3, 1);

         // Initialize data
         newServerData.voyageList = new List<Voyage>();
         newServerData.pendingVoyageInvites = new List<VoyageInviteData>();
         newServerData.processedVoyageInvites = new List<VoyageInviteData>();
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

      private void processServerDeclaredInvites () {
         // This function is for non main servers, the function will fetch all invites that is declared by the main server and send the invites to the connected users
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            // Fetch invites for this server
            List<VoyageInviteData> serverDeclaredInvites = ServerDataWriter.self.fetchVoyageInvitesFromText();

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               foreach (VoyageInviteData voyageInvite in serverDeclaredInvites) {
                  bool existsInProcessedList = existsInProcessList(ourServerData, voyageInvite);

                  // Send out the invite to the connected user and cache it as processed invites for the origin server to see
                  if (ourServerData.connectedUserIds.Contains(voyageInvite.inviteeId) && !existsInProcessedList) {
                     ServerNetwork.self.server.handleVoyageGroupInvite(voyageInvite.voyageGroupId, voyageInvite.inviterName, voyageInvite.inviteeId);
                     ourServerData.processedVoyageInvites.Add(voyageInvite);

                     // Only keep reference to 100 entries then delete the first entry if it exceeds
                     if (ourServerData.processedVoyageInvites.Count > MAX_PROCESSED_INVITE_CACHE) {
                        ourServerData.processedVoyageInvites.RemoveAt(0);
                     }
                  }
               }
            });
         });

         // If we have a pending voyage invite, check other servers if they have successfully processed it
         if (ourServerData.pendingVoyageInvites.Count > 0) {
            // Cycle through our pending invite list
            List<VoyageInviteData> invitesToRemove = new List<VoyageInviteData>();
            foreach (VoyageInviteData pendingInvite in ourServerData.pendingVoyageInvites) {
               // Cycle through the remote server's processed invite list
               foreach (ServerData serverData in serverDataList) {
                  bool existsInProcessedList = existsInProcessList(serverData, pendingInvite);

                  // If the pending invite exists in the other server's processed list, cache the pending invite as a candidate for removal since it is finished processing
                  if (existsInProcessedList) {
                     invitesToRemove.Add(pendingInvite);
                  }
               }
            }

            // Remove the collected pending invites from our list
            foreach (VoyageInviteData pendingInvite in invitesToRemove) {
               ourServerData.pendingVoyageInvites.Remove(pendingInvite);
            }
         }
      }

      private bool existsInProcessList (ServerData serverData, VoyageInviteData voyageData) {
         return serverData.processedVoyageInvites.Exists(_ => _.inviteeId == voyageData.inviteeId
                  && _.inviterId == voyageData.inviterId
                  && _.voyageGroupId == voyageData.voyageGroupId 
                  && _.creationTime == voyageData.creationTime);
      }

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

      public void createPendingInvite (VoyageInviteData voyageData) {
         ourServerData.pendingVoyageInvites.Add(voyageData);

         // Sync server data and servers
         overwriteNetworkServer(ourServerData, true);
      }

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
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            // Fetch active servers in background thread
            List<ServerData> activeServers = ServerDataWriter.self.getServers();

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               // Handle Server Data update and registry
               serversToUpdate = new List<ServerData>();
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
            });
         });
      }

      private void processNewServerDataEntry (ServerData newServerData) {
         // Fetch last server modification time in main thread since it only accesses the last modification time of the text file
         ServerData fetchedServerData = ServerDataWriter.self.getLastServerUpdate(newServerData);

         // Will acknowledge server is active if the server text file was updated in less than 3 seconds
         TimeSpan timeSpan = DateTime.UtcNow - fetchedServerData.latestUpdateTime;
         if (timeSpan.TotalSeconds < SERVER_ACTIVE_TIME) {
            // Add server data if not yet existing in server data list
            serversToUpdate.Add(newServerData);
            if (!serverDataList.Exists(_ => _.port == newServerData.port)) {
               D.editorLog("This server is recently active, Caching now: " + newServerData.port, Color.green);
               addNewServer(newServerData, false);
            }
         } else {
            D.editorLog("The server was not active for: " + SERVER_ACTIVE_TIME + " Choosing to ignore this server as a remote");
         }
      }

      public void processServerContent (List<ServerData> serverList) {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            // Fetch the content of the recently updated servers in background thread
            List<ServerData> serverListContent = ServerDataWriter.self.getServerContent(serverList);

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               foreach (ServerData newServerData in serverListContent) {
                  ServerData existingData = serverDataList.Find(_ => _.port == newServerData.port);
                  if (existingData != null) {
                     if (existingData.port != ourPort) {
                        // Assign the content of the recently updated servers
                        existingData.voyageList = newServerData.voyageList;
                        existingData.connectedUserIds = newServerData.connectedUserIds;
                        existingData.pendingVoyageInvites = newServerData.pendingVoyageInvites;
                        existingData.processedVoyageInvites = newServerData.processedVoyageInvites;
                        existingData.openAreas = newServerData.openAreas;
                        existingData.latestUpdateTime = newServerData.latestUpdateTime;
                        overwriteNetworkServer(newServerData);
                     }
                  }
               }
            });
         });
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