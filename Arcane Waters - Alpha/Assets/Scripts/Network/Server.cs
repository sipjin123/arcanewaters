using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Server : MonoBehaviour {//Photon.PunBehaviour {
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

   // The list of active voyage instances on this server
   public List<Voyage> voyages = new List<Voyage>();

   // Determines if this is the local server
   public bool isLocalServer;

   #endregion

   private void Awake () {
      // Assign a random id
      id = Random.Range(1, 10000);

      // TODO: Confirm if this is no longer needed

      // Store references
      //view = GetComponent<PhotonView>();

      // Is this our Server?
      /*if (view.isMine) {
         ServerNetwork.self.server = this;
         D.editorLog("Server is self", Color.red);

         // In Editor host mode, the instance is created before this Server object exists
         InstanceManager.self.recalculateOpenAreas();
      }*/

      // Keep track of all servers
      //ServerNetwork.self.servers.Add(this);
      //ServerNetwork.self.serverList.Add(this);
   }

   public void setAsLocalServer () {
      isLocalServer = true;
      ServerNetwork.self.server = this;
      D.editorLog("Server is self", Color.red);

      // In Editor host mode, the instance is created before this Server object exists
      InstanceManager.self.recalculateOpenAreas();
   }

   private void Start () {
      // Update the online users
      InvokeRepeating("checkOnlineUsers", 5.5f, 5f);

      // Update the status of the voyages hosted by our server
      InvokeRepeating("updateVoyageInfo", 5.5f, 5f);
   }

   private void Update () {
      if (isLocalServer) { 
         this.ipAddress = MyNetworkManager.self.networkAddress;
         this.port = MyNetworkManager.self.telepathy.port;
         this.playerCount = MyNetworkManager.self.numPlayers;
      }
   }

   void OnPhotonSerializeView (PhotonStream stream, PhotonMessageInfo info) {
      // TODO: Confirm if this is still needed since each server fetches other server data and always uploads their own data
      /*
      if (stream.isWriting) {
         // We own this object: send the others our data 
         stream.SendNext(id);
         stream.SendNext(ipAddress);
         stream.SendNext(port);
         stream.SendNext(playerCount);
         stream.SendNext(name);
         stream.SendNext(openAreas);

         int[] connectedUserIdsArray = new int[connectedUserIds.Count];
         connectedUserIds.CopyTo(connectedUserIdsArray);
         stream.SendNext(connectedUserIdsArray);

         stream.SendNext(Util.serialize(voyages));
      } else {
         // Someone else's object, receive data 
         id = (int) stream.ReceiveNext();
         ipAddress = (string) stream.ReceiveNext();
         port = (int) stream.ReceiveNext();
         this.playerCount = (int) stream.ReceiveNext();
         this.name = (string) stream.ReceiveNext();
         this.openAreas = (string[]) stream.ReceiveNext();
         int[] connectedUserIdsArray = (int[]) stream.ReceiveNext();
         connectedUserIds.Clear();
         foreach (int userId in connectedUserIdsArray) {
            connectedUserIds.Add(userId);
         }

         this.voyages = Util.unserialize<Voyage>((string[]) stream.ReceiveNext());
      }*/
   }

   /*
   public override void OnConnectionFail (DisconnectCause cause) {
      base.OnConnectionFail(cause);

      D.log("Server " + port + " connection failed: " + cause);
      PhotonNetwork.Destroy(this.gameObject);
   }*/

   void OnDestroy () {
      ServerNetwork.self.removeServer(this);
   }

   protected void checkOnlineUsers () {
      // TODO: Confirm if this is no longer needed (The users are automatically updated by the ServerWebRequest when a server was recently changed)
      //D.editorLog("Server must update players ");
      /*
      // Rebuilds the list of online users connected to this server
      if (this.photonView.isMine) {
         connectedUserIds.Clear();

         foreach (NetEntity entity in MyNetworkManager.getPlayers().Values) {
            if (entity != null && entity.connectionToClient != null) {
               connectedUserIds.Add(entity.userId);
            }
         }
      }
      */
   }

   protected void updateVoyageInfo () {
      // TODO: Confirm if this is no longer needed (The voyages are automatically updated by the ServerWebRequest when a server was recently changed)
      //D.editorLog("Server must update voyages ");
      /*
      // Rebuilds the list of voyage instances and their status held in this server
      if (this.photonView.isMine) {
         List<Voyage> allVoyages = new List<Voyage>();

         foreach (Instance instance in InstanceManager.self.getVoyageInstances()) {
            Voyage voyage = new Voyage(instance.voyageId, instance.areaKey, instance.difficulty, instance.isPvP,
               instance.creationDate, instance.getTreasureSitesCount(), instance.getCapturedTreasureSitesCount());
            allVoyages.Add(voyage);
         }

         voyages = allVoyages;
      }*/
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

   [PunRPC]
   public void HandleVoyageGroupInvite (int voyageGroupId, string inviterName, int inviteeUserId) {
      D.editorLog("Handle voyage invites here", Color.green);
      // Find the NetEntity of the invitee
      NetEntity inviteeEntity = EntityManager.self.getEntity(inviteeUserId);
      
      if (inviteeEntity != null) {
         inviteeEntity.Target_ReceiveVoyageGroupInvitation(inviteeEntity.connectionToClient, voyageGroupId, inviterName);
      }
   }

   [PunRPC]
   public void CreateVoyageInstance () {
      VoyageManager.self.createVoyageInstance();
   }

   [PunRPC]
   public void CreateVoyageInstance (string areaKey) {
      VoyageManager.self.createVoyageInstance(areaKey);
   }

   #region Private Variables

   #endregion
}
