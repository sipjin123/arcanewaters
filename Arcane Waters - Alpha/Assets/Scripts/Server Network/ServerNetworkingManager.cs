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
      RegisterSerializableClass<VoyageGroupInfo>();
      RegisterSerializableClass<Voyage>();
      RegisterSerializableClass<UserLocationBundle>();
      RegisterSerializableClass<AssignedUserInfo>();
      RegisterSerializableClass<PenaltyInfo>();
      RegisterSerializableClass<ServerOverview>();

      // Log the number of connected players
      if (CommandCodes.get(CommandCodes.Type.AUTO_TEST)) {
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

   public bool tryGetServerHostingVoyage (int voyageId, out NetworkedServer hostingServer) {
      hostingServer = null;

      // Search the voyage in all the servers we know about
      foreach (NetworkedServer server in servers) {
         if (server.voyages.TryGetValue(voyageId, out Voyage voyage)) {
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

   public RpcResponse<int> getNewVoyageGroupId () {
      return server.InvokeServerRpc(server.MasterServer_GetNewVoyageGroupId);
   }

   public RpcResponse<int> getNewVoyageId () {
      return server.InvokeServerRpc(server.MasterServer_GetNewVoyageId);
   }

   public void findUserPrivateAreaToVisit (int visitorUserId, int visitedUserId, string areaKeyOverride, string spawnTarget, Direction facing) {
      server.InvokeServerRpc(server.MasterServer_FindUserPrivateAreaVisit, visitorUserId, visitedUserId, areaKeyOverride, spawnTarget, facing);
   }

   public void findUserLocationForAdminGoTo (int adminUserId, int userId) {
      server.InvokeServerRpc(server.MasterServer_FindUserLocationForAdminGoTo, adminUserId, userId);
   }

   public void getUsersInInstanceForAdminVoyagePanel (int voyageId, int instanceId, int callerUserId) {
      server.InvokeServerRpc(server.MasterServer_GetUsersInInstanceForAdminVoyagePanel, voyageId, instanceId, callerUserId);
   }

   public void registerUserInTreasureSite (int userId, int voyageId, int treasureSiteInstanceId) {
      server.InvokeServerRpc(server.MasterServer_RegisterUserInTreasureSite, userId, voyageId, treasureSiteInstanceId);
   }

   public void sendDirectChatMessage (ChatInfo chatInfo) {
      D.adminLog("Sending direct chat message to player for pvp announcement: {" + chatInfo.sender + " : " + chatInfo.recipient + "}", D.ADMIN_LOG_TYPE.PvpAnnouncement);
      server.InvokeServerRpc(server.MasterServer_SendPvpAnnouncement, chatInfo.senderId, chatInfo.text, chatInfo.sender, chatInfo.recipient);
   }

   public void sendGlobalChatMessage (ChatInfo chatInfo) {
      server.InvokeServerRpc(server.MasterServer_SendGlobalMessage, chatInfo.chatId, chatInfo.text, chatInfo.chatTime.ToBinary(), chatInfo.sender, chatInfo.senderId, GuildIconData.guildIconDataToString(chatInfo.guildIconData), chatInfo.guildName, chatInfo.isSenderMuted, chatInfo.isSenderAdmin);
   }

   public void sendGuildChatMessage (int guildId, ChatInfo chatInfo) {
      server.InvokeServerRpc(server.MasterServer_SendGuildChatMessage, guildId, chatInfo.chatId, chatInfo.text, chatInfo.chatTime.ToBinary(), chatInfo.sender, chatInfo.senderId, GuildIconData.guildIconDataToString(chatInfo.guildIconData), chatInfo.guildName, chatInfo.isSenderMuted);
   }

   public void sendSpecialChatMessage (int userId, ChatInfo chatInfo) {
      server.InvokeServerRpc(server.MasterServer_SendSpecialChatMessage, userId, chatInfo.chatId, chatInfo.messageType, chatInfo.text, chatInfo.chatTime.ToBinary(), chatInfo.sender, chatInfo.recipient, chatInfo.senderId, GuildIconData.guildIconDataToString(chatInfo.guildIconData), chatInfo.guildName, chatInfo.isSenderMuted);
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

   public void sendGroupInvitationNotification (int groupId, int inviterUserId, string inviterName, int inviteeUserId) {
      server.InvokeServerRpc(server.MasterServer_SendGroupInvitationNotification, groupId, inviterUserId, inviterName, inviteeUserId);
   }

   public void createVoyageInstanceInServer (int serverPort, int voyageId, Voyage parameters) {
      server.InvokeServerRpc(server.MasterServer_CreateVoyageInstanceInServer, serverPort, voyageId, parameters);
   }

   public void requestVoyageInstanceCreation (Voyage parameters) {
      server.InvokeServerRpc(server.MasterServer_RequestVoyageInstanceCreation, parameters);
   }

   public void sendVoyageGroupCompositionToMembers (int groupId) {
      server.InvokeServerRpc(server.MasterServer_SendVoyageGroupCompositionToMembers, groupId);
   }

   public void sendMemberPartialUpdateToGroup (int groupId, int userId, string userName, int XP, string areaKey) {
      server.InvokeServerRpc(server.MasterServer_SendMemberPartialUpdateToGroup, groupId, userId, userName, XP, areaKey);
   }

   public void joinPvpGame (int voyageId, int userId, string userName, PvpTeamType team) {
      server.InvokeServerRpc(server.MasterServer_JoinPvpGame, voyageId, userId, userName, team);
   }

   public void clearPowerupsForUser (int userId) {
      server.InvokeServerRpc(server.MasterServer_ClearPowerupsForUser, userId);
   }

   public void warpUser (int userId, int voyageId, string areaKey, Direction newFacingDirection, string spawn = "") {
      server.InvokeServerRpc(server.MasterServer_WarpUser, userId, voyageId, areaKey, newFacingDirection, spawn);
   }

   public void displayNoticeScreenWithError (int userId, ErrorMessage.Type errorType, string message) {
      server.InvokeServerRpc(server.MasterServer_DisplayNoticeScreenWithError, userId, errorType, message);
   }

   public void logInMasterServer (string message) {
      server.InvokeServerRpc(server.MasterServer_Log, this.server.networkedPort.Value, message);
   }

   public RpcResponse<int> redirectUserToBestServer (int userId, string userName, int voyageId, bool isSinglePlayer, string destinationAreaKey, string currentAddress, string currentAreaKey, int targetInstanceId, int targetServerPort) {
      return server.InvokeServerRpc(server.MasterServer_RedirectUserToBestServer, userId, userName, voyageId, isSinglePlayer, destinationAreaKey, server.networkedPort.Value, currentAddress, currentAreaKey, targetInstanceId, targetServerPort);
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

   public void forceSinglePlayerModeForUser (int userId) {
      server.InvokeServerRpc(server.MasterServer_ForceSinglePlayerForUser, userId);
   }

   public void mutePlayer (int userId, bool isStealth, long expiresAt) {
      server.InvokeServerRpc(server.MasterServer_MutePlayer, userId, isStealth, expiresAt);
   }

   public void unMutePlayer (int userId) {
      server.InvokeServerRpc(server.MasterServer_UnMutePlayer, userId);
   }

   public void banPlayer (int userId, bool isPermanent, long expiresAt) {
      server.InvokeServerRpc(server.MasterServer_BanPlayer, userId, isPermanent, expiresAt);
   }

   public void kickPlayer (int userId) {
      server.InvokeServerRpc(server.MasterServer_KickPlayer, userId);
   }

   public void changeUserName (int userId, string oldName, string newName) {
      server.InvokeServerRpc(server.MasterServer_ChangeUserName, userId, oldName, newName);
   }

   public void recreateLeagueInstanceAndAddUserToGroup (int voyageId, int groupId, int userId, string userName) {
      server.InvokeServerRpc(server.MasterServer_RecreateLeagueInstanceAndAddUserToGroup, voyageId, groupId, userId, userName);
   }

   public List<int> getAllOnlineUsers () {
      List<int> userIds = new List<int>();

      foreach (NetworkedServer server in servers) {
         userIds.AddRange(server.connectedUserIds);
      }

      return userIds;
   }

   #region Private Variables

   // Players will only be assigned to the master server if all other servers have this many more players
   private const int MASTER_SERVER_PLAYER_OFFSET = 50;

   #endregion
}
