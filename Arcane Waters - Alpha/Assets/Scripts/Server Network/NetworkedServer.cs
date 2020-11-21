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

public class NetworkedServer : NetworkedBehaviour
{
   #region Public Variables

   // The port associated with this Networked Server
   public NetworkedVar<int> networkedPort = new NetworkedVar<int>(new NetworkedVarSettings { WritePermission = NetworkedVarPermission.OwnerOnly });

   // The users connected to this server
   public NetworkedList<int> connectedUserIds = new NetworkedList<int>(new NetworkedVarSettings { WritePermission = NetworkedVarPermission.OwnerOnly });

   // An example of how to set up a Networked Dictionary with a custom object type
   public NetworkedDictionary<int, VoyageGroupInfo> voyagesDictionary = new NetworkedDictionary<int, VoyageGroupInfo>(new NetworkedVarSettings { WritePermission = NetworkedVarPermission.OwnerOnly });

   // The voyage instances hosted in this server
   public NetworkedList<Voyage> voyages = new NetworkedList<Voyage>(new NetworkedVarSettings { WritePermission = NetworkedVarPermission.OwnerOnly, SendChannel = "Fragmented" });

   // A listing of open area types on this server
   public NetworkedList<string> openAreas = new NetworkedList<string>(new NetworkedVarSettings { WritePermission = NetworkedVarPermission.OwnerOnly });

   // Keeps track of the users claimed by this server
   public NetworkedList<int> claimedUserIds = new NetworkedList<int>(new NetworkedVarSettings { WritePermission = NetworkedVarPermission.OwnerOnly });

   #endregion

   private void Start () {
      // We only want to make modifications to this Networked Server if we are the owner
      if (this.IsOwner) {
         // Assign this server's port to this Networked Server object
         this.networkedPort.Value = MyNetworkManager.getCurrentPort();

         // For demonstration purposes, create some random Voyage Group Info
         for (int i = 0; i < 5; i++) {
            VoyageGroupInfo voyage = new VoyageGroupInfo();
            voyage.groupId = UnityEngine.Random.Range(5, 10000);
            voyagesDictionary.Add(voyage.groupId, voyage);
         }

         // Register this server as our own
         ServerNetworkingManager.self.server = this;

         // Regularly update data shared between servers
         InvokeRepeating(nameof(updateConnectedPlayers), 5f, 1f);
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

   public bool isMasterServer () {
      return networkedPort.Value == Global.MASTER_SERVER_PORT;
   }

   private void OnDestroy () {
      ServerNetworkingManager.self.servers.Remove(this);
   }

   [ServerRPC]
   public void MasterServer_SendGlobalMessage (int chatId, string message, long timestamp, string senderName, int senderUserId) {
      InvokeClientRpcOnEveryone(Server_ReceiveGlobalChatMessage, chatId, message, timestamp, senderName, senderUserId);
   }

   [ClientRPC]
   public void Server_ReceiveGlobalChatMessage (int chatId, string message, long timestamp, string senderName, int senderUserId) {
      // Send the chat message to all users connected to this server
      foreach (KeyValuePair<int, NetEntity> player in MyNetworkManager.getPlayers()) {
         player.Value.Target_ReceiveGlobalChat(chatId, message, timestamp, senderName, senderUserId);
      }
   }

   [ServerRPC]
   public void MasterServer_SendVoyageGroupInvitation (int groupId, int inviterUserId, string inviterName, int inviteeUserId) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServerContainingUser(inviteeUserId);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_ReceiveVoyageGroupInvitation, groupId, inviterUserId, inviterName, inviteeUserId);
      }
   }

   [ClientRPC]
   public void Server_ReceiveVoyageGroupInvitation (int groupId, int inviterUserId, string inviterName, int inviteeUserId) {
      NetEntity player = EntityManager.self.getEntity(inviteeUserId);
      if (player != null) {
         player.Target_ReceiveVoyageGroupInvitation(player.connectionToClient, groupId, inviterName);
      }
   }

   [ServerRPC]
   public void MasterServer_SendVoyageInstanceCreation (int serverPort, string areaKey, bool isPvP, Biome.Type biome) {
      NetworkedServer targetServer = ServerNetworkingManager.self.getServer(serverPort);
      if (targetServer != null) {
         targetServer.InvokeClientRpcOnOwner(Server_ReceiveVoyageInstanceCreation, areaKey, isPvP, biome);
      }
   }

   [ClientRPC]
   public void Server_ReceiveVoyageInstanceCreation (string areaKey, bool isPvP, Biome.Type biome) {
      VoyageManager.self.createVoyageInstance(areaKey, isPvP, biome);
   }

   #region Private Variables

   #endregion
}