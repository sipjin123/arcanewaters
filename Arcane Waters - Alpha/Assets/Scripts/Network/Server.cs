﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Server : Photon.PunBehaviour {
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
   public PhotonView view;

   // A listing of open area types on this server
   public Area.Type[] openAreas = new Area.Type[0];

   // Keeps track of the random map summaries for this server
   public MapSummary[] mapSummaries = new MapSummary[Area.getRandomAreaTypes().Count];

   #endregion

   private void Awake () {
      // Assign a random id
      id = Random.Range(1, 10000);

      // Store references
      view = GetComponent<PhotonView>();

      // Is this our Server?
      if (view.isMine) {
         ServerNetwork.self.server = this;

         // In Editor host mode, the instance is created before this Server object exists
         InstanceManager.self.recalculateOpenAreas();
      }

      // Keep track of all servers
      ServerNetwork.self.servers.Add(this);
   }

   private void Start () {
      // Update our map summaries with the latest number of players
      InvokeRepeating("updateMapSummariesForThisServer", 5f, 5f);
   }

   private void Update () {
      if (view != null && view.isMine) {
         this.ipAddress = MyNetworkManager.self.networkAddress;
         this.port = MyNetworkManager.self.telepathy.port;
         this.playerCount = MyNetworkManager.self.numPlayers;
      }
   }

   void OnPhotonSerializeView (PhotonStream stream, PhotonMessageInfo info) {
      if (stream.isWriting) {
         // We own this object: send the others our data 
         stream.SendNext(id);
         stream.SendNext(ipAddress);
         stream.SendNext(port);
         stream.SendNext(playerCount);
         stream.SendNext(name);
         stream.SendNext(openAreas);

         for (int i = 0; i < this.mapSummaries.Length; i++) {
            MapSummary summary = this.mapSummaries[i];
            stream.SendNext(summary.serverAddress);
            stream.SendNext(summary.serverPort);
            stream.SendNext(summary.areaType);
            stream.SendNext(summary.biomeType);
            stream.SendNext(summary.playersCount);
            stream.SendNext(summary.maxPlayersCount);
         }
      } else {
         // Someone else's object, receive data 
         id = (int) stream.ReceiveNext();
         ipAddress = (string) stream.ReceiveNext();
         port = (int) stream.ReceiveNext();
         this.playerCount = (int) stream.ReceiveNext();
         this.name = (string) stream.ReceiveNext();
         this.openAreas = (Area.Type[]) stream.ReceiveNext();

         for (int i = 0; i < this.mapSummaries.Length; i++) {
            MapSummary summary = new MapSummary();
            summary.serverAddress = (string) stream.ReceiveNext();
            summary.serverPort = (int) stream.ReceiveNext();
            summary.areaType = (Area.Type) stream.ReceiveNext();
            summary.biomeType = (Biome.Type) stream.ReceiveNext();
            summary.playersCount = (int) stream.ReceiveNext();
            summary.maxPlayersCount = (int) stream.ReceiveNext();

            this.mapSummaries[i] = summary;
         }
      }
   }

   public override void OnConnectionFail (DisconnectCause cause) {
      base.OnConnectionFail(cause);

      D.log("Server " + port + " connection failed: " + cause);
      PhotonNetwork.Destroy(this.gameObject);
   }

   void OnDestroy () {
      ServerNetwork.self.removeServer(this);
   }

   protected void updateMapSummariesForThisServer () {
      // Let the other servers know about our map summaries
      if (this.photonView.isMine) {
         List<MapSummary> mapSummaryList = new List<MapSummary>();

         foreach (Instance instance in RandomMapManager.self.getInstances()) {
            mapSummaryList.Add(instance.getMapSummary());
         }

         this.mapSummaries = mapSummaryList.ToArray();
      }
   }

   [PunRPC]
   void ServerMessage (string message) {
      D.log("Server message: " + message);
   }

   [PunRPC]
   public void SendGlobalChat (int chatId, string message, long timestamp, string senderName, int senderUserId) {
      foreach (NetEntity entity in MyNetworkManager.getPlayers().Values) {
         if (entity != null && entity.connectionToClient != null) {
            entity.Target_ReceiveGlobalChat(chatId, message, timestamp, senderName, senderUserId);
         }
      }
   }

   #region Private Variables

   #endregion
}
