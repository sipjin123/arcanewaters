using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ServerCommunicationHandlerv2;

public class Server : MonoBehaviour {
   #region Public Variables

   // A test ID
   public int id;

   // Our IP address
   public string ipAddress;

   // Our server port
   public int port;

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
      this.ipAddress = MyNetworkManager.self.networkAddress; 
      this.port = MyNetworkManager.self.telepathy.port;

      isLocalServer = true;
      ServerNetwork.self.server = this;
      ServerNetwork.self.servers.Add(this);
      D.editorLog("Local Server is: " + deviceName + " - " + port, Color.red);

      // In Editor host mode, the instance is created before this Server object exists
      InstanceManager.self.recalculateOpenAreas();
   }

   void OnDestroy () {
      ServerNetwork.self.removeServer(this);
   }

   public bool isMainServer () {
      return port == 7777;
   }

   public void SendGlobalChat (string message, int senderUserId, string userName) {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.storeChatLog(senderUserId, userName, message, System.DateTime.UtcNow, ChatInfo.Type.Global, ServerCommunicationHandler.self.ourIp);
      });
   }

   public void handleVoyageGroupInvite (int voyageGroupId, string inviterName, int inviteeUserId) {
      // Find the NetEntity of the invitee
      NetEntity inviteeEntity = EntityManager.self.getEntity(inviteeUserId);
      
      if (inviteeEntity != null) {
         inviteeEntity.Target_ReceiveVoyageGroupInvitation(inviteeEntity.connectionToClient, voyageGroupId, inviterName);
      }
   }

   #region Private Variables

   #endregion
}
