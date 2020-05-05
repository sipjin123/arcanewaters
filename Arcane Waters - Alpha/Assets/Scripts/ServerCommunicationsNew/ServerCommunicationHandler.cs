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
      public ServerSqlData ourServerData = new ServerSqlData();

      // The chat list existing in the database
      public List<ChatInfo> chatInfoList = new List<ChatInfo>();

      // The chat id's sent to all players
      public List<int> distributedChatInfoList = new List<int>();

      // Server data of all servers
      public List<ServerSqlData> serverDataList = new List<ServerSqlData>();

      // List of voyages to be created
      public List<PendingVoyageCreation> pendingVoyageCreations = new List<PendingVoyageCreation>();

      // Make sure to never re-spawn instantiated instances
      public List<int> createdVoyageIds = new List<int>();

      // Caches the voyages that have been marked as not pending in the database
      public List<int> disposedVoyageIds = new List<int>();

      // List of voyage invitations
      public List<VoyageInviteData> pendingVoyageInvites = new List<VoyageInviteData>();

      // The latest chat info
      public ChatInfo latestChatinfo = new ChatInfo();

      // The servers that needs updating
      public List<ServerSqlData> serversToUpdate = new List<ServerSqlData>();

      // The valid time interval to determine if server is active
      public static int SERVER_ACTIVE_TIME = 10;

      // Determines if is using the local database
      public bool isLocalDatabase = false;

      // The time our local server initialized
      private DateTime localServerStartTime;

      #endregion

      #region Initialization

      private void Awake () {
         self = this;
      }

      public void initializeServers () {
         initializeLocalServer();

         InvokeRepeating("checkServerUpdates", 2, .5f);
      }

      private void confirmServerActivity () {
         List<ServerSqlData> fetchedServerData = new List<ServerSqlData>();
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            // Frequently send to the database to confirm that this server is active
            ourServerData.lastPingTime = DateTime.UtcNow;
            ServerDataWritter.self.updateServerData(ourServerData);

            // Fetch the ping of the other servers
            foreach (ServerSqlData serverData in serverDataList) {
               if (!isOurServer(serverData)) {
                  ServerSqlData newServerData = ServerDataWritter.self.getServerPing(serverData);
                  fetchedServerData.Add(newServerData);
               }
            }

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               foreach (ServerSqlData serverData in fetchedServerData) {
                  // Check if the other servers are active recently
                  TimeSpan pingInterval = DateTime.UtcNow - serverData.lastPingTime;
                  if (pingInterval.TotalSeconds > SERVER_ACTIVE_TIME) {
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

      private void initializeLocalServer () {
         localServerStartTime = DateTime.UtcNow;
         ourDeviceName = SystemInfo.deviceName;
         ourIp = MyNetworkManager.self.networkAddress;
         ServerSqlData newSqlData = new ServerSqlData {
            ip = ourIp,
            port = ourPort,
            deviceName = ourDeviceName
         };

         // If server is ours and just initialized, clear data
         updateServerDatabase(newSqlData, false);

         // Keep reference to self
         ourServerData = newSqlData;

         // Send updates to database to confirm that local server is active, checks other servers and if they are not active then the inactive server will be removed
         InvokeRepeating("confirmServerActivity", 1, 1);

         addNewServer(newSqlData, true);
      }

      #endregion

      public bool isOurServer (ServerSqlData serverData) {
         return (serverData.deviceName == ourDeviceName && serverData.ip == ourIp && serverData.port == ourPort);
      }

      private void processChatList () {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<ChatInfo> serverChatList = DB_Main.getChat(ChatInfo.Type.Global, 0, false, 20);
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               List<ChatInfo> sortedChatList = serverChatList.OrderByDescending(_ => _.chatTime).ToList();
               latestChatinfo = sortedChatList[0];
               chatInfoList = sortedChatList;

               if (!distributedChatInfoList.Contains(latestChatinfo.chatId)) {
                  // Prevents chat spam
                  distributedChatInfoList.Add(latestChatinfo.chatId);

                  // Make sure to fetch only chats created after our local server is initialized
                  if (latestChatinfo.chatTime >= localServerStartTime) {
                     foreach (int userId in ourServerData.connectedUserIds) {
                        NetEntity targetEntity = EntityManager.self.getEntity(userId);
                        targetEntity.Target_ReceiveGlobalChat(latestChatinfo.chatId, latestChatinfo.text, latestChatinfo.chatTime.ToBinary(), "Server", 0);
                     }
                  }
               }
            });
         });
      }

      #region UserManagement

      public void addPlayer (int userId) {
         if (!ourServerData.connectedUserIds.Contains(userId)) {
            ourServerData.connectedUserIds.Add(userId);

            // Sync server data and servers
            overwriteServerData(ourServerData, true);

            // Send updated values to the database
            updateServerDatabase(ourServerData, true);
         }
      }

      public void removePlayer (int userId) {
         if (ourServerData.connectedUserIds.Contains(userId)) {
            ourServerData.connectedUserIds.Remove(userId);

            // Sync server data and servers
            overwriteServerData(ourServerData, true);

            // Send updated values to the database
            updateServerDatabase(ourServerData, true);
         }
      }

      public void claimPlayer (int userId) {
         if (!ourServerData.claimedUserIds.Contains(userId)) {
            ourServerData.claimedUserIds.Add(userId);

            // Sync server data and servers
            overwriteServerData(ourServerData, true);

            // Send updated values to the database
            updateServerDatabase(ourServerData, true);
         }
      }

      public void releasePlayerClaim (int userId) {
         if (ourServerData.claimedUserIds.Contains(userId)) {
            ourServerData.claimedUserIds.Remove(userId);

            // Sync server data and servers
            overwriteServerData(ourServerData, true);

            // Send updated values to the database
            updateServerDatabase(ourServerData, true);
         }
      }

      #endregion

      #region Server Data Features

      private void addNewServer (ServerSqlData serverData, bool isLocal) {
         serverDataList.Add(serverData);
         Server server = Instantiate(serverPrefab, Vector3.zero, Quaternion.identity);
         server.gameObject.name = "Server_" + serverData.deviceName;
         DontDestroyOnLoad(server.gameObject);

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

         if (ServerNetwork.self != null) {
            if (isLocal) {
               ServerNetwork.self.server = server;
               ServerNetwork.self.server.setAsLocalServer();
            } else {
               ServerNetwork.self.servers.Add(server);
            }
         }
      }

      public void updateServerDatabase (ServerSqlData serverData, bool retainData = false) {
         if (!retainData) {
            serverData.voyageList = new List<Voyage>();
            serverData.connectedUserIds = new List<int>();
            serverData.openAreas = new List<string>();
         }
         serverData.latestUpdate = DateTime.UtcNow;
         serverData.lastPingTime = serverData.latestUpdate;
      }

      public List<PendingVoyageCreation> newCopyOfVoyageCreations (List<PendingVoyageCreation> fetchedVoyageCreation) {
         List<PendingVoyageCreation> newCopy = new List<PendingVoyageCreation>();
         List<PendingVoyageCreation> sortedList = fetchedVoyageCreation.FindAll(_ => (_.updateTime - ourServerData.latestUpdate).TotalSeconds < 2);
         foreach (PendingVoyageCreation voyageEntry in sortedList) {
            newCopy.Add(new PendingVoyageCreation {
               areaKey = voyageEntry.areaKey,
               isPvP = voyageEntry.isPvP,
               id = voyageEntry.id,
               isPending = voyageEntry.isPending,
               serverIp = voyageEntry.serverIp,
               serverName = voyageEntry.serverName,
               serverPort = voyageEntry.serverPort,
               updateTime = DateTime.UtcNow
            });
         }

         return newCopy;
      }

      public void checkServerUpdates () {
         serversToUpdate = new List<ServerSqlData>();

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            // Fetch all server update info
            List<ServerSqlData> serverListUpdateTime = ServerDataWritter.self.getServersUpdateTime(ourServerData);
            List<PendingVoyageCreation> fetchedVoyageCreation = DB_Main.getPendingVoyageCreations(ourDeviceName);
            ChatInfo latestChatInfo = DB_Main.getLatestChatInfo();
            pendingVoyageInvites = DB_Main.getAllVoyageInvites();

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               // Make sure not to fetch old data
               pendingVoyageCreations = newCopyOfVoyageCreations(fetchedVoyageCreation);

               // Handle new voyage creation data
               handleVoyageCreation(pendingVoyageCreations);

               // Handle new voyage invite data
               handleVoyageInvites();

               // Handle new chat data
               if (latestChatinfo.chatId > 0) {
                  List<ChatInfo> chatList = chatInfoList.OrderByDescending(_ => _.chatTime).ToList();
                  if (chatList.Count > 0) {
                     if (latestChatInfo.chatTime > chatList[0].chatTime) {
                        processChatList();
                     }
                  }
               } else {
                  processChatList();
               }

               // Handle Server Data update and registry
               foreach (ServerSqlData newSqlData in serverListUpdateTime) {
                  if (newSqlData.deviceName == ourDeviceName && newSqlData.port == ourPort && newSqlData.ip == ourIp) {
                     ServerSqlData existingSqlData = serverDataList.Find(_ => _.deviceName == newSqlData.deviceName && _.port == newSqlData.port && _.ip == newSqlData.ip);
                     if (existingSqlData != null) {
                        // Update our data to make sure we are in sync with the database
                        TimeSpan updateSpan = (existingSqlData.latestUpdate - newSqlData.latestUpdate);
                        if (updateSpan.TotalSeconds > 1) {
                           updateServerDatabase(ourServerData, true);
                        }
                     }
                  } else {
                     ServerSqlData existingSqlData = serverDataList.Find(_ => _.deviceName == newSqlData.deviceName && _.port == newSqlData.port && _.ip == newSqlData.ip);
                     if (existingSqlData != null) {
                        // Update the server data if it was modified
                        if (newSqlData.latestUpdate > existingSqlData.latestUpdate) {
                           //D.editorLog("The server: " + newSqlData.deviceName + " has updated its data from: " + existingSqlData.latestUpdate + " to " + newSqlData.latestUpdate, Color.green);
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
            ServerSqlData pingedServerData = ServerDataWritter.self.getServerPing(newSqlData);

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               TimeSpan timeSpan = DateTime.UtcNow - pingedServerData.lastPingTime;
               // Will acknowledge server is active if the database entry was updated in less than 10 seconds
               if (timeSpan.TotalSeconds < SERVER_ACTIVE_TIME) {
                  // Add server data if not yet existing in server data list
                  serversToUpdate.Add(newSqlData);
                  if (!serverDataList.Exists(_ => _.deviceName == newSqlData.deviceName && _.port == newSqlData.port && _.ip == newSqlData.ip)) {
                     D.editorLog("This server is New, Caching now: " + newSqlData.port + "_" + newSqlData.deviceName, Color.green);
                     addNewServer(newSqlData, false);
                  }
               }
            });
         });
      }

      public void getServerContent (List<ServerSqlData> serverList) {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            // Fetch the content of the recently updated servers
            List<ServerSqlData> serverListContent = ServerDataWritter.self.getServerContent(serverList);

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               foreach (ServerSqlData newSqlData in serverListContent) {
                  ServerSqlData existingData = serverDataList.Find(_ => _.deviceName == newSqlData.deviceName && _.port == newSqlData.port && _.ip == newSqlData.ip);
                  if (existingData != null) {
                     // Assign the content of the recently updated servers
                     existingData.voyageList = newSqlData.voyageList;
                     existingData.connectedUserIds = newSqlData.connectedUserIds;
                     existingData.openAreas = newSqlData.openAreas;
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

      private void overwriteServerData (ServerSqlData newSqlData, bool localServer = false) {
         if (ServerNetwork.self != null) {
            Server existingServer = ServerNetwork.self.servers.ToList().Find(_ => _.deviceName == newSqlData.deviceName && _.ipAddress == newSqlData.ip && _.port == newSqlData.port);
            if (existingServer != null) {
               existingServer.openAreas = newSqlData.openAreas.ToArray();
               existingServer.voyages = newSqlData.voyageList;
               existingServer.connectedUserIds = new HashSet<int>(newSqlData.connectedUserIds);
               existingServer.claimedUserIds = new HashSet<int>(newSqlData.claimedUserIds);

               if (localServer) {
                  ServerNetwork.self.server.openAreas = newSqlData.openAreas.ToArray();
                  ServerNetwork.self.server.voyages = newSqlData.voyageList;
                  ServerNetwork.self.server.connectedUserIds = new HashSet<int>(newSqlData.connectedUserIds);
                  ServerNetwork.self.server.claimedUserIds = new HashSet<int>(newSqlData.claimedUserIds);
               }

            } else {
               StartCoroutine(CO_ResetServerData(newSqlData));
               D.editorLog("Missing server, it is not existing yet: " + newSqlData.deviceName + " - " + newSqlData.ip + " - " + newSqlData.port, Color.green);
            }
         }
      }

      private IEnumerator CO_ResetServerData (ServerSqlData newSqlData) {
         yield return new WaitForSeconds(.1f);
         overwriteServerData(newSqlData);
      }

      public void createInvite (Server server, int groupId, int inviterId, string inviterName, int inviteeId) {
         VoyageInviteData newVoyageInvite = new VoyageInviteData(server, inviterId, inviterName, inviteeId, groupId, InviteStatus.Created, DateTime.UtcNow);

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            DB_Main.createVoyageInvite(newVoyageInvite);
         });
      }

      #endregion

      #region Voyage Features

      public void requestCreateVoyage (List<PendingVoyageCreation> voyageList) {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            foreach (PendingVoyageCreation voyageEntry in voyageList) {
               ServerSqlData serverData = serverDataList.Find(_ => _.deviceName == voyageEntry.serverName && _.ip == voyageEntry.serverIp && _.port == voyageEntry.serverPort);
               if (serverData != null) {
                  DB_Main.setServerVoyageCreation(voyageEntry, voyageEntry.updateTime, voyageEntry.id);
               }
            }
         });
      }

      private void handleVoyageInvites () {
         foreach (VoyageInviteData voyageInvite in pendingVoyageInvites) {
            if (voyageInvite.inviteStatus == InviteStatus.Created) {
               Server selectedServer = ServerNetwork.self.getServerContainingUser(voyageInvite.inviteeId);
               if (selectedServer != null) {
                  selectedServer.handleVoyageGroupInvite(voyageInvite.voyageGroupId, voyageInvite.inviterName, voyageInvite.inviteeId);
                  voyageInvite.inviteStatus = InviteStatus.Pending;
                  UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
                     DB_Main.modifyServerVoyageInvite(voyageInvite.voyageXmlId, voyageInvite.inviteStatus);
                  });
               } else {
                  D.editorLog("Could not find the server: " + voyageInvite.serverName + " - " + voyageInvite.serverPort, Color.red);
               }
            }
         }
      }

      private void handleVoyageCreation (List<PendingVoyageCreation> newVoyageCreations) {
         // Create instances of the voyages
         foreach (PendingVoyageCreation voyageCreations in newVoyageCreations) {
            if (ServerNetwork.self != null) {
               if (voyageCreations.serverName == ourDeviceName && voyageCreations.serverPort == ourPort && voyageCreations.serverIp == ourIp) {
                  if (!createdVoyageIds.Contains(voyageCreations.id)) {
                     voyageCreations.isPending = false;
                     string areaKey = voyageCreations.areaKey;
                     bool isPvP = voyageCreations.isPvP;
                     int idToRemove = voyageCreations.id;

                     if (idToRemove > 0) {
                        createdVoyageIds.Add(idToRemove);
                        VoyageManager.self.createVoyageInstance(areaKey, isPvP);
                     }
                  }
               }
            }
         }

         // Marks all the pending create voyage as NOT pending
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            foreach (PendingVoyageCreation voyageCreations in newVoyageCreations) {
               if (!disposedVoyageIds.Contains(voyageCreations.id)) {
                  disposedVoyageIds.Add(voyageCreations.id);
                  DB_Main.removeServerVoyageCreation(voyageCreations.id);
               }
            }
         });
      }

      public void respondVoyageInvite (int groupId, string inviterName, int inviteeId, InviteStatus status) {
         VoyageInviteData selectedVoyageInvite = pendingVoyageInvites.Find(_ => _.voyageGroupId == groupId && _.inviterName == inviterName && _.inviteeId == inviteeId);
         selectedVoyageInvite.inviteStatus = status;

         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            DB_Main.modifyServerVoyageInvite(selectedVoyageInvite.voyageXmlId, selectedVoyageInvite.inviteStatus);
         });
      }

      public void refreshVoyageInstances (List<Voyage> voyageList) {
         ourServerData.voyageList = voyageList;

         // Sync server data and servers
         overwriteServerData(ourServerData, true);

         // Send updated values to the database
         updateServerDatabase(ourServerData, true);
      }

      #endregion

      #region Private Variables

      #endregion
   }

}