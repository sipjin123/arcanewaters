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

public class ServerNetworkingManager : MonoBehaviour {
   #region Public Variables

   // The server we have control of
   public NetworkedServer server;

   // The Servers we know about
   public HashSet<NetworkedServer> servers = new HashSet<NetworkedServer>();

   // Self
   public static ServerNetworkingManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      // This is where we register any classes that we want to serialize between the Server processes
      RegisterSerializableClass<VoyageGroupInfo>();
      RegisterSerializableClass<Voyage>();
      RegisterSerializableClass<UserLocationBundle>();
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

   public NetworkedServer findBestServerForConnectingPlayer (string areaKey, string username, int userId, string address,
      bool isSinglePlayer, int voyageId) {
      // If the player is in a voyage group and warping to a voyage area or treasure site, get the unique server hosting it
      if (voyageId != -1 &&
         (VoyageManager.isVoyageOrLeagueArea(areaKey) || VoyageManager.isTreasureSiteArea(areaKey))) {
         NetworkedServer server = getServerHostingVoyage(voyageId);
         if (server == null) {
            D.error("Couldn't find the server hosting the voyage " + voyageId);
         }
         return server;
      }

      // Check if there's an open area already on one of the servers
      foreach (NetworkedServer server in servers) {
         List<string> openAreas = new List<string>(server.openAreas);
         if (openAreas.Contains(areaKey)) {
            return server;
         }
      }

      // Find the server with the least people
      NetworkedServer bestServer = getFirstServerWithLeastPlayers();
      try {
         string logMsg = string.Format("Found best server {0} with player count {1} for {2} ({3}) ServerName:{4} ServerCount:{5}",
            bestServer.networkedPort.Value, bestServer.connectedUserIds.Count, username, address, bestServer.name, servers.Count);
         D.debug(logMsg);
      } catch {
         D.debug("Found best server but cannot get details");
      }

      // If this player is claimed by a server, we have to return to that server
      foreach (NetworkedServer server in servers) {
         if (server.claimedUserIds.Contains(userId)) {
            bestServer = server;
            break;
         }
      }

      if (bestServer == null) {
         D.editorLog("Couldn't find a good server to connect to, server count:", Color.red);
         D.log("Couldn't find a good server to connect to, server count: " + servers.Count);
      }

      return bestServer;
   }

   public NetworkedServer getServerContainingUser (int userId) {
      foreach (NetworkedServer server in servers) {
         if (server.connectedUserIds.Contains(userId)) {
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

   public NetworkedServer getFirstServerWithLeastPlayers () {
      List<NetworkedServer> bestServers = getServersWithLeastPlayers();

      if (bestServers.Count == 0) {
         return null;
      } else {
         // If many servers have the same lowest number of players, select the first port
         List<NetworkedServer> similarServers = bestServers.OrderBy(_ => _.networkedPort.Value).ToList();
         return similarServers[0];
      }
   }

   public NetworkedServer getRandomServerWithLeastPlayers () {
      List<NetworkedServer> bestServers = getServersWithLeastPlayers();

      if (bestServers.Count == 0) {
         return null;
      } else {
         NetworkedServer randomServer = bestServers.ChooseRandom();
         return randomServer;
      }
   }

   private List<NetworkedServer> getServersWithLeastPlayers () {
      int leastPlayers = int.MaxValue;
      List<NetworkedServer> bestServers = new List<NetworkedServer>();

      foreach (NetworkedServer server in servers) {
         if (server.connectedUserIds.Count == leastPlayers) {
            bestServers.Add(server);
         } else if (server.connectedUserIds.Count < leastPlayers) {
            bestServers.Clear();
            bestServers.Add(server);
            leastPlayers = server.connectedUserIds.Count;
         }
      }

      return bestServers;
   }

   public NetworkedServer getServerHostingVoyage (int voyageId) {
      // Search the voyage in all the servers we know about
      foreach (NetworkedServer server in servers) {
         foreach (Voyage voyage in server.voyages) {
            if (voyage.voyageId == voyageId) {
               return server;
            }
         }
      }

      return null;
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

   public void claimPlayer (int userId) {
      if (!server.claimedUserIds.Contains(userId)) {
         server.claimedUserIds.Add(userId);
      }
   }

   public void releasePlayerClaim (int userId) {
      server.claimedUserIds.Remove(userId);
   }

   public RpcResponse<int> getNewVoyageGroupId () {
      return server.InvokeServerRpc(server.MasterServer_GetNewVoyageGroupId);
   }

   public RpcResponse<int> getNewVoyageId () {
      return server.InvokeServerRpc(server.MasterServer_GetNewVoyageId);
   }

   public void findUserLocationForAdminGoTo (int adminUserId, int userId) {
      server.InvokeServerRpc(server.MasterServer_FindUserLocationForAdminGoTo, adminUserId, userId);
   }

   public void registerUserInTreasureSite (int userId, int voyageId, int treasureSiteInstanceId) {
      server.InvokeServerRpc(server.MasterServer_RegisterUserInTreasureSite, userId, voyageId, treasureSiteInstanceId);
   }

   public void sendGlobalChatMessage (ChatInfo chatInfo) {
      server.InvokeServerRpc(server.MasterServer_SendGlobalMessage, chatInfo.chatId, chatInfo.text, chatInfo.chatTime.ToBinary(), chatInfo.sender, chatInfo.senderId, GuildIconData.guildIconDataToString(chatInfo.guildIconData));
   }

   public void sendGuildChatMessage (int guildId, ChatInfo chatInfo) {
      server.InvokeServerRpc(server.MasterServer_SendGuildChatMessage, guildId, chatInfo.chatId, chatInfo.text, chatInfo.chatTime.ToBinary(), chatInfo.sender, chatInfo.senderId, GuildIconData.guildIconDataToString(chatInfo.guildIconData));
   }

   public void sendSpecialChatMessage (int userId, ChatInfo chatInfo) {
      server.InvokeServerRpc(server.MasterServer_SendSpecialChatMessage, userId, chatInfo.chatId, chatInfo.messageType, chatInfo.text, chatInfo.chatTime.ToBinary(), chatInfo.sender, chatInfo.recipient, chatInfo.senderId, GuildIconData.guildIconDataToString(chatInfo.guildIconData));
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

   public void createVoyageInstanceInServer (int serverPort, int voyageId, string areaKey, bool isPvP, bool isLeague, int leagueIndex, Biome.Type biome, int difficulty) {
      server.InvokeServerRpc(server.MasterServer_CreateVoyageInstanceInServer, serverPort, voyageId, areaKey, isPvP, isLeague, leagueIndex, biome, difficulty);
   }

   public void requestVoyageInstanceCreation (string areaKey, bool isPvP, bool isLeague, int leagueIndex, Biome.Type biome, int difficulty) {
      server.InvokeServerRpc(server.MasterServer_RequestVoyageInstanceCreation, areaKey, isPvP, isLeague, leagueIndex, biome, difficulty);
   }

   public void sendVoyageGroupMembersToUser (int userId) {
      server.InvokeServerRpc(server.MasterServer_SendVoyageGroupMembersToUser, userId);
   }

   #region Private Variables

   #endregion
}

public class UserLocationBundle
{
   #region Public Variables
   // The user id
   public int userId;

   // The area where the user is located
   public string areaKey;

   // The instance where the user is located
   public int instanceId;

   // The user local position in the area
   public float localPositionX;
   public float localPositionY;

   // The group the user belongs to
   public int voyageGroupId;

   #endregion

   public Vector2 getLocalPosition () {
      return new Vector2(localPositionX, localPositionY);
   }
}