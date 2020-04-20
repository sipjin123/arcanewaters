﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Server : MonoBehaviour {
   #region Public Variables

   // A test ID
   public int id;

   // Our IP address
   public string ipAddress;

   // Our server port
   public int port;

   // The number of players on this server
   public int playerCount;

   // Our Photon view
   //public PhotonView view;

   // The device name
   public string deviceName;
    
   // A listing of open area types on this server
   public string[] openAreas = new string[0];

   // Keeps track of the users connected to this server
   public HashSet<int> connectedUserIds = new HashSet<int>();

   // Keeps track of the users claimed by this server
   public HashSet<int> claimedUserIds = new HashSet<int>();

   // The list of active voyage instances on this server
   public List<Voyage> voyages = new List<Voyage>();

   // Determines if this is the local server
   public bool isLocalServer;

   #endregion

   private void Awake () {
      // Assign a random id
      id = Random.Range(1, 10000);
   }

   public void setAsLocalServer () {
      isLocalServer = true;
      ServerNetwork.self.server = this;
      ServerNetwork.self.servers.Add(this);
      ServerNetwork.self.serverList.Add(this);
      D.editorLog("Local Server is: " + deviceName + " - " + port, Color.red);

      // In Editor host mode, the instance is created before this Server object exists
      InstanceManager.self.recalculateOpenAreas();
   }

   private void Update () {
      //if (view != null && view.isMine) {
      if (isLocalServer) { 
         this.ipAddress = MyNetworkManager.self.networkAddress;
         this.port = MyNetworkManager.self.telepathy.port;
         this.playerCount = MyNetworkManager.self.numPlayers;
      }
   }

   void OnDestroy () {
      ServerNetwork.self.removeServer(this);
   }

   [PunRPC]
   void ServerMessage (string message) {
      D.log("Server message: " + message);
   }

   public void SendGlobalChat (string message, int senderUserId) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.storeChatLog(senderUserId, message, System.DateTime.UtcNow, ChatInfo.Type.Global);
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            D.editorLog("Server Sent a message globally", Color.green);
         });
      });
      /*
      foreach (NetEntity entity in MyNetworkManager.getPlayers().Values) {
         if (entity != null && entity.connectionToClient != null) {
            entity.Target_ReceiveGlobalChat(chatId, message, timestamp, senderName, senderUserId);
         }
      }*/
   }

   public void HandleVoyageGroupInvite (int voyageGroupId, string inviterName, int inviteeUserId) {
      D.editorLog("Handle voyage invites here", Color.green);
      // Find the NetEntity of the invitee
      NetEntity inviteeEntity = EntityManager.self.getEntity(inviteeUserId);
      
      if (inviteeEntity != null) {
         inviteeEntity.Target_ReceiveVoyageGroupInvitation(inviteeEntity.connectionToClient, voyageGroupId, inviterName);
      }
   }

   public void CreateVoyageInstance () {
      VoyageManager.self.createVoyageInstance();
   }

   public void CreateVoyageInstance (string areaKey) {
      D.editorLog("Creating new voyage: " + areaKey, Color.green);
      VoyageManager.self.createVoyageInstance(areaKey);
   }

   #region Private Variables

   #endregion
}
