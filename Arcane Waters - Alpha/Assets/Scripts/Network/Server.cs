using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
   public string[] openAreas = new string[0];

   // Keeps track of the users connected to this server
   public HashSet<int> connectedUserIds = new HashSet<int>();

   // The list of active voyage instances on this server
   public List<Voyage> voyages = new List<Voyage>();

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
      // Update the online users
      InvokeRepeating("checkOnlineUsers", 5.5f, 5f);

      // Update the voyage statuses
      InvokeRepeating("updateVoyageInfo", 5.5f, 5f);

      // Regularly check that there are enough voyage instances open
      InvokeRepeating("createVoyageInstanceIfNeeded", 10f, 10f);
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

   protected void checkOnlineUsers () {
      // Rebuilds the list of online users connected to this server
      if (this.photonView.isMine) {
         connectedUserIds.Clear();

         foreach (NetEntity entity in MyNetworkManager.getPlayers().Values) {
            if (entity != null && entity.connectionToClient != null) {
               connectedUserIds.Add(entity.userId);
            }
         }
      }
   }

   protected void updateVoyageInfo () {
      // Rebuilds the list of voyage instances and their status held in this server
      if (this.photonView.isMine) {
         List<Voyage> allVoyages = new List<Voyage>();

         foreach (Instance instance in InstanceManager.self.getVoyageInstances()) {
            Voyage voyage = new Voyage(instance.voyageId, instance.areaKey, instance.difficulty, instance.isPvP,
               instance.creationDate, instance.getTreasureSitesCount(), instance.getCapturedTreasureSitesCount());
            allVoyages.Add(voyage);
         }

         voyages = allVoyages;
      }
   }

   protected void createVoyageInstanceIfNeeded () {
      if (view == null || !view.isMine || port != 7777) {
         return;
      }

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Get the group count in each active voyage
         Dictionary<int, int> voyageToGroupCount = DB_Main.getGroupCountInAllVoyages();

         // Back to Unity Thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Get the voyage instances in all known servers
            List<Voyage> allVoyages = ServerNetwork.self.getAllVoyages();

            // Count the number of voyages that are still open to new groups
            int openVoyagesCount = 0;
            foreach (Voyage voyage in allVoyages) {
               // Get the number of groups in the voyage instance
               int groupCount;
               if (!voyageToGroupCount.TryGetValue(voyage.voyageId, out groupCount)) {
                  // If the number of groups could not be found, set it to zero
                  groupCount = 0;
               }

               if (VoyageManager.isVoyageOpenToNewGroups(voyage, groupCount)) {
                  openVoyagesCount++;
               }
            }

            // If there are missing voyages, create a new one
            if (openVoyagesCount < Voyage.OPEN_VOYAGE_INSTANCES) {
               // Find the server with the least people
               Server bestServer = ServerNetwork.self.getServerWithLeastPlayers();
               
               // Create a new voyage instance on the chosen server
               bestServer.photonView.RPC("CreateVoyageInstance", bestServer.view.owner);
            }
         });
      });
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
      // Find the NetEntity of the invitee
      NetEntity inviteeEntity = EntityManager.self.getEntity(inviteeUserId);

      if (inviteeEntity != null) {
         inviteeEntity.Target_ReceiveVoyageGroupInvitation(inviteeEntity.connectionToClient, voyageGroupId, inviterName);
      }
   }

   [PunRPC]
   public void CreateVoyageInstance () {
      // Get the list of sea maps area keys
      List<string> seaMaps = AreaManager.self.getSeaAreaKeys();
      
      // If there are no available areas, do nothing
      if (seaMaps.Count == 0) {
         return;
      }

      // Get the voyage instances in all known servers
      List<Voyage> allVoyages = ServerNetwork.self.getAllVoyages();

      // Background thread
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Get a new voyage id
         int voyageId = DB_Main.getNewVoyageId();

         // Back to Unity Thread
         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // Randomize the voyage parameters
            string areaKey = seaMaps[Random.Range(0, seaMaps.Count)];
            Voyage.Difficulty difficulty = Util.randomEnumStartAt<Voyage.Difficulty>(1);
            bool isPvP = Random.Range(0, 2) == 0;

            // Create the area instance
            InstanceManager.self.createNewInstance(areaKey, false, true, voyageId, isPvP, difficulty);
         });
      });
   }

   #region Private Variables

   #endregion
}
