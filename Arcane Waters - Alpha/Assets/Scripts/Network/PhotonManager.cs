using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhotonManager : Photon.PunBehaviour {
   #region Public Variables

   // Self
   public static PhotonManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   void Start () {
      // Pick a unique user ID
      PhotonNetwork.AuthValues = new AuthenticationValues(Random.Range(1, 100000) + "");
   }

   public override void OnConnectedToMaster () {
      D.editorLog("OnConnectedToMaster", Color.magenta);
      /*
      Debug.Log("Connected to Master!"); 

      if (MyNetworkManager.self.telepathy.port == 7777) {
         createServerRoom();
      } else {
         PhotonNetwork.JoinRoom(getServerRoomName());
      }*/
   }

   public void createServerRoom () {
      D.editorLog("createServerRoom", Color.magenta);
      /*
      RoomOptions newRoomOptions = new RoomOptions();
      newRoomOptions.IsVisible = true;
      newRoomOptions.IsOpen = true;
      newRoomOptions.MaxPlayers = 250;
      newRoomOptions.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable();
      newRoomOptions.CustomRoomProperties.Add("s", "for example level name");
      newRoomOptions.CustomRoomPropertiesForLobby = new string[] { "s" }; // makes level name accessible in a room list in the lobby
      PhotonNetwork.CreateRoom(getServerRoomName(), newRoomOptions, null);*/
   }

   public override void OnCreatedRoom () {
      Debug.Log("Created room.");
   }

   public override void OnJoinedRoom () {
      D.editorLog("OnJoinedRoom", Color.magenta);
      /*
      D.debug("Joined room: " + PhotonNetwork.room);

      D.editorLog("Created the photon server here", Color.green);

      ServerWebRequests.self.initializeServers();*/
      // TODO: Check if alternative logic is working properly
      /*
      GameObject server = PhotonNetwork.Instantiate("Server", Vector3.zero, Quaternion.identity, 0);
      server.name = "Server";*/
   }

   protected string getServerRoomName () {
      string name = "ServerRoom";

      // Make sure multiple devs can launch servers locally without having the names be the same
      name += "-" + SystemInfo.deviceName;

      Debug.Log("Using server room name: " + name);

      return name;
   }

   #region Private Variables

   #endregion
}
