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

public class NetworkedServer : NetworkedBehaviour
{
   #region Public Variables

   // The port associated with this Networked Server
   public NetworkedVar<int> networkedPort = new NetworkedVar<int>(Global.defaultNetworkedVarSettings);

   // The users connected to this server
   public NetworkedList<int> connectedUserIds = new NetworkedList<int>(Global.defaultNetworkedVarSettings);

   // The users that have been assigned to this server - there can be userIds duplicates in multiple servers
   public NetworkedDictionary<int, AssignedUserInfo> assignedUserIds = new NetworkedDictionary<int, AssignedUserInfo>(new NetworkedVarSettings { WritePermission = NetworkedVarPermission.Everyone, SendChannel = "Fragmented", SendTickrate = 0 });

   // The accounts connected to this server
   public NetworkedDictionary<int, int> connectedAccountIds = new NetworkedDictionary<int, int>(new NetworkedVarSettings { WritePermission = NetworkedVarPermission.OwnerOnly, SendChannel = "Fragmented", SendTickrate = 0 });

   // The voyage groups stored in this server
   public NetworkedDictionary<int, VoyageGroupInfo> voyageGroups = new NetworkedDictionary<int, VoyageGroupInfo>(new NetworkedVarSettings { WritePermission = NetworkedVarPermission.Everyone, SendChannel = "Fragmented", SendTickrate = 0 });

   // The voyage instances hosted in this server
   public NetworkedList<Voyage> voyages = new NetworkedList<Voyage>(Global.defaultNetworkedVarSettings);

   // The treasure site instances hosted in this server
   public NetworkedList<Voyage> treasureSites = new NetworkedList<Voyage>(Global.defaultNetworkedVarSettings);

   // Keeps track of the users claimed by this server
   public NetworkedList<int> claimedUserIds = new NetworkedList<int>(Global.defaultNetworkedVarSettings);

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
         InvokeRepeating(nameof(updateVoyageInstances), 5.5f, 1f);
      }

      // Register this server
      ServerNetworkingManager.self.servers.Add(this);
   }

   private void Update () {
      // Set the name of this object based on the port
      this.name = "Networked Server #" + this.networkedPort.Value;
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
         }
      }
   }

   private void updateVoyageInstances () {
      voyages.Clear();
      treasureSites.Clear();

      // Get the number of groups in all voyage instances
      Dictionary<int, int> allGroupCount = VoyageGroupManager.self.getGroupCountInAllVoyages();

      foreach (Instance instance in InstanceManager.self.getVoyageInstances()) {
         allGroupCount.TryGetValue(instance.voyageId, out int groupCount);
         addNewVoyageInstance(instance, groupCount);
      }

      // Get the treasure site instances that are linked to a voyage
      foreach (Instance instance in InstanceManager.self.getTreasureSiteInstancesLinkedToVoyages()) {
         allGroupCount.TryGetValue(instance.voyageId, out int groupCount);
         addNewVoyageInstance(treasureSites, instance, groupCount);
      }
   }

   public void addNewVoyageInstance (Instance instance, int groupCount) {
      addNewVoyageInstance(voyages, instance, groupCount);
   }

   public void addNewVoyageInstance (NetworkedList<Voyage> voyageList, Instance instance, int groupCount) {
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

      Voyage voyage = new Voyage(instance.voyageId, instance.id, instance.areaKey, Area.getName(instance.areaKey), instance.difficulty, instance.biome, instance.isPvP,
            instance.isLeague, instance.leagueIndex, instance.leagueRandomSeed, instance.creationDate, instance.treasureSiteCount, instance.capturedTreasureSiteCount, instance.aliveNPCEnemiesCount,
            instance.getTotalNPCEnemyCount(), groupCount, instance.getPlayerCount(), playerCountTeamA, playerCountTeamB, pvpGameMaxPlayerCount, pvpGameState);
      voyageList.Add(voyage);
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
         connectedAccountIds[accountId] = 1;
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
   public void MasterServer_SendGlobalMessage (int chatId, string message, long timestamp, string senderName, int senderUserId, string guildIconDataString, bool isSenderMuted, bool isSenderAdmin) {
      InvokeClientRpcOnEveryone(Server_ReceiveGlobalChatMessage, chatId, message, timestamp, senderName, senderUserId, guildIconDataString, isSenderMuted, isSenderAdmin);
   }

   [ClientRPC]
   public void Server_ReceiveGlobalChatMessage (int chatId, string message, long timestamp, string senderName, int senderUserId, string guildIconDataString, bool isSenderMuted, bool isSenderAdmin) {
      // Send the chat message to all users connected to this server
      foreach (NetEntity netEntity in MyNetworkManager.getPlayers()) {
         netEntity.Target_ReceiveGlobalChat(chatId, message, timestamp, senderName, senderUserId, guildIconDataString, isSenderMuted, isSenderAdmin);
      }
   }

   [ServerRPC]
   public void MasterServer_SendGuildChatMessage (int guildId, int chatId, string message, long timestamp, string senderName, int senderUserId, string guildIconDataString, bool isSenderMuted) {
      InvokeClientRpcOnEveryone(Server_ReceiveGuildChatMessage, guildId, chatId, message, timestamp, senderName, senderUserId, guildIconDataString, isSenderMuted);
   }

   [ClientRPC]
   public void Server_ReceiveGuildChatMessage (int guildId, int chatId, string message, long timestamp, string senderName, int senderUserId, string guildIconDataString, bool isSenderMuted) {
      // Send the chat message to all guild members connected to this server
      foreach (NetEntity player in MyNetworkManager.getPlayers()) {
         if (player.guildId == guildId) {
            player.Target_ReceiveSpecialChat(player.connectionToClient, chatId, message, senderName, player.entityName, timestamp, ChatInfo.Type.Guild, GuildIconData.guildIconDataFromString(guildIconDataString), senderUserId, isSenderMuted);
         }
      }
   }

   [ServerRPC]
   public void MasterServer_SendSpecialChatMessage (int userId, int chatId, ChatInfo.Type messageType, string message, long timestamp, string senderName, string receiverName, int senderUserId, string guildIconDataString, bool isSenderMuted) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(userId);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_ReceiveSpecialChatMessage, userId, chatId, messageType, message, timestamp, senderName, receiverName, senderUserId, guildIconDataString, isSenderMuted);
      } else {
         handleChatMessageDeliveryError(senderUserId, messageType, message);
      }
   }

   [ClientRPC]
   public void Server_ReceiveSpecialChatMessage (int userId, int chatId, ChatInfo.Type messageType, string message, long timestamp, string senderName, string receiverName, int senderUserId, string guildIconDataString, bool isSenderMuted) {
      NetEntity player = EntityManager.self.getEntity(userId);
      if (player != null) {
         player.Target_ReceiveSpecialChat(player.connectionToClient, chatId, message, senderName, receiverName, timestamp, messageType, GuildIconData.guildIconDataFromString(guildIconDataString), senderUserId, isSenderMuted);
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
   public void MasterServer_CreateVoyageInstanceInServer (int serverPort, int voyageId, string areaKey, bool isPvP, bool isLeague, int leagueIndex, int leagueRandomSeed, Biome.Type biome, int difficulty) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServer(serverPort);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_ReceiveVoyageInstanceCreation, voyageId, areaKey, isPvP, isLeague, leagueIndex, leagueRandomSeed, biome, difficulty);
      }
   }

   [ClientRPC]
   public void Server_ReceiveVoyageInstanceCreation (int voyageId, string areaKey, bool isPvP, bool isLeague, int leagueIndex, int leagueRandomSeed, Biome.Type biome, int difficulty) {
      VoyageManager.self.createVoyageInstance(voyageId, areaKey, isPvP, isLeague, leagueIndex, leagueRandomSeed, biome, difficulty);
   }

   [ServerRPC]
   public void MasterServer_RequestVoyageInstanceCreation (string areaKey, bool isPvP, bool isLeague, int leagueIndex, int leagueRandomSeed, Biome.Type biome, int difficulty) {
      VoyageManager.self.requestVoyageInstanceCreation(areaKey, isPvP, isLeague, leagueIndex, leagueRandomSeed, biome, difficulty);
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
         adminEntity.admin.returnUserLocationForAdminGoto(location);
      }
   }

   [ServerRPC]
   public void MasterServer_GetUsersInInstanceForAdminVoyagePanel (int voyageId, int instanceId, int callerUserId) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerHostingVoyage(voyageId);
      if (targetServer != null) {
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
      NetworkedServer voyageServer = ServerNetworkingManager.self.getServerHostingVoyage(voyageId);
      if (voyageServer != null) {
         voyageServer.InvokeClientRpcOnOwner(Server_RegisterUserInTreasureSite, userId, voyageId, instanceId);
      }
   }

   [ClientRPC]
   public void Server_RegisterUserInTreasureSite (int userId, int voyageId, int instanceId) {
      VoyageManager.self.registerUserInTreasureSite(userId, voyageId, instanceId);
   }

   [ServerRPC]
   public void MasterServer_JoinPvpGame (int voyageId, int userId, string userName, PvpTeamType team) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerHostingVoyage(voyageId);
      if (targetServer != null) {
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
   public int MasterServer_RedirectUserToBestServer (int userId, string userName, int voyageId, bool isSinglePlayer, string destinationAreaKey, int currentServerPort, string currentAddress, string currentAreaKey) {
      return RedirectionManager.self.redirectUserToBestServer(userId, userName, voyageId, isSinglePlayer, destinationAreaKey, currentServerPort, currentAddress, currentAreaKey);
   }

   [ServerRPC]
   public void MasterServer_OnUserConnectsToServer (int userId) {
      VoyageGroupManager.self.onUserConnectsToServer(userId);
   }

   [ServerRPC]
   public void MasterServer_OnUserDisconnectsFromServer (int userId) {
      VoyageGroupManager.self.onUserDisconnectsFromServer(userId);
   }

   #region Private Variables

   #endregion
}