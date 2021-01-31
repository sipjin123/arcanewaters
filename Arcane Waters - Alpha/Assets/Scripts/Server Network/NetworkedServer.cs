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

   // The voyage groups stored in this server
   public NetworkedDictionary<int, VoyageGroupInfo> voyageGroups = new NetworkedDictionary<int, VoyageGroupInfo>(new NetworkedVarSettings { WritePermission = NetworkedVarPermission.Everyone, SendChannel = "Fragmented", SendTickrate = 0 });

   // The voyage instances hosted in this server
   public NetworkedList<Voyage> voyages = new NetworkedList<Voyage>(Global.defaultNetworkedVarSettings);

   // A listing of open area types on this server
   public NetworkedList<string> openAreas = new NetworkedList<string>(Global.defaultNetworkedVarSettings);

   // Keeps track of the users claimed by this server
   public NetworkedList<int> claimedUserIds = new NetworkedList<int>(Global.defaultNetworkedVarSettings);

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
         InvokeRepeating(nameof(updateVoyageInstances), 5.5f, 2f);
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
      foreach (KeyValuePair<int, NetEntity> player in MyNetworkManager.getPlayers()) {
         connectedUserIds.Add(player.Value.userId);
      }
   }

   private void updateVoyageInstances () {
      voyages.Clear();

      // Get the number of groups in all voyage instances
      Dictionary<int, int> allGroupCount = VoyageGroupManager.self.getGroupCountInAllVoyages();

      foreach (Instance instance in InstanceManager.self.getVoyageInstances()) {
         allGroupCount.TryGetValue(instance.voyageId, out int groupCount);
         Voyage voyage = new Voyage(instance.voyageId, instance.areaKey, Area.getName(instance.areaKey), instance.difficulty, instance.biome, instance.isPvP,
            instance.creationDate, instance.treasureSiteCount, instance.capturedTreasureSiteCount, groupCount);
         voyages.Add(voyage);
      }
   }

   public bool isMasterServer () {
      return networkedPort.Value == Global.MASTER_SERVER_PORT;
   }

   private void OnDestroy () {
      ServerNetworkingManager.self.servers.Remove(this);
   }

   [ServerRPC]
   public void MasterServer_SendGlobalMessage (int chatId, string message, long timestamp, string senderName, int senderUserId, string guildIconDataString) {
      InvokeClientRpcOnEveryone(Server_ReceiveGlobalChatMessage, chatId, message, timestamp, senderName, senderUserId, guildIconDataString);
   }

   [ClientRPC]
   public void Server_ReceiveGlobalChatMessage (int chatId, string message, long timestamp, string senderName, int senderUserId, string guildIconDataString) {
      // Send the chat message to all users connected to this server
      foreach (KeyValuePair<int, NetEntity> player in MyNetworkManager.getPlayers()) {
         player.Value.Target_ReceiveGlobalChat(chatId, message, timestamp, senderName, senderUserId, guildIconDataString);
      }
   }

   [ServerRPC]
   public void MasterServer_SendGuildChatMessage (int guildId, int chatId, string message, long timestamp, string senderName, int senderUserId, string guildIconDataString) {
      InvokeClientRpcOnEveryone(Server_ReceiveGuildChatMessage, guildId, chatId, message, timestamp, senderName, senderUserId, guildIconDataString);
   }

   [ClientRPC]
   public void Server_ReceiveGuildChatMessage (int guildId, int chatId, string message, long timestamp, string senderName, int senderUserId, string guildIconDataString) {
      // Send the chat message to all guild members connected to this server
      foreach (KeyValuePair<int, NetEntity> player in MyNetworkManager.getPlayers()) {
         if (player.Value.guildId == guildId) {
            player.Value.Target_ReceiveSpecialChat(player.Value.connectionToClient, chatId, message, senderName, player.Value.entityName, timestamp, ChatInfo.Type.Guild, GuildIconData.guildIconDataFromString(guildIconDataString), senderUserId);
         }
      }
   }

   [ServerRPC]
   public void MasterServer_SendSpecialChatMessage (int userId, int chatId, ChatInfo.Type messageType, string message, long timestamp, string senderName, string receiverName, int senderUserId, string guildIconDataString) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(userId);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_ReceiveSpecialChatMessage, userId, chatId, messageType, message, timestamp, senderName, receiverName, senderUserId, guildIconDataString);
      } else {
         handleChatMessageDeliveryError(senderUserId, messageType, message);
      }
   }

   [ClientRPC]
   public void Server_ReceiveSpecialChatMessage (int userId, int chatId, ChatInfo.Type messageType, string message, long timestamp, string senderName, string receiverName, int senderUserId, string guildIconDataString) {
      NetEntity player = EntityManager.self.getEntity(userId);
      if (player != null) {
         player.Target_ReceiveSpecialChat(player.connectionToClient, chatId, message, senderName, receiverName, timestamp, messageType, GuildIconData.guildIconDataFromString(guildIconDataString), senderUserId);
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
      foreach (KeyValuePair<int, NetEntity> player in MyNetworkManager.getPlayers()) {
         if (player.Value.guildId == guildId) {
            ServerMessageManager.sendConfirmation(confirmType, player.Value, customMessage);
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
      foreach (KeyValuePair<int, NetEntity> pair in MyNetworkManager.getPlayers()) {
         if (pair.Value.guildId == guildId && pair.Value.guildRankPriority == originalRankPriority) {
            pair.Value.guildRankPriority = newRankPriority;
            pair.Value.guildPermissions = newPermissions;
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
   public void MasterServer_CreateVoyageInstanceInServer (int serverPort, int voyageId, string areaKey, bool isPvP, Biome.Type biome, Voyage.Difficulty difficulty) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServer(serverPort);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_ReceiveVoyageInstanceCreation, voyageId, areaKey, isPvP, biome, difficulty);
      }
   }

   [ClientRPC]
   public void Server_ReceiveVoyageInstanceCreation (int voyageId, string areaKey, bool isPvP, Biome.Type biome, Voyage.Difficulty difficulty) {
      VoyageManager.self.createVoyageInstance(voyageId, areaKey, isPvP, biome, difficulty);
   }

   [ServerRPC]
   public void MasterServer_RequestVoyageInstanceCreation (string areaKey, bool isPvP, Biome.Type biome, Voyage.Difficulty difficulty) {
      VoyageManager.self.requestVoyageInstanceCreation(areaKey, isPvP, biome, difficulty);
   }

   [ServerRPC]
   public void MasterServer_SendVoyageGroupMembersToUser (int userId) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(userId);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_SendVoyageGroupMembersToUser, userId);
      }
   }

   [ClientRPC]
   public void Server_SendVoyageGroupMembersToUser (int userId) {
      NetEntity player = EntityManager.self.getEntity(userId);
      if (player != null) {
         VoyageGroupManager.self.sendVoyageGroupMembersToClient(player);
      }
   }

   [ServerRPC]
   public int MasterServer_GetNewVoyageGroupId () {
      return VoyageGroupManager.self.getNewGroupId();
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

   #region Private Variables

   #endregion
}