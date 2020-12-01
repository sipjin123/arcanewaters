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
   public void MasterServer_SendGlobalMessage (int chatId, string message, long timestamp, string senderName, int senderUserId, string iconBackground, string iconBackPalettes, string iconBorder, string iconSigil, string iconSigilPalettes) {
      InvokeClientRpcOnEveryone(Server_ReceiveGlobalChatMessage, chatId, message, timestamp, senderName, senderUserId, iconBackground, iconBackPalettes, iconBorder, iconSigil, iconSigilPalettes);
   }

   [ClientRPC]
   public void Server_ReceiveGlobalChatMessage (int chatId, string message, long timestamp, string senderName, int senderUserId, string iconBackground, string iconBackPalettes, string iconBorder, string iconSigil, string iconSigilPalettes) {
      // Send the chat message to all users connected to this server
      foreach (KeyValuePair<int, NetEntity> player in MyNetworkManager.getPlayers()) {
         player.Value.Target_ReceiveGlobalChat(chatId, message, timestamp, senderName, senderUserId, iconBackground, iconBackPalettes, iconBorder, iconSigil, iconSigilPalettes);
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
   public void MasterServer_SendVoyageInstanceCreation (int serverPort, int voyageId, string areaKey, bool isPvP, Biome.Type biome) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServer(serverPort);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_ReceiveVoyageInstanceCreation, voyageId, areaKey, isPvP, biome);
      }
   }

   [ClientRPC]
   public void Server_ReceiveVoyageInstanceCreation (int voyageId, string areaKey, bool isPvP, Biome.Type biome) {
      VoyageManager.self.createVoyageInstance(voyageId, areaKey, isPvP, biome);
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

   #region Private Variables

   #endregion
}