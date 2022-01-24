using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MLAPI;
using MLAPI.NetworkedVar;
using MLAPI.NetworkedVar.Collections;
using MLAPI.Serialization;
using System.IO;
using MLAPI.Serialization.Pooled;
using Newtonsoft.Json;
using MLAPI.Messaging;
using System;
using System.Linq;
using System.Text;

public class NetworkedServer : NetworkedBehaviour
{
   #region Public Variables

   // The port associated with this Networked Server
   public NetworkedVar<int> networkedPort = new NetworkedVar<int>(Global.defaultNetworkedVarSettings);

   // The users connected to this server
   public NetworkedList<int> connectedUserIds = new NetworkedList<int>(Global.defaultNetworkedVarSettings);

   // The private farm instances that already exists
   public NetworkedList<int> privateFarmInstances = new NetworkedList<int>(Global.defaultNetworkedVarSettings);

   // The private house instances that already exists
   public NetworkedList<int> privateHouseInstances = new NetworkedList<int>(Global.defaultNetworkedVarSettings);
   
   // The users that have been assigned to this server - there can be userIds duplicates in multiple servers
   public NetworkedDictionary<int, AssignedUserInfo> assignedUserIds = new NetworkedDictionary<int, AssignedUserInfo>(new NetworkedVarSettings { WritePermission = NetworkedVarPermission.Everyone, SendChannel = "Fragmented", SendTickrate = 0 });

   // The accounts connected to this server
   public NetworkedDictionary<int, bool> connectedAccountIds = new NetworkedDictionary<int, bool>(new NetworkedVarSettings { WritePermission = NetworkedVarPermission.OwnerOnly, SendChannel = "Fragmented", SendTickrate = 0 });

   // The voyage groups stored in this server
   public NetworkedDictionary<int, VoyageGroupInfo> voyageGroups = new NetworkedDictionary<int, VoyageGroupInfo>(new NetworkedVarSettings { WritePermission = NetworkedVarPermission.Everyone, SendChannel = "Fragmented", SendTickrate = 0 });

   // The voyage instances hosted in this server
   public NetworkedDictionary<int, Voyage> voyages = new NetworkedDictionary<int, Voyage>(Global.defaultNetworkedVarSettings);

   // The treasure site instances hosted in this server
   public NetworkedDictionary<int, Voyage> treasureSites = new NetworkedDictionary<int, Voyage>(Global.defaultNetworkedVarSettings);

   // Keeps track of the users claimed by this server
   public NetworkedDictionary<int, bool> claimedUserIds = new NetworkedDictionary<int, bool>(new NetworkedVarSettings { WritePermission = NetworkedVarPermission.OwnerOnly, SendChannel = "Fragmented", SendTickrate = 0 });

   // Account dictionary overrides
   public NetworkedDictionary<string, string> accountOverrides = new NetworkedDictionary<string, string>();

   // The number of instances in each area that this server hosts
   public NetworkedDictionary<string, int> areaToInstanceCount = new NetworkedDictionary<string, int>(Global.defaultNetworkedVarSettings);

   // The number of players in each area (estimated by the master server)
   public Dictionary<string, int> playerCountPerArea = new Dictionary<string, int>();

   #endregion

   private void Start () {
      // We only want to make modifications to this Networked Server if we are the owner
      if (this.IsOwner) {
         // Assign this server's port to this Networked Server object
         this.networkedPort.Value = MyNetworkManager.getCurrentPort();

         // Register this server as our own
         ServerNetworkingManager.self.server = this;

         // Regularly update data shared between servers
         InvokeRepeating(nameof(updateConnectedPlayers), 5f, 1f);
         InvokeRepeating(nameof(updateAssignedUsers), 5f, 1f);
      }

      // Register this server
      ServerNetworkingManager.self.servers.Add(this);
   }

   private void Update () {
      // Set the name of this object based on the port
      this.name = "Networked Server #" + this.networkedPort.Value;

      // Record frame times for FPS calculation
      if (this.IsOwner) {
         _pastFrameTimes.AddLast(Time.time);
         if (_pastFrameTimes.Count > 60) {
            _pastFrameTimes.RemoveFirst();
         }
      }
   }

   private void updateConnectedPlayers () {
      connectedUserIds.Clear();

      foreach (ClientConnectionData connData in MyNetworkManager.getClientConnectionData()) {
         if (connData != null) {
            // Add the user
            if (connData.netEntity != null) {
               connectedUserIds.Add(connData.netEntity.userId);
            }
         }
      }
   }

   private void updateAssignedUsers () {
      List<AssignedUserInfo> assignedUserInfoList = new List<AssignedUserInfo>(assignedUserIds.Values);
      foreach (AssignedUserInfo userInfo in assignedUserInfoList) {
         // Update the online time of the user
         if (connectedUserIds.Contains(userInfo.userId)) {
            // Note that this will not cause a synchronization of the dictionary entry in the server network (not needed)
            userInfo.lastOnlineTime = DateTime.UtcNow.ToBinary();
         }

         // If the user has been offline for too long, remove him from the assigned users
         if ((DateTime.UtcNow - DateTime.FromBinary(userInfo.lastOnlineTime)).TotalSeconds > DisconnectionManager.SECONDS_UNTIL_PLAYERS_DESTROYED * 1.5f) {
            assignedUserIds.Remove(userInfo.userId);
            MyNetworkManager.self.onUserUnassignedFromServer(userInfo.userId);
         }
      }
   }

   public void updateVoyageInstance (Instance instance) {
      // PvP attributes
      int playerCountTeamA = 0;
      int playerCountTeamB = 0;
      int pvpGameMaxPlayerCount = 0;
      PvpGame.State pvpGameState = PvpGame.State.None;

      PvpGame pvpGame = PvpManager.self.getGameWithInstance(instance.id);
      if (pvpGame != null) {
         playerCountTeamA = pvpGame.getPlayerCountInTeam(PvpTeamType.A);
         playerCountTeamB = pvpGame.getPlayerCountInTeam(PvpTeamType.B);
         pvpGameState = pvpGame.getGameState();
         pvpGameMaxPlayerCount = pvpGame.getMaxPlayerCount();
      }

      int groupCount = VoyageGroupManager.self.getGroupCountInVoyage(instance.voyageId);

      Voyage voyage = new Voyage(instance.voyageId, instance.id, instance.areaKey, Area.getName(instance.areaKey), instance.difficulty, instance.biome, instance.isPvP,
            instance.isLeague, instance.leagueIndex, instance.leagueRandomSeed, instance.leagueExitAreaKey, instance.leagueExitSpawnKey, instance.leagueExitFacingDirection, instance.creationDate, instance.treasureSiteCount, instance.capturedTreasureSiteCount, instance.aliveNPCEnemiesCount,
            instance.getTotalNPCEnemyCount(), groupCount, instance.getPlayerCount(), playerCountTeamA, playerCountTeamB, pvpGameMaxPlayerCount, pvpGameState);

      if (VoyageManager.isTreasureSiteArea(voyage.areaKey)) {
         treasureSites[voyage.voyageId] = voyage;
      } else {
         voyages[voyage.voyageId] = voyage;
      }
   }

   public void removeVoyageInstance (Instance instance) {
      if (instance.voyageId <= 0) {
         return;
      }

      if (VoyageManager.isTreasureSiteArea(instance.areaKey)) {
         treasureSites.Remove(instance.voyageId);
      } else {
         voyages.Remove(instance.voyageId);
      }
   }

   public void synchronizeConnectedAccount (int accountId) {
      bool isAccountConnected = false;
      foreach (ClientConnectionData connData in MyNetworkManager.getClientConnectionData()) {
         if (connData != null && connData.isAuthenticated() && connData.accountId == accountId && !DisconnectionManager.self.isUserPendingDisconnection(connData.userId)) {
            isAccountConnected = true;
            break;
         }
      }

      if (isAccountConnected) {
         connectedAccountIds[accountId] = true;
      } else {
         connectedAccountIds.Remove(accountId);
      }
   }

   public bool isMasterServer () {
      return networkedPort.Value == Global.MASTER_SERVER_PORT;
   }

   [ServerRPC]
   public void recalculateOpenAreas () {
      if (!isMasterServer()) {
         return;
      }

      // Count the number of players assigned to each area
      playerCountPerArea.Clear();
      foreach (KeyValuePair<int, AssignedUserInfo> KV in assignedUserIds) {
         if (KV.Value.isSinglePlayer) {
            continue;
         }

         if (playerCountPerArea.ContainsKey(KV.Value.areaKey)) {
            playerCountPerArea[KV.Value.areaKey]++;
         } else {
            playerCountPerArea[KV.Value.areaKey] = 1;
         }
      }
   }

   [ServerRPC]
   public bool hasOpenArea (string areaKey) {
      if (playerCountPerArea.ContainsKey(areaKey)) {
         // Get the number of instances in that area
         areaToInstanceCount.TryGetValue(areaKey, out int instanceCount);

         if (playerCountPerArea[areaKey] < Instance.getMaxPlayerCount(areaKey, false) * instanceCount) {
            return true;
         }
      }

      return false;
   }

   private void OnDestroy () {
      ServerNetworkingManager.self.servers.Remove(this);
   }

   [ServerRPC]
   public void MasterServer_SendPvpAnnouncement (int instanceId, string message, string senderName, string recipient) {
      InvokeClientRpcOnEveryone(ClientRPCReceivePvpAnnouncement, instanceId, message, senderName, recipient);
   }

   [ClientRPC]
   public void ClientRPCReceivePvpAnnouncement (int instanceId, string message, string senderName, string recipient) {
      D.adminLog("Sending client rpc chat message to player {" + recipient + "} for pvp announcement: {" + senderName + "}", D.ADMIN_LOG_TYPE.PvpAnnouncement);
      int recipientId = 0;
      try {
         recipientId = int.Parse(recipient);
      } catch {

      }

      if (recipientId < 1) {
         D.adminLog("Invalid Pvp Announcement Recipient: " + recipientId, D.ADMIN_LOG_TYPE.PvpAnnouncement);
         return;
      }

      // Send the chat message to all users connected to this server
      foreach (NetEntity netEntity in MyNetworkManager.getPlayers()) {
         // Make sure that only players in voyage or battle will not receive this announcement
         if (netEntity.battleId > 0) {
            D.adminLog("Invalid Pvp Announcement, {" + netEntity.userId + ":" + netEntity.entityName + "} is in a battle!", D.ADMIN_LOG_TYPE.PvpAnnouncement);
            continue;
         }

         if (netEntity.userId == recipientId) {
            netEntity.Target_ReceivePvpChat(netEntity.connectionToClient, instanceId, message);
         }
      }
   }

   #region Visit System

   [ServerRPC]
   public void MasterServer_FindUserPrivateAreaVisit (int visitorUserId, int visitedUserId, string areaKeyOverride, string spawnTarget, Direction facing) {
      // This function is in charge of determining if there is a server that contains the private instance being visited
      NetworkedServer targetServer = ServerNetworkingManager.self.server;

      if (areaKeyOverride.Contains(CustomFarmManager.GROUP_AREA_KEY)) {
         targetServer = ServerNetworkingManager.self.getServerContainingPrivateFarmInstance(visitedUserId);
      } else if (areaKeyOverride.Contains(CustomHouseManager.GROUP_AREA_KEY)) {
         targetServer = ServerNetworkingManager.self.getServerContainingPrivateHouseInstance(visitedUserId);
      } else {
         targetServer = null;
      }
      
      if (targetServer != null) {
         // Find the server that has the private instance of an existing user
         targetServer.InvokeClientRpcOnOwner(Server_FindUserPrivateLocationToVisit, visitorUserId, visitedUserId, areaKeyOverride, spawnTarget, facing);
      } else {
         // Find the server where the visitor is in, then send the deny notification to that server
         NetworkedServer sourceServer = ServerNetworkingManager.self.getServerContainingUser(visitorUserId);
         if (sourceServer != null) {
            sourceServer.InvokeClientRpcOnOwner(Server_PrivateInstanceDoestNotExist, visitorUserId, visitedUserId, areaKeyOverride, Vector2.zero);
         } else {
            D.debug("Failed to find Source Server {" + visitedUserId + "} for area {" + areaKeyOverride + "}");
         }
      }
   }

   [ClientRPC]
   public void Server_FindUserPrivateLocationToVisit (int visitorUserId, int visitedUserId, string privateInstanceAreakey, string spawnTarget, Direction facing) {
      // This function is in charge of preparing the data to send out to the source server to determine the target server containing the private instance
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         UserInfo ownerUserInfo = DB_Main.getUserInfoById(visitedUserId);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            if (ownerUserInfo == null) {
               D.adminLog("Could not find player {" + visitedUserId + "} Failed to visit {" + privateInstanceAreakey + "}!", D.ADMIN_LOG_TYPE.Visit);
               InvokeServerRpc(MasterServer_DenyUserVisit, visitorUserId);
               return;
            }

            // Assign location bundle data
            bool hasAreaKeyOverride = privateInstanceAreakey.Length > 0;
            UserLocationBundle location = new UserLocationBundle();
            location.userId = ownerUserInfo.userId;
            location.serverPort = ServerNetworkingManager.self.server.networkedPort.Value;
            location.areaKey = privateInstanceAreakey;
            location.instanceId = -1;
            location.voyageGroupId = -1;
            location.localPositionX = ownerUserInfo.localPos.x;
            location.localPositionY = ownerUserInfo.localPos.y;

            // Override local position if the spawn location is found
            if (location.areaKey.Length > 0) {
               AreaManager.self.tryGetCustomMapManager(location.areaKey, out CustomMapManager customMapManager);
               if (customMapManager != null) {
                  string baseMapKey = AreaManager.self.getAreaName(customMapManager.getBaseMapId(ownerUserInfo));
                  Vector2 newArea2 = SpawnManager.self.getLocalPosition(baseMapKey, spawnTarget, true);
                  location.localPositionX = newArea2.x;
                  location.localPositionY = newArea2.y;
               }
            }

            InvokeServerRpc(MasterServer_ReturnUserPrivateLocationToVisit, visitorUserId, visitedUserId, location);
         });
      });
   }

   [ClientRPC]
   public void Server_FindUserLocationToVisit (int visitorUserId, int visitedUserId, string areaKeyOverride, string spawnTarget, Direction facing) {
      NetEntity ownerEntity = EntityManager.self.getEntity(visitedUserId);

      if (ownerEntity == null) {
         D.adminLog("Could not find player {" + visitedUserId + "} Failed to visit {" + areaKeyOverride + "}!", D.ADMIN_LOG_TYPE.Visit);
         InvokeServerRpc(MasterServer_DenyUserVisit, visitorUserId);
         return;
      }

      // Deny visit if the area is not a custom map
      if (!CustomMapManager.isUserSpecificAreaKey(ownerEntity.areaKey)) {
         D.adminLog("Invalid Private Area {" + visitedUserId + "} Failed to visit {" + areaKeyOverride + "}!", D.ADMIN_LOG_TYPE.Visit);
         InvokeServerRpc(MasterServer_DenyUserVisit, visitorUserId);
         return;
      }

      // Assign location bundle data
      bool hasAreaKeyOverride = areaKeyOverride.Length > 0;
      UserLocationBundle location = new UserLocationBundle();
      location.userId = ownerEntity.userId;
      location.serverPort = ServerNetworkingManager.self.server.networkedPort.Value;
      location.areaKey = hasAreaKeyOverride ? areaKeyOverride : ownerEntity.areaKey;
      location.instanceId = hasAreaKeyOverride ? 0 : ownerEntity.instanceId;
      location.voyageGroupId = ownerEntity.voyageGroupId;
      location.localPositionX = ownerEntity.transform.localPosition.x;
      location.localPositionY = ownerEntity.transform.localPosition.y;

      // Override local position if the spawn location is found
      if (location.areaKey.Length > 0) {
         AreaManager.self.tryGetCustomMapManager(location.areaKey, out CustomMapManager customMapManager);
         if (customMapManager != null) {
            string baseMapKey = AreaManager.self.getAreaName(customMapManager.getBaseMapId(ownerEntity));
            Vector2 newArea2 = SpawnManager.self.getLocalPosition(baseMapKey, spawnTarget, true);
            location.localPositionX = newArea2.x;
            location.localPositionY = newArea2.y;
         }
      }

      if (location.areaKey.Length > 0 && location.areaKey != CustomHouseManager.GROUP_AREA_KEY && location.areaKey != CustomFarmManager.GROUP_AREA_KEY) {
         D.adminLog("Override Location Bundle: {" + (hasAreaKeyOverride ? "AreaOverride: " + areaKeyOverride : "False:None") + ")} : {" + location.areaKey + "} {" + ownerEntity.instanceId + "}", D.ADMIN_LOG_TYPE.Visit);
         InvokeServerRpc(MasterServer_ReturnUserLocationToVisit, visitorUserId, location);
      } else {
         D.adminLog("Could not find valid location key for {" + visitedUserId + "} Failed to visit {" + location.userId + "} Area Key or instance id is INVALID {" + location.areaKey + "}{" + location.instanceId + "}!", D.ADMIN_LOG_TYPE.Visit);
         InvokeServerRpc(MasterServer_DenyUserVisit, visitorUserId);
      }
   }

   [ServerRPC]
   public void MasterServer_ReturnUserPrivateLocationToVisit (int visitorUserId, int visitedUserId, UserLocationBundle location) {
      NetworkedServer visitorsServer = ServerNetworkingManager.self.getServerContainingUser(visitorUserId);
      NetworkedServer targetServer = ServerNetworkingManager.self.server;

      if (location.areaKey.Contains(CustomFarmManager.GROUP_AREA_KEY)) {
         targetServer = ServerNetworkingManager.self.getServerContainingPrivateFarmInstance(visitedUserId);
      } else if (location.areaKey.Contains(CustomHouseManager.GROUP_AREA_KEY)) {
         targetServer = ServerNetworkingManager.self.getServerContainingPrivateHouseInstance(visitedUserId);
      } else {
         targetServer = null;
      }

      if (targetServer != null && visitorsServer != null) {
         D.adminLog("Found the target server to grant the visit command to! {" + visitorUserId + "} {" + location.userId + "} {" + location.areaKey + "}", D.ADMIN_LOG_TYPE.Visit);
         visitorsServer.InvokeClientRpcOnOwner(Server_ReturnUserPrivateLocationToVisit, visitorUserId, visitedUserId, location);
      } else {
         D.adminLog("Could not find the target server to grant the visit command to! {" + visitorUserId + "} {" + location.userId + "} {" + location.areaKey + "}", D.ADMIN_LOG_TYPE.Visit);
         InvokeServerRpc(MasterServer_DenyUserVisit, visitorUserId);
      }
   }

   [ServerRPC]
   public void MasterServer_ReturnUserLocationToVisit (int visitorUserId, UserLocationBundle location) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(visitorUserId);
      if (targetServer != null) {
         D.adminLog("Found the target server to grant the visit command to! {" + visitorUserId + "} {" + location.userId + "} {" + location.areaKey + "}", D.ADMIN_LOG_TYPE.Visit);
         targetServer.InvokeClientRpcOnOwner(Server_ReturnUserLocationToVisit, visitorUserId, location);
      } else {
         D.adminLog("Could not find the target server to grant the visit command to! {" + visitorUserId + "} {" + location.userId + "} {" + location.areaKey + "}", D.ADMIN_LOG_TYPE.Visit);
      }
   }

   [ClientRPC]
   public void Server_ReturnUserPrivateLocationToVisit (int visitorUserId, int visitedUserId, UserLocationBundle location) {
      NetEntity visitedUserEnitity = EntityManager.self.getEntity(visitorUserId);
      if (visitedUserEnitity != null) {
         D.adminLog("{" + visitedUserEnitity.entityName + " : " + visitorUserId + "} Found the target Entity to grant the visit command to for {" + location.userId + "} {" + location.areaKey + "}", D.ADMIN_LOG_TYPE.Visit);
         visitedUserEnitity.visitUserToPrivateLocation(visitorUserId, visitedUserId, location);
      } else {
         D.adminLog("Could not find the target Entity to grant the visit command to! {" + visitorUserId + "} {" + location.userId + "} {" + location.areaKey + "}", D.ADMIN_LOG_TYPE.Visit);
      }
   }

   [ClientRPC]
   public void Server_ReturnUserLocationToVisit (int visitorUserId, UserLocationBundle location) {
      NetEntity adminEntity = EntityManager.self.getEntity(visitorUserId);
      if (adminEntity != null) {
         D.adminLog("{" + adminEntity.entityName + " : " + visitorUserId + "} Found the target Entity to grant the visit command to for {" + location.userId + "} {" + location.areaKey + "}", D.ADMIN_LOG_TYPE.Visit);
         adminEntity.visitUserToLocation(visitorUserId, location);
      } else {
         D.adminLog("Could not find the target Entity to grant the visit command to! {" + visitorUserId + "} {" + location.userId + "} {" + location.areaKey + "}", D.ADMIN_LOG_TYPE.Visit);
      }
   }
   
   [ServerRPC]
   public void MasterServer_DenyUserVisit (int visitorUserId) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(visitorUserId);
      if (targetServer != null) {
         D.debug("Deny visit, Found the server containing {" + visitorUserId + "}");
         targetServer.InvokeClientRpcOnOwner(Server_ReturnUserDenyVisit, visitorUserId);
      } else {
         D.debug("Failed to deny visit, can no longer find the server containing {" + visitorUserId + "}");
      }
   }

   [ServerRPC]
   public void MasterServer_PrivateInstanceDoestNotExist (int visitorUserId, int visitedUserId, string visitedPrivateAreaKey, Vector2 localPosition) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(visitorUserId);
      if (targetServer != null) {
         D.debug("Deny visit, Found the server containing {" + visitorUserId + "}");
         targetServer.InvokeClientRpcOnOwner(Server_PrivateInstanceDoestNotExist, visitorUserId, visitedUserId, visitedPrivateAreaKey, localPosition);
      } else {
         D.debug("Failed to deny visit, can no longer find the server containing {" + visitorUserId + "}");
      }
   }

   [ClientRPC]
   public void Server_ReturnUserDenyVisit (int visitorUserId) {
      NetEntity adminEntity = EntityManager.self.getEntity(visitorUserId);
      if (adminEntity != null) {
         D.adminLog("Deny visit, Found entity {" + visitorUserId + "}", D.ADMIN_LOG_TYPE.Visit);
         adminEntity.denyUserVisit(visitorUserId);
      } else {
         D.adminLog("Cant deny visit, cant find entity any more {" + visitorUserId + "}", D.ADMIN_LOG_TYPE.Visit);
      }
   }

   [ClientRPC]
   public void Server_PrivateInstanceDoestNotExist (int visitorUserId, int visitedUserId, string visitedPrivateAreakey, Vector2 localPosition) {
      NetEntity adminEntity = EntityManager.self.getEntity(visitorUserId);
      if (adminEntity != null) {
         D.adminLog("Cannot visit, Cannot find Instance! Generating new one. Found entity {" + visitorUserId + "}", D.ADMIN_LOG_TYPE.Visit);
         adminEntity.privateInstanceDoestNotExist(visitorUserId, visitedPrivateAreakey, localPosition);
      } else {
         D.adminLog("Cant deny visit, cant find entity any more {" + visitorUserId + "}", D.ADMIN_LOG_TYPE.Visit);
      }
   }
   
   #endregion

   [ServerRPC]
   public void MasterServer_SendGlobalMessage (int chatId, string message, long timestamp, string senderName, int senderUserId, string guildIconDataString, string guildName, bool isSenderMuted, bool isSenderAdmin) {
      InvokeClientRpcOnEveryone(Server_ReceiveGlobalChatMessage, chatId, message, timestamp, senderName, senderUserId, guildIconDataString, guildName, isSenderMuted, isSenderAdmin);
   }

   [ClientRPC]
   public void Server_ReceiveGlobalChatMessage (int chatId, string message, long timestamp, string senderName, int senderUserId, string guildIconDataString, string guildName, bool isSenderMuted, bool isSenderAdmin) {
      // Send the chat message to all users connected to this server
      foreach (NetEntity netEntity in MyNetworkManager.getPlayers()) {
         netEntity.Target_ReceiveGlobalChat(chatId, message, timestamp, senderName, senderUserId, guildIconDataString, guildName, isSenderMuted, isSenderAdmin);
      }
   }

   [ServerRPC]
   public void MasterServer_SendGuildChatMessage (int guildId, int chatId, string message, long timestamp, string senderName, int senderUserId, string guildIconDataString, string guildName, bool isSenderMuted) {
      InvokeClientRpcOnEveryone(Server_ReceiveGuildChatMessage, guildId, chatId, message, timestamp, senderName, senderUserId, guildIconDataString, guildName, isSenderMuted);
   }

   [ClientRPC]
   public void Server_ReceiveGuildChatMessage (int guildId, int chatId, string message, long timestamp, string senderName, int senderUserId, string guildIconDataString, string guildName, bool isSenderMuted) {
      // Send the chat message to all guild members connected to this server
      foreach (NetEntity player in MyNetworkManager.getPlayers()) {
         if (player.guildId == guildId) {
            player.Target_ReceiveSpecialChat(player.connectionToClient, chatId, message, senderName, player.entityName, timestamp, ChatInfo.Type.Guild, GuildIconData.guildIconDataFromString(guildIconDataString), guildName, senderUserId, isSenderMuted);
         }
      }
   }

   [ServerRPC]
   public void MasterServer_SendSpecialChatMessage (int userId, int chatId, ChatInfo.Type messageType, string message, long timestamp, string senderName, string receiverName, int senderUserId, string guildIconDataString, string guildName, bool isSenderMuted) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(userId);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_ReceiveSpecialChatMessage, userId, chatId, messageType, message, timestamp, senderName, receiverName, senderUserId, guildIconDataString, guildName, isSenderMuted);
      } else {
         handleChatMessageDeliveryError(senderUserId, messageType, message);
      }
   }

   [ClientRPC]
   public void Server_ReceiveSpecialChatMessage (int userId, int chatId, ChatInfo.Type messageType, string message, long timestamp, string senderName, string receiverName, int senderUserId, string guildIconDataString, string guildName, bool isSenderMuted) {
      NetEntity player = EntityManager.self.getEntity(userId);
      if (player != null) {
         player.Target_ReceiveSpecialChat(player.connectionToClient, chatId, message, senderName, receiverName, timestamp, messageType, GuildIconData.guildIconDataFromString(guildIconDataString), guildName, senderUserId, isSenderMuted);
      } else {
         handleChatMessageDeliveryError(senderUserId, messageType, message);
      }
   }

   private void handleChatMessageDeliveryError (int senderUserId, ChatInfo.Type messageType, string originalMessage) {
      if (messageType == ChatInfo.Type.Whisper) {
         // For whispers, send a notification to the sender when the message failed to be delivered   
         ChatInfo chatInfo = new ChatInfo(0, "Could not find the recipient", DateTime.UtcNow, ChatInfo.Type.Error);
         chatInfo.recipient = "";
         ServerNetworkingManager.self.sendSpecialChatMessage(senderUserId, chatInfo);
      }
   }

   [ServerRPC]
   public void MasterServer_CensorGlobalMessagesFromUser (int userId) {
      InvokeClientRpcOnEveryone(Server_CensorGlobalMessagesFromUser, userId);
   }

   [ClientRPC]
   public void Server_CensorGlobalMessagesFromUser (int userId) {
      foreach (NetEntity netEntity in MyNetworkManager.getPlayers()) {
         netEntity.Target_CensorGlobalMessagesFromUser(userId);
      }
   }

   [ServerRPC]
   public void MasterServer_SendConfirmationMessage (ConfirmMessage.Type confirmType, int userId, string customMessage) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(userId);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_ReceiveConfirmationMessage, confirmType, userId, customMessage);
      }
   }

   [ClientRPC]
   public void Server_ReceiveConfirmationMessage (ConfirmMessage.Type confirmType, int userId, string customMessage) {
      NetEntity player = EntityManager.self.getEntity(userId);
      if (player != null) {
         ServerMessageManager.sendConfirmation(confirmType, player, customMessage);
      }
   }

   [ServerRPC]
   public void MasterServer_SendConfirmationMessageToGuild (ConfirmMessage.Type confirmType, int guildId, string customMessage) {
      InvokeClientRpcOnEveryone(Server_ReceiveConfirmationMessageForGuild, confirmType, guildId, customMessage);
   }

   [ClientRPC]
   public void Server_ReceiveConfirmationMessageForGuild (ConfirmMessage.Type confirmType, int guildId, string customMessage) {
      // Send the confirmation to all guild members connected to this server
      foreach (NetEntity player in MyNetworkManager.getPlayers()) {
         if (player.guildId == guildId) {
            ServerMessageManager.sendConfirmation(confirmType, player, customMessage);
         }
      }
   }

   [ServerRPC]
   public void MasterServer_SendConfirmationMessageToGroup (ConfirmMessage.Type confirmType, int groupId, string customMessage) {
      if (groupId != -1) {
         InvokeClientRpcOnEveryone(Server_ReceiveConfirmationMessageForGroup, confirmType, groupId, customMessage);
      }
   }

   [ClientRPC]
   public void Server_ReceiveConfirmationMessageForGroup (ConfirmMessage.Type confirmType, int groupId, string customMessage) {
      // Send the confirmation to all group members connected to this server
      foreach (NetEntity player in MyNetworkManager.getPlayers()) {
         if (player.voyageGroupId == groupId) {
            ServerMessageManager.sendConfirmation(confirmType, player, customMessage);
         }
      }
   }

   [ServerRPC]
   public void MasterServer_UpdateGuildMemberPermissions (int userId, int guildRankPriority, int guildPermissions) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(userId);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_UpdateGuildMemberPermissions, userId, guildRankPriority, guildPermissions);
      }
   }

   [ClientRPC]
   public void Server_UpdateGuildMemberPermissions (int userId, int guildRankPriority, int guildPermissions) {
      NetEntity player = EntityManager.self.getEntity(userId);
      if (player != null) {
         player.guildRankPriority = guildRankPriority;
         player.guildPermissions = guildPermissions;
      }
   }

   [ServerRPC]
   public void MasterServer_ReplaceGuildMembersRankPriority (int guildId, int originalRankPriority, int newRankPriority, int newPermissions) {
      InvokeClientRpcOnEveryone(Server_ReplaceGuildMembersRankPriority, guildId, originalRankPriority, newRankPriority, newPermissions);
   }

   [ClientRPC]
   public void Server_ReplaceGuildMembersRankPriority (int guildId, int originalRankPriority, int newRankPriority, int newPermissions) {
      foreach (NetEntity player in MyNetworkManager.getPlayers()) {
         if (player.guildId == guildId && player.guildRankPriority == originalRankPriority) {
            player.guildRankPriority = newRankPriority;
            player.guildPermissions = newPermissions;
         }
      }
   }

   [ServerRPC]
   public void MasterServer_UpdateUserGuildId (int userId, int newGuildId) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(userId);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_UpdateUserGuildId, userId, newGuildId);
      }
   }

   [ClientRPC]
   public void Server_UpdateUserGuildId (int userId, int newGuildId) {
      NetEntity player = EntityManager.self.getEntity(userId);
      if (player != null) {
         player.guildId = newGuildId;
      }
   }

   [ServerRPC]
   public void MasterServer_SendGroupInvitationNotification (int groupId, int inviterUserId, string inviterName, int inviteeUserId) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(inviteeUserId);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_ReceiveGroupInvitationNotification, groupId, inviterUserId, inviterName, inviteeUserId);
      }
   }

   [ClientRPC]
   public void Server_ReceiveGroupInvitationNotification (int groupId, int inviterUserId, string inviterName, int inviteeUserId) {
      NetEntity player = EntityManager.self.getEntity(inviteeUserId);
      if (player != null) {
         player.Target_ReceiveGroupInvitationNotification(player.connectionToClient, groupId, inviterName);
      }
   }

   [ServerRPC]
   public void MasterServer_CreateVoyageInstanceInServer (int serverPort, int voyageId, Voyage parameters) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServer(serverPort);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_ReceiveVoyageInstanceCreation, voyageId, parameters);
      }
   }

   [ClientRPC]
   public void Server_ReceiveVoyageInstanceCreation (int voyageId, Voyage parameters) {
      VoyageManager.self.createVoyageInstance(voyageId, parameters);
   }

   [ServerRPC]
   public void MasterServer_RequestVoyageInstanceCreation (Voyage parameters) {
      VoyageManager.self.requestVoyageInstanceCreation(parameters);
   }

   [ServerRPC]
   public void MasterServer_SendVoyageGroupCompositionToMembers (int groupId) {
      if (!VoyageGroupManager.self.tryGetGroupById(groupId, out VoyageGroupInfo voyageGroup)) {
         return;
      }

      // Find the servers the group members are connected to
      HashSet<NetworkedServer> targetServers = new HashSet<NetworkedServer>();
      foreach (int userId in voyageGroup.members) {
         NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(userId);
         if (targetServer != null) {
            targetServers.Add(targetServer);
         }
      }

      foreach (NetworkedServer server in targetServers) {
         server.InvokeClientRpcOnOwner(Server_SendVoyageGroupCompositionToMembers, groupId);
      }
   }

   [ClientRPC]
   public void Server_SendVoyageGroupCompositionToMembers (int groupId) {
      VoyageGroupManager.self.sendGroupCompositionToMembers(groupId);
   }

   [ServerRPC]
   public void MasterServer_SendMemberPartialUpdateToGroup (int groupId, int userId, string userName, int XP, string areaKey) {
      if (!VoyageGroupManager.self.tryGetGroupById(groupId, out VoyageGroupInfo voyageGroup)) {
         return;
      }

      // Find the servers the group members are connected to
      foreach (int memberUserId in voyageGroup.members) {
         NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(memberUserId);
         if (targetServer != null) {
            targetServer.InvokeClientRpcOnOwner(Server_SendMemberPartialUpdateToGroupMember, memberUserId, userId, userName, XP, areaKey);
         }
      }
   }

   [ClientRPC]
   public void Server_SendMemberPartialUpdateToGroupMember (int targetUserId, int userId, string userName, int XP, string areaKey) {
      NetEntity player = EntityManager.self.getEntity(targetUserId);
      if (player != null) {
         player.rpc.Target_ReceiveVoyageGroupMemberPartialUpdate(player.connectionToClient, userId, userName, XP, areaKey);
      }
   }

   [ServerRPC]
   public int MasterServer_GetNewVoyageGroupId () {
      return VoyageGroupManager.self.getNewGroupId();
   }

   [ServerRPC]
   public int MasterServer_GetNewVoyageId () {
      return VoyageManager.self.getNewVoyageId();
   }

   [ServerRPC]
   public void MasterServer_FindUserLocationForAdminGoTo (int adminUserId, int userId) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(userId);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_FindUserLocationForAdminGoTo, adminUserId, userId);
      }
   }

   [ClientRPC]
   public void Server_FindUserLocationForAdminGoTo (int adminUserId, int userId) {
      NetEntity player = EntityManager.self.getEntity(userId);
      if (player != null) {
         UserLocationBundle location = new UserLocationBundle();
         location.userId = player.userId;
         location.serverPort = ServerNetworkingManager.self.server.networkedPort.Value;
         location.areaKey = player.areaKey;
         location.instanceId = player.instanceId;
         location.localPositionX = player.transform.localPosition.x;
         location.localPositionY = player.transform.localPosition.y;
         location.voyageGroupId = player.voyageGroupId;
         InvokeServerRpc(MasterServer_ReturnUserLocationForAdminGoTo, adminUserId, location);
      }
   }

   [ServerRPC]
   public void MasterServer_ReturnUserLocationForAdminGoTo (int adminUserId, UserLocationBundle location) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(adminUserId);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_ReturnUserLocationForAdminGoTo, adminUserId, location);
      }
   }

   [ClientRPC]
   public void Server_ReturnUserLocationForAdminGoTo (int adminUserId, UserLocationBundle location) {
      NetEntity adminEntity = EntityManager.self.getEntity(adminUserId);
      if (adminEntity != null) {
         adminEntity.admin.forceWarpToLocation(adminUserId, location);
      }
   }

   [ServerRPC]
   public void MasterServer_GetUsersInInstanceForAdminVoyagePanel (int voyageId, int instanceId, int callerUserId) {
      if (ServerNetworkingManager.self.tryGetServerHostingVoyage(voyageId, out NetworkedServer targetServer)) {
         targetServer.InvokeClientRpcOnOwner(Server_GetUsersInInstanceForAdminVoyagePanel, voyageId, instanceId, callerUserId);
      }
   }

   [ClientRPC]
   public void Server_GetUsersInInstanceForAdminVoyagePanel (int voyageId, int instanceId, int callerUserId) {
      List<int> userIdList;

      Instance instance = InstanceManager.self.getInstance(instanceId);
      if (instance != null) {
         userIdList = instance.getPlayerUserIds();
      } else {
         userIdList = new List<int>();
      }

      InvokeServerRpc(MasterServer_ReturnUsersInInstanceForAdminVoyagePanel, callerUserId, userIdList.ToArray());
   }

   [ServerRPC]
   public void MasterServer_ReturnUsersInInstanceForAdminVoyagePanel (int callerUserId, int[] userIdArray) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(callerUserId);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_ReturnUsersInInstanceForAdminVoyagePanel, callerUserId, userIdArray);
      }
   }

   [ClientRPC]
   public void Server_ReturnUsersInInstanceForAdminVoyagePanel (int callerUserId, int[] userIdArray) {
      NetEntity callerPlayer = EntityManager.self.getEntity(callerUserId);
      if (callerPlayer != null) {
         callerPlayer.rpc.returnUsersInInstanceForAdminVoyagePanel(userIdArray);
      }
   }

   [ServerRPC]
   public void MasterServer_RegisterUserInTreasureSite (int userId, int voyageId, int instanceId) {
      if (ServerNetworkingManager.self.tryGetServerHostingVoyage(voyageId, out NetworkedServer voyageServer)) {
         voyageServer.InvokeClientRpcOnOwner(Server_RegisterUserInTreasureSite, userId, voyageId, instanceId);
      }
   }

   [ClientRPC]
   public void Server_RegisterUserInTreasureSite (int userId, int voyageId, int instanceId) {
      VoyageManager.self.registerUserInTreasureSite(userId, voyageId, instanceId);
   }

   [ServerRPC]
   public void MasterServer_JoinPvpGame (int voyageId, int userId, string userName, PvpTeamType team) {
      if (ServerNetworkingManager.self.tryGetServerHostingVoyage(voyageId, out NetworkedServer targetServer)) {
         targetServer.InvokeClientRpcOnOwner(Server_JoinPvpGame, voyageId, userId, userName, team);
      }
   }

   [ClientRPC]
   public void Server_JoinPvpGame (int voyageId, int userId, string userName, PvpTeamType team) {
      PvpManager.self.joinPvpGame(voyageId, userId, userName, team);
   }

   [ServerRPC]
   public void MasterServer_ClearPowerupsForUser (int userId) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(userId);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_ClearPowerupsForUser, userId);
      }
   }

   [ClientRPC]
   public void Server_ClearPowerupsForUser (int userId) {
      // If the user is not in this server, don't search again in others to prevent an infinite loop
      if (EntityManager.self.getEntity(userId) == null) {
         return;
      }

      PowerupManager.self.clearPowerupsForUser(userId);
   }

   [ServerRPC]
   public void MasterServer_WarpUser (int userId, int voyageId, string areaKey, Direction newFacingDirection, string spawn) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(userId);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_WarpUser, userId, voyageId, areaKey, newFacingDirection, spawn);
      }
   }

   [ClientRPC]
   public void Server_WarpUser (int userId, int voyageId, string areaKey, Direction newFacingDirection, string spawn) {
      NetEntity targetEntity = EntityManager.self.getEntity(userId);
      if (targetEntity != null) {
         targetEntity.spawnInNewMap(voyageId, areaKey, spawn, newFacingDirection);
      }
   }

   [ServerRPC]
   public void MasterServer_DisplayNoticeScreenWithError (int userId, ErrorMessage.Type errorType, string message) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(userId);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_DisplayNoticeScreenWithError, userId, errorType, message);
      }
   }

   [ClientRPC]
   public void Server_DisplayNoticeScreenWithError (int userId, ErrorMessage.Type errorType, string message) {
      NetEntity targetEntity = EntityManager.self.getEntity(userId);
      if (targetEntity != null) {
         ServerMessageManager.sendError(errorType, targetEntity, message);
      }
   }

   [ServerRPC]
   public void MasterServer_Log (int originServerPort, string message) {
      D.log($"{originServerPort}: {message}");
   }

   [ServerRPC]
   public int MasterServer_RedirectUserToBestServer (int userId, string userName, int voyageId, bool isSinglePlayer, string destinationAreaKey, int currentServerPort, string currentAddress, string currentAreaKey, int targetInstanceId, int targetServerPort) {
      return RedirectionManager.self.redirectUserToBestServer(userId, userName, voyageId, isSinglePlayer, destinationAreaKey, currentServerPort, currentAddress, currentAreaKey, targetInstanceId, targetServerPort);
   }

   [ServerRPC]
   public void MasterServer_OnUserConnectsToServer (int userId) {
      VoyageGroupManager.self.onUserConnectsToServer(userId);
   }

   [ServerRPC]
   public void MasterServer_OnUserDisconnectsFromServer (int userId) {
      VoyageGroupManager.self.onUserDisconnectsFromServer(userId);
   }

   [ServerRPC]
   public void MasterServer_ForceDisconnectAllNonAdminUsers (int adminUserId, string message) {
      InvokeClientRpcOnEveryone(Server_ForceDisconnectAllNonAdminUsers, adminUserId, message);
   }

   [ClientRPC]
   public void Server_ForceDisconnectAllNonAdminUsers (int adminUserId, string message) {
      int playerCount = 0;
      foreach (NetEntity netEntity in MyNetworkManager.getPlayers()) {
         if (netEntity != null && !netEntity.isAdmin()) {
            netEntity.connectionToClient.Send(new ErrorMessage(ErrorMessage.Type.Kicked, $"You were disconnected.\n\n Reason: {message}"));
            playerCount++;
         }
      }
      ServerNetworkingManager.self.sendConfirmationMessage(ConfirmMessage.Type.General, adminUserId, $"Server {ServerNetworkingManager.self.server.networkedPort.Value} kicked {playerCount} players offline.");
   }

   [ServerRPC]
   public void MasterServer_SummonUser (int targetUserId, UserLocationBundle adminLocation) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(targetUserId);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_SummonUser, targetUserId, adminLocation);
      }
   }

   [ClientRPC]
   public void Server_SummonUser (int targetUserId, UserLocationBundle adminLocation) {
      NetEntity targetEntity = EntityManager.self.getEntity(targetUserId);
      if (targetEntity != null) {
         targetEntity.admin.forceWarpToLocation(adminLocation.userId, adminLocation);
      }
   }

   [ServerRPC]
   public void MasterServer_ForceSinglePlayerForUser (int userId) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(userId);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_ForceSinglePlayerForUser, userId);
      }
   }

   [ClientRPC]
   public void Server_ForceSinglePlayerForUser (int userId) {
      NetEntity targetEntity = EntityManager.self.getEntity(userId);
      if (targetEntity != null) {
         targetEntity.admin.forceSinglePlayer();
      }
   }

   [ServerRPC]
   public void MasterServer_MutePlayer (int userId, bool isStealth, long expiresAt) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(userId);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_MutePlayer, userId, isStealth, expiresAt);
      }
   }

   [ClientRPC]
   public void Server_MutePlayer (int userId, bool isStealth, long expiresAt) {
      NetEntity targetEntity = EntityManager.self.getEntity(userId);
      if (targetEntity != null) {
         targetEntity.admin.mutePlayer(isStealth, expiresAt);
      }
   }

   [ServerRPC]
   public void MasterServer_UnMutePlayer (int userId) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(userId);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_UnMutePlayer, userId);
      }
   }

   [ClientRPC]
   public void Server_UnMutePlayer (int userId) {
      NetEntity targetEntity = EntityManager.self.getEntity(userId);
      if (targetEntity != null) {
         targetEntity.admin.unMutePlayer();
      }
   }

   [ServerRPC]
   public void MasterServer_KickPlayer (int userId) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(userId);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_KickPlayer, userId);
      }
   }

   [ClientRPC]
   public void Server_KickPlayer (int userId) {
      NetEntity targetEntity = EntityManager.self.getEntity(userId);
      if (targetEntity != null) {
         targetEntity.admin.kickPlayer();
      }
   }

   [ServerRPC]
   public void MasterServer_BanPlayer (int userId, bool isPermanent, long expiresAt) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(userId);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_BanPlayer, userId, isPermanent, expiresAt);
      }
   }

   [ClientRPC]
   public void Server_BanPlayer (int userId, bool isPermanent, long expiresAt) {
      NetEntity targetEntity = EntityManager.self.getEntity(userId);
      if (targetEntity != null) {
         targetEntity.admin.banPlayer(isPermanent, expiresAt);
      }
   }

   [ServerRPC]
   public void MasterServer_ChangeUserName (int userId, string oldName, string newName) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(userId);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_ChangeUserName, userId, oldName, newName);
      }
   }

   [ClientRPC]
   public void Server_ChangeUserName (int userId, string oldName, string newName) {
      NetEntity targetEntity = EntityManager.self.getEntity(userId);
      if (targetEntity != null) {
         targetEntity.entityName = newName;
         targetEntity.admin.Rpc_ReceiveNewName(userId, oldName, newName);
      }
   }

   [ServerRPC]
   public void MasterServer_RecreateLeagueInstanceAndAddUserToGroup (int voyageId, int groupId, int userId, string userName) {
      if (ServerNetworkingManager.self.tryGetServerHostingVoyage(voyageId, out NetworkedServer targetServer)) {
         targetServer.InvokeClientRpcOnOwner(Server_RecreateLeagueInstanceAndAddUserToGroup, groupId, userId, userName);
      }
   }

   [ClientRPC]
   public void Server_RecreateLeagueInstanceAndAddUserToGroup (int groupId, int userId, string userName) {
      VoyageManager.self.recreateLeagueInstanceAndAddUserToGroup(groupId, userId, userName);
   }

   public int getAverageFPS () {
      // We need at least 2 samples to calculate it
      if (_pastFrameTimes.Count < 2) return 1;

      return (int) (_pastFrameTimes.Count / (_pastFrameTimes.Last.Value - _pastFrameTimes.First.Value));
   }

   #region Log Retrieval

   public void requestServerLog (AdminManager man, ulong serverNetworkId) {
      // Note down the manager that requested this to return to him later
      _serverLogsRequesters[man.netId] = man;
      InvokeServerRpc(MasterServer_RequestServerLog, serverNetworkId, man.netId);
   }

   [ServerRPC]
   public void MasterServer_RequestServerLog (ulong serverNetworkId, uint forNetId) {
      // If log is needed for the master server, we can form it immediately
      if (serverNetworkId == ServerNetworkingManager.self.server.NetworkId) {
         requestServerLog(serverNetworkId, forNetId);
         return;
      }

      // Otherwise find the server and send a request for the log
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         if (server.NetworkId == serverNetworkId) {
            InvokeClientRpcOnClient(Server_RequestServerLog, server.OwnerClientId, serverNetworkId, forNetId);
            return;
         }
      }

      D.warning("Could not find server " + serverNetworkId + " to retrieve logs");
   }

   [ClientRPC]
   public void Server_RequestServerLog (ulong serverNetworkId, uint forNetId) {
      requestServerLog(serverNetworkId, forNetId);
   }

   private void requestServerLog (ulong serverNetworkId, uint forNetId) {
      // Transform into a byte array
      byte[] data = Encoding.ASCII.GetBytes(D.getLogString());

      // Remove the beginning of the log if it is too large
      int maxServerLogSize = D.MAX_MASTER_SERVER_LOG_CHUNK_SIZE * D.MAX_MASTER_SERVER_LOG_CHUNK_COUNT;
      if (data.Length > maxServerLogSize) {
         data = data.RangeSubset(data.Length - maxServerLogSize, maxServerLogSize);
      }

      // Split the log into chunks smaller than the RPC size limit per message
      int lastIndex = 0;
      while (lastIndex < data.Length) {
         byte[] chunk = data.RangeSubset(lastIndex, Mathf.Min(D.MAX_MASTER_SERVER_LOG_CHUNK_SIZE, data.Length - lastIndex));
         lastIndex += D.MAX_MASTER_SERVER_LOG_CHUNK_SIZE;

         // If we are not the master server, send over to it first
         if (ServerNetworkingManager.self.server.isMasterServer()) {
            receiveServerLogDataMasterServer(chunk, lastIndex >= data.Length, serverNetworkId, forNetId);
         } else {
            InvokeServerRpc(MasterServer_ReceiveServerLogData, chunk, lastIndex >= data.Length, serverNetworkId, forNetId);
         }
      }
   }

   [ServerRPC(RequireOwnership = false)]
   public void MasterServer_ReceiveServerLogData (byte[] serverLogData, bool last, ulong serverNetworkId, uint forNetId) {
      receiveServerLogDataMasterServer(serverLogData, last, serverNetworkId, forNetId);
   }

   private void receiveServerLogDataMasterServer (byte[] serverLogData, bool last, ulong serverNetworkId, uint forNetId) {
      InvokeClientRpcOnOwner(Server_ReceiveServerLog, serverLogData, last, serverNetworkId, forNetId);
   }

   [ClientRPC]
   public void Server_ReceiveServerLog (byte[] serverLogData, bool last, ulong serverNetworkId, uint forNetId) {
      // We arrived at the server, which received log request from the client
      // Figure out which client requested it and send the partial log data to him
      if (_serverLogsRequesters.TryGetValue(forNetId, out AdminManager man)) {
         if (man != null) {
            man.Target_ReceivePartialRemoteServerLogBytes(serverLogData, serverNetworkId, last);
         }
      }
   }

   #endregion

   #region Network Overview Reports

   public static ServerOverview createServerOverview () {
      // Create server overview about the CURRENT OWNER server, not his particular entity
      ServerOverview ow = new ServerOverview {
         serverNetworkId = ServerNetworkingManager.self.server.NetworkId,
         port = ServerNetworkingManager.self.server.networkedPort.Value,
         machineName = Environment.MachineName,
         processName = System.Diagnostics.Process.GetCurrentProcess()?.ProcessName,
         fps = ServerNetworkingManager.self.server.getAverageFPS(),
         instances = InstanceManager.self.createOverviewForAllInstances(),
         uptime = NetworkTime.time
      };

      return ow;
   }

   public void requestServerOverviews (RPCManager man) {
      // Note down the manager that requested this, so we can find it later
      _networkOverviewRequesters[man.netId] = man;

      // Ask the master server to send over all server overviews
      InvokeServerRpc(MasterServer_RequestServerOverviews, man.netId);
   }

   [ServerRPC]
   public void MasterServer_RequestServerOverviews (uint forNetId) {
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         // Ask for an overview for every server that is not the master, we are on the master right now
         if (!server.isMasterServer()) {
            InvokeClientRpcOnClient(Server_RequestServerOverview, server.OwnerClientId, forNetId, DateTime.UtcNow.Ticks);
         }
      }

      // We want to create the overview for master server itself as well
      long sentAt = DateTime.UtcNow.Ticks;
      receiveServerOverview(createServerOverview(), forNetId, sentAt);
   }

   [ClientRPC]
   public void Server_RequestServerOverview (uint forNetId, long sentAt) {
      // This specific instance should be the remote-server that originally requested the overview
      // ServerNetworkingManager.self.server should be the owner of this server that we want information about
      ServerOverview ow = createServerOverview();
      InvokeServerRpc(MasterServer_ReceiveServerOverview, ow, forNetId, sentAt);
   }

   [ServerRPC(RequireOwnership = false)]
   public void MasterServer_ReceiveServerOverview (ServerOverview serverOverview, uint forNetId, long sentAt) {
      receiveServerOverview(serverOverview, forNetId, sentAt);
   }

   private void receiveServerOverview (ServerOverview serverOverview, uint forNetId, long sentAt) {
      // Calculate how much time it took from sending overview request, to receiving the overview
      serverOverview.toMasterRTT = (DateTime.UtcNow.Ticks - sentAt) / TimeSpan.TicksPerMillisecond;
      InvokeClientRpcOnOwner(Server_ReceiveServerOverview, serverOverview, forNetId);
   }

   [ClientRPC]
   public void Server_ReceiveServerOverview (ServerOverview serverOverview, uint forNetId) {
      // If the requester is still there, pass on the overview
      if (_networkOverviewRequesters.TryGetValue(forNetId, out RPCManager man)) {
         if (man != null) {
            man.Target_ReceiveServerOverview(serverOverview);
         }
      }
   }

   #endregion

   #region Private Variables

   // Which users are requesting network overview
   private Dictionary<uint, RPCManager> _networkOverviewRequesters = new Dictionary<uint, RPCManager>();

   // Which users are requesting server logs
   private Dictionary<uint, AdminManager> _serverLogsRequesters = new Dictionary<uint, AdminManager>();

   // List of frame times over the last 60 frames for FPS calculation
   private LinkedList<float> _pastFrameTimes = new LinkedList<float>();

   #endregion
}