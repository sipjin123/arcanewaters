using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using System.IO;
using Newtonsoft.Json;
using MLAPI;
using System.Linq;
using MLAPI.Messaging;
using System;
using System.Text;

public class ServerNetworkingManager : MonoBehaviour
{
   #region Public Variables

   // The server we have control of
   public NetworkedServer server;

   // The Servers we know about
   public HashSet<NetworkedServer> servers = new HashSet<NetworkedServer>();

   // Self
   public static ServerNetworkingManager self;

   // The cached logs
   public string lastMasterServerLog, lastServerLog;

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      // This is where we register any classes that we want to serialize between the Server processes
      RegisterSerializableClass<Group>();
      RegisterSerializableClass<GroupInstance>();
      RegisterSerializableClass<UserLocationBundle>();
      RegisterSerializableClass<AssignedUserInfo>();
      RegisterSerializableClass<PenaltyInfo>();
      RegisterSerializableClass<ServerOverview>();
      RegisterSerializableClass<InstanceOverview>();
      RegisterSerializableClass<BasicInstanceOverview>();

      // Log the number of connected players
      if (Util.isAutoTest()) {
         InvokeRepeating(nameof(logNumberOfConnectedClients), 0f, 5f);
      } else {
         InvokeRepeating(nameof(logNumberOfConnectedClients), 0f, 60f);
      }
   }

   public static NetworkingManager get () {
      // This wrapper function is just to help clarify the difference between the "Server" Networking Manager, and the regular Network Manager
      return NetworkingManager.Singleton;
   }

   private static void RegisterSerializableClass<T> () {
      // This is just a long-winded way of saying that we want to use Json to serialize and deserialize an object
      SerializationManager.RegisterSerializationHandlers<T>(
         (Stream stream, T objectInstance) => {
            using (PooledBitWriter writer = PooledBitWriter.Get(stream)) {
               writer.WriteStringPacked(JsonConvert.SerializeObject(objectInstance));
            }
         },
         (Stream stream) => {
            using (PooledBitReader reader = PooledBitReader.Get(stream)) {
               return JsonConvert.DeserializeObject<T>(reader.ReadStringPacked().ToString());
            }
         }
      );
   }

   private void logNumberOfConnectedClients () {
      if (server != null) {
         // If this is the master server
         if (server.isMasterServer()) {
            string newMasterServerLog = $"Total client connections: {self.servers.Sum(x => x.connectedUserIds.Count)}, Total assigned users: {self.servers.Sum(x => x.assignedUserIds.Count)}";
            if (lastMasterServerLog != newMasterServerLog) {
               D.debug(newMasterServerLog);
               lastMasterServerLog = newMasterServerLog;
            }
         }

         string newServerLog = $"My client connections: {server.connectedUserIds.Count}, My assigned users: {server.assignedUserIds.Count}";
         if (lastServerLog != newServerLog) {
            D.debug(newServerLog);
            lastServerLog = newServerLog;
         }
      }
   }

   public NetworkedServer getServerContainingUser (int userId) {
      foreach (NetworkedServer server in servers) {
         if (server.connectedUserIds.Contains(userId)) {
            return server;
         }
      }
      return null;
   }

   public bool tryGetServerContainingAnyUser (List<int> userIds, out NetworkedServer server, out int userId) {
      foreach (NetworkedServer s in servers) {
         foreach(int id in userIds) {
            if (s.connectedUserIds.Contains(id)) {
               server = s;
               userId = id;
               return true;
            }
         }
      }

      server = null;
      userId = 0;
      return false;
   }

   public NetworkedServer getServerContainingPrivateFarmInstance (int userId) {
      foreach (NetworkedServer server in servers) {
         if (server.privateFarmInstances.Contains(userId)) {
            return server;
         }
      }
      return null;
   }

   public NetworkedServer getServerContainingPrivateHouseInstance (int userId) {
      foreach (NetworkedServer server in servers) {
         if (server.privateHouseInstances.Contains(userId)) {
            return server;
         }
      }
      return null;
   }

   public NetworkedServer getServerContainingAccount (int accId) {
      foreach (NetworkedServer server in servers) {
         if (server.connectedAccountIds.ContainsKey(accId)) {
            return server;
         }
      }
      return null;
   }

   public NetworkedServer getServerContainingAssignedUser (int userId) {
      foreach (NetworkedServer server in servers) {
         if (server.assignedUserIds.ContainsKey(userId)) {
            return server;
         }
      }
      return null;
   }

   public NetworkedServer getServer (int port) {
      foreach (NetworkedServer server in servers) {
         if (server.networkedPort.Value == port) {
            return server;
         }
      }
      return null;
   }

   public NetworkedServer getFirstServerWithLeastAssignedPlayers () {
      List<NetworkedServer> bestServers = getServersWithLeastAssignedPlayers();

      if (bestServers.Count == 0) {
         return null;
      } else {
         // If many servers have the same lowest number of players, select the first port
         List<NetworkedServer> similarServers = bestServers.OrderBy(_ => _.networkedPort.Value).ToList();
         return similarServers[0];
      }
   }

   public NetworkedServer getRandomServerWithLeastAssignedPlayers () {
      List<NetworkedServer> bestServers = getServersWithLeastAssignedPlayers();

      if (bestServers.Count == 0) {
         return null;
      } else {
         NetworkedServer randomServer = bestServers.ChooseRandom();
         return randomServer;
      }
   }

   private List<NetworkedServer> getServersWithLeastAssignedPlayers () {
      int leastPlayers = int.MaxValue;
      List<NetworkedServer> bestServers = new List<NetworkedServer>();

      foreach (NetworkedServer server in servers) {
         int serverPlayerCount = server.assignedUserIds.Count;

         // If this is the master server, we will act as though it has extra players, to only use it when the other servers have many more players
         if (server.networkedPort.Value == Global.MASTER_SERVER_PORT) {
            serverPlayerCount += MASTER_SERVER_PLAYER_OFFSET;
         }

         if (serverPlayerCount == leastPlayers) {
            bestServers.Add(server);
         } else if (serverPlayerCount < leastPlayers) {
            bestServers.Clear();
            bestServers.Add(server);
            leastPlayers = serverPlayerCount;
         }
      }

      return bestServers;
   }

   public bool tryGetServerHostingGroupInstance (int groupInstanceId, out NetworkedServer hostingServer) {
      hostingServer = null;

      // Search the instance in all the servers we know about
      foreach (NetworkedServer server in servers) {
         if (server.groupInstances.TryGetValue(groupInstanceId, out GroupInstance groupInstance)) {
            hostingServer = server;
            return true;
         }
      }

      return false;
   }

   public bool isUserOnline (int userId) {
      bool isOnline = false;

      foreach (NetworkedServer server in servers) {
         if (server.connectedUserIds.Contains(userId)) {
            isOnline = true;
            break;
         }
      }

      return isOnline;
   }

   public bool isAccountOnlineInAnotherServer (int accountId) {
      foreach (NetworkedServer otherServer in servers) {
         if (otherServer.networkedPort != this.server.networkedPort && otherServer.connectedAccountIds.ContainsKey(accountId)) {
            return true;
         }
      }

      return false;
   }

   public bool isUserAssignedAnywhere (int userId) {
      foreach (NetworkedServer server in servers) {
         if (server.assignedUserIds.ContainsKey(userId)) {
            return true;
         }
      }

      return false;
   }

   public bool isUserAssignedToAnotherServer (int userId) {
      foreach (NetworkedServer server in servers) {
         if (server.networkedPort != this.server.networkedPort && server.assignedUserIds.ContainsKey(userId)) {
            return true;
         }
      }

      return false;
   }

   public void claimPlayer (int userId) {
      server.claimedUserIds[userId] = true;
   }
   
   public void releasePlayerClaim (int userId) {
      server.claimedUserIds.Remove(userId);
   }

   public void addPrivateFarmInstance (int userId) {
      if (server.privateFarmInstances.Contains(userId)) {
      } else {
         server.privateFarmInstances.Add(userId);
      }
   }
   
   public void releasePrivateFarmInstance (int userId) {
      if (server.privateFarmInstances.Contains(userId)) {
         server.privateFarmInstances.Remove(userId);
      }
   }

   public void addPrivateHouseInstance (int userId) {
      if (server.privateHouseInstances.Contains(userId)) {
      } else {
         server.privateHouseInstances.Add(userId);
      }
   }

   public void releasePrivateHouseInstance (int userId) {
      if (server.privateHouseInstances.Contains(userId)) {
         server.privateHouseInstances.Remove(userId);
      }
   }

   public RpcResponse<int> getNewGroupId () {
      return server.InvokeServerRpc(server.MasterServer_GetNewGroupId);
   }

   public RpcResponse<int> getNewGroupInstanceId () {
      return server.InvokeServerRpc(server.MasterServer_GetNewGroupInstanceId);
   }

   public void findUserPrivateAreaToVisit (int visitorUserId, int visitedUserId, string areaKeyOverride, string spawnTarget, Direction facing) {
      server.InvokeServerRpc(server.MasterServer_FindUserPrivateAreaVisit, visitorUserId, visitedUserId, areaKeyOverride, spawnTarget, facing);
   }

   public void findUserLocationForAdminGoTo (int adminUserId, int userId, bool hasConfirmed) {
      server.InvokeServerRpc(server.MasterServer_FindUserLocationForAdminGoTo, adminUserId, userId, hasConfirmed);
   }

   public void findUserLocationForSteamFriendJoin (int userId, ulong steamFriendId) {
      server.InvokeServerRpc(server.MasterServer_FindUserLocationForSteamFriendJoin, userId, steamFriendId);
   }

   public void findUserLocationForFriendJoin (int userId, int friendUserId) {
      server.InvokeServerRpc(server.MasterServer_FindUserLocationForFriendJoin, userId, friendUserId);
   }

   public void getUsersInInstanceForAdminVoyagePanel (int groupInstanceId, int instanceId, int callerUserId) {
      server.InvokeServerRpc(server.MasterServer_GetUsersInInstanceForAdminVoyagePanel, groupInstanceId, instanceId, callerUserId);
   }

   public void registerUserInTreasureSite (int userId, int groupInstanceId, int treasureSiteInstanceId) {
      server.InvokeServerRpc(server.MasterServer_RegisterUserInTreasureSite, userId, groupInstanceId, treasureSiteInstanceId);
   }

   public void sendDirectChatMessage (ChatInfo chatInfo) {
      D.adminLog("Sending direct chat message to player for pvp announcement: {" + chatInfo.sender + " : " + chatInfo.recipient + "}", D.ADMIN_LOG_TYPE.PvpAnnouncement);
      server.InvokeServerRpc(server.MasterServer_SendPvpAnnouncement, chatInfo.senderId, chatInfo.text, chatInfo.sender, chatInfo.recipient);
   }

   public void sendContextMenuRequest (int senderUserId, string senderUserName, int targetUserId, int inviterGroupId, int inviterGuildId) {
      server.InvokeServerRpc(server.MasterServer_SendContextMenuRequest, senderUserId, senderUserName, server.networkedPort.Value, targetUserId, inviterGroupId, inviterGuildId);
   }

   public void returnContextMenuRequest (int senderUserId, string senderUserName, int originPort, int targetUserId, string targetName, int targetGroupId, int targetGuildId, bool isSameGroup, bool isSameGuild) {
      server.InvokeServerRpc(server.MasterServer_ReturnContextMenuRequest, senderUserId, senderUserName, originPort, targetUserId, targetName, targetGroupId, targetGuildId, isSameGroup, isSameGuild);
   }

   public void sendGlobalChatMessage (ChatInfo chatInfo) {
      server.InvokeServerRpc(server.MasterServer_SendGlobalMessage, chatInfo.chatId, chatInfo.text, chatInfo.chatTime.ToBinary(), chatInfo.sender, chatInfo.senderId, GuildIconData.guildIconDataToString(chatInfo.guildIconData), chatInfo.guildName, chatInfo.isSenderMuted, chatInfo.isSenderAdmin, chatInfo.extra);
   }

   public void sendSpecialChatMessage (int userId, ChatInfo chatInfo) {
      server.InvokeServerRpc(server.MasterServer_SendSpecialChatMessage, userId, chatInfo.chatId, chatInfo.messageType, chatInfo.text, chatInfo.chatTime.ToBinary(), chatInfo.sender, chatInfo.recipient, chatInfo.senderId, GuildIconData.guildIconDataToString(chatInfo.guildIconData), chatInfo.guildName, chatInfo.isSenderMuted, chatInfo.extra);
   }

   public void censorGlobalMessagesFromUser (int userId) {
      server.InvokeServerRpc(server.MasterServer_CensorGlobalMessagesFromUser, userId);
   }

   public void sendConfirmationMessage (ConfirmMessage.Type confirmType, int userId, string customMessage = "") {
      server.InvokeServerRpc(server.MasterServer_SendConfirmationMessage, confirmType, userId, customMessage);
   }

   public void sendConfirmationMessageToGuild (ConfirmMessage.Type confirmType, int guildId, string customMessage = "") {
      server.InvokeServerRpc(server.MasterServer_SendConfirmationMessageToGuild, confirmType, guildId, customMessage);
   }

   public void sendConfirmationMessageToGroup (ConfirmMessage.Type confirmType, int groupId, string customMessage = "") {
      server.InvokeServerRpc(server.MasterServer_SendConfirmationMessageToGroup, confirmType, groupId, customMessage);
   }

   public void updateGuildMemberPermissions (int userId, int guildRankPriority, int guildPermissions) {
      server.InvokeServerRpc(server.MasterServer_UpdateGuildMemberPermissions, userId, guildRankPriority, guildPermissions);
   }

   public void replaceGuildMembersRankPriority (int guildId, int originalRankPriority, int newRankPriority, int newPermissions) {
      server.InvokeServerRpc(server.MasterServer_ReplaceGuildMembersRankPriority, guildId, originalRankPriority, newRankPriority, newPermissions);
   }

   public void updateUserGuildId (int userId, int newGuildId) {
      server.InvokeServerRpc(server.MasterServer_UpdateUserGuildId, userId, newGuildId);
   }

   public void sendGuildInvitationNotification (int guildId, int inviterUserId, string inviterName, int invitedUserId, string guildName) {
      server.InvokeServerRpc(server.MasterServer_SendGuildInvitationNotification, guildId, inviterUserId, inviterName, invitedUserId, guildName);
   }

   public void sendGuildAcceptNotification (int guildId, string inviterName, int inviterUserId, string invitedUserName, int invitedUserId, string guildName) {
      server.InvokeServerRpc(server.MasterServer_SendGuildAcceptNotification, guildId, inviterName, inviterUserId, invitedUserName, invitedUserId, guildName);
   }
   
   public void sendGroupInvitationNotification (int groupId, int inviterUserId, string inviterName, int inviteeUserId) {
      server.InvokeServerRpc(server.MasterServer_SendGroupInvitationNotification, groupId, inviterUserId, inviterName, inviteeUserId);
   }

   public void createGroupInstanceInServer (int serverPort, int groupInstanceId, GroupInstance parameters) {
      server.InvokeServerRpc(server.MasterServer_CreateGroupInstanceInServer, serverPort, groupInstanceId, parameters);
   }

   public void requestGroupInstanceCreation (GroupInstance parameters) {
      server.InvokeServerRpc(server.MasterServer_RequestGroupInstanceCreation, parameters);
   }

   public void warpUserToPOIInstanceInServer (int serverPort, int groupId, int userId, string areaKey, string spawnKey, Direction facingDirection) {
      server.InvokeServerRpc(server.MasterServer_WarpUserToPOIInstanceInServer, serverPort, groupId, userId, areaKey, spawnKey, facingDirection);
   }

   public void sendGroupCompositionToMembers (int groupId) {
      server.InvokeServerRpc(server.MasterServer_SendGroupCompositionToMembers, groupId);
   }

   public void sendMemberPartialUpdateToGroup (int groupId, int userId, string userName, int XP, string areaKey) {
      server.InvokeServerRpc(server.MasterServer_SendMemberPartialUpdateToGroup, groupId, userId, userName, XP, areaKey);
   }

   public void joinPvpGame (int groupInstanceId, int userId, string userName, PvpTeamType team) {
      server.InvokeServerRpc(server.MasterServer_JoinPvpGame, groupInstanceId, userId, userName, team);
   }

   public void clearPowerupsForUser (int userId) {
      server.InvokeServerRpc(server.MasterServer_ClearPowerupsForUser, userId);
   }

   public void warpUser (int userId, int groupInstanceId, string areaKey, Direction newFacingDirection, string spawn = "") {
      server.InvokeServerRpc(server.MasterServer_WarpUser, userId, groupInstanceId, areaKey, newFacingDirection, spawn);
   }

   public void logRequest (string message) {
      server.InvokeServerRpc(server.MasterServer_LogRequest, message);
   }

   public void displayNoticeScreenWithError (int userId, ErrorMessage.Type errorType, string message) {
      server.InvokeServerRpc(server.MasterServer_DisplayNoticeScreenWithError, userId, errorType, message);
   }

   public void logInMasterServer (string message) {
      server.InvokeServerRpc(server.MasterServer_Log, this.server.networkedPort.Value, message);
   }

   public RpcResponse<int> redirectUserToBestServer (int userId, string userName, int groupInstanceId, bool isSinglePlayer, string destinationAreaKey, string currentAddress, string currentAreaKey, int targetInstanceId, int targetServerPort) {
      return server.InvokeServerRpc(server.MasterServer_RedirectUserToBestServer, userId, userName, groupInstanceId, isSinglePlayer, destinationAreaKey, server.networkedPort.Value, currentAddress, currentAreaKey, targetInstanceId, targetServerPort);
   }

   public void onUserConnectsToServer (int userId) {
      server.InvokeServerRpc(server.MasterServer_OnUserConnectsToServer, userId);
   }

   public void onUserDisconnectsFromServer (int userId) {
      server.InvokeServerRpc(server.MasterServer_OnUserDisconnectsFromServer, userId);
   }

   public void forceDisconnectAllNonAdminUsers (int adminUserId, string message) {
      server.InvokeServerRpc(server.MasterServer_ForceDisconnectAllNonAdminUsers, adminUserId, message);
   }

   public void summonUser (int targetUserId, UserLocationBundle adminLocation) {
      server.InvokeServerRpc(server.MasterServer_SummonUser, targetUserId, adminLocation);
   }

   public void applyPenaltyToPlayer(int accId, PenaltyInfo.ActionType penaltyType, int seconds) {
      server.InvokeServerRpc(server.MasterServer_ApplyPenaltyToPlayer, accId, penaltyType, seconds);
   }

   public void playAchievementSfxForPlayer (int userId) {
      server.InvokeServerRpc(server.MasterServer_PlayAchievementSfxForPlayer, userId);
   }

   public void changeUserName (int userId, string oldName, string newName) {
      server.InvokeServerRpc(server.MasterServer_ChangeUserName, userId, oldName, newName);
   }

   public void recreateLeagueInstanceAndAddUserToGroup (int groupInstanceId, int groupId, int userId, string userName) {
      server.InvokeServerRpc(server.MasterServer_RecreateLeagueInstanceAndAddUserToGroup, groupInstanceId, groupId, userId, userName);
   }

   public bool doesLocalServerHaveQueuedArea (string areaKey) {
      if (server == null) {
         D.debug("Missing Local Server!");
         return false;
      }

      if (server.areaBeingGenerated == null) {
         D.debug("Missing List!");
         return false;
      }

      if (server.areaBeingGenerated.ContainsKey(areaKey)) {
         return true;
      }

      return false;
   }

   public List<int> getAllOnlineUsers () {
      List<int> userIds = new List<int>();

      foreach (NetworkedServer server in servers) {
         userIds.AddRange(server.connectedUserIds);
      }

      return userIds;
   }

   public void updateAdminGameSettings () {
      server.InvokeServerRpc(server.MasterServer_UpdateAdminGameSettings);
   }

   public void requestLogFromServer (int targetServerPort, int requesterUserId) {
      server.InvokeServerRpc(server.MasterServer_RequestLogFromServer, targetServerPort, requesterUserId, server.networkedPort.Value);
   }

   public List<BasicInstanceOverview> getAllInstancesInArea (string areaKey, bool ignoreSinglePlayer = false) {
      List<BasicInstanceOverview> instanceList = new List<BasicInstanceOverview>();
      foreach (NetworkedServer server in servers) {
         foreach (BasicInstanceOverview instance in server.instances.Values) {
            if (instance.areaKey == areaKey && !(instance.isSinglePlayer && ignoreSinglePlayer)) {
               instanceList.Add(instance);
            }
         }
      }
      return instanceList;
   }

   public string getAllInstancesInfoText (Instance currentInstance) {
      bool isSinglePlayer = currentInstance.isSinglePlayer;
      bool isPrivate = currentInstance.getMaxPlayers() == 1;
      bool isGroupInstance = GroupInstanceManager.isAnyGroupSpecificArea(currentInstance.areaKey);

      // Get the list of instances in this area, in all servers
      List<BasicInstanceOverview> instanceList;
      if (isSinglePlayer || isPrivate || isGroupInstance) {
         instanceList = new List<BasicInstanceOverview>();
         instanceList.Add(currentInstance.getBasicInstanceOverview());
      } else {
         instanceList = getAllInstancesInArea(currentInstance.areaKey, true);
      }

      // Check if the current instance is not yet in the server network data (created this frame)
      if (!instanceList.Exists(i => i.serverPort == this.server.networkedPort.Value && i.instanceId == currentInstance.id)) {
         // Manually add it to the list
         instanceList.Add(currentInstance.getBasicInstanceOverview());
      }

      instanceList = instanceList.OrderBy(i => i.serverPort).ThenBy(i => i.instanceId).ToList();

      StringBuilder builder = new StringBuilder();

      if (isSinglePlayer) {
         builder.Append($"Single Player");
         builder.Append($"\n\n");
      } else if (isPrivate) {
         builder.Append($"Private Instance");
         builder.Append($"\n\n");
      } else if (isGroupInstance) {
         builder.Append($"Group Instance");
         builder.Append($"\n\n");
      }

      int currentPort = -1;
      foreach (BasicInstanceOverview instance in instanceList) {
         if (instance.serverPort != currentPort) {
            builder.Append($"Server #{instance.serverPort - Global.MASTER_SERVER_PORT}");
            builder.Append($"\n");
            currentPort = instance.serverPort;
         }

         if (instance.serverPort == this.server.networkedPort.Value && instance.instanceId == currentInstance.id) {
            // Get the local latest info for the current instance
            builder.Append($"<color=#FFBB33>>> Room {currentInstance.id} ({currentInstance.getPlayerCount()}/{currentInstance.getMaxPlayers()})</color>");
            builder.Append($"\n");
         } else {
            builder.Append($"Room {instance.instanceId} ({instance.playerCount}/{instance.maxPlayerCount})");
            builder.Append($"\n");
         }
      }

      return builder.ToString();
   }

   #region Private Variables

   // Players will only be assigned to the master server if all other servers have this many more players
   private const int MASTER_SERVER_PLAYER_OFFSET = 50;

   #endregion
}
