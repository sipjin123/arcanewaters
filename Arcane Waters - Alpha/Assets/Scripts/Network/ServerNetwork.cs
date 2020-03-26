using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class ServerNetwork : MonoBehaviour {
   #region Public Variables

   // String constants to store details about this specific server in our Photon custom properties
   public static string PLAYER_CLAIM = "player_claim-";

   // The Server object we have control of
   public Server server;

   // The Servers we know about
   public HashSet<Server> servers = new HashSet<Server>();

   // Self
   public static ServerNetwork self;

   #endregion

   void Awake () {
      self = this;

      // Routinely check if we need to reconnect to Photon
      InvokeRepeating("checkPhotonConnection", 10f, 5f);
   }

   public Server findBestServerForConnectingPlayer (string areaKey, string username, int userId, string address, bool isSinglePlayer) {
      // Always return the current server if single player
      if (isSinglePlayer) {
         return server;
      }

      // Check if there's an open area already on one of the servers
      foreach (Server server in servers) {
         List<string> openAreas = new List<string>(server.openAreas);
         if (openAreas.Contains(areaKey)) {
            return server;
         }
      }

      // Find the server with the least people
      Server bestServer = getServerWithLeastPlayers();
      try {
         string logMsg = string.Format("Found best server {0}:{1} with player count {2} for {3} ({4})",
            bestServer.ipAddress, bestServer.port, bestServer.playerCount, username, address);
         D.debug(logMsg);
      } catch {
         D.debug("Found best server but cannot get details");
      }

      // If this player is claimed by a server, we have to return to that server
      foreach (Server server in servers) {
         if (server.photonView.owner.CustomProperties.ContainsKey(PLAYER_CLAIM + userId)) {
            bestServer = server;
            break;
         }
      }

      if (bestServer == null) {
         D.log("Couldn't find a good server to connect to, server count: " + servers.Count);
      }

      return bestServer;
   }

   public Server getServerHostingVoyage (int voyageId) {
      // Search the voyage in all the servers we know about
      foreach (Server server in servers) {
         foreach (Voyage voyage in server.voyages) {
            if (voyage.voyageId == voyageId) {
               return server;
            }
         }
      }

      return null;
   }

   public void removeServer (Server server) {
      if (servers.Contains(server)) {
         D.debug("Removing server: " + server.port);
         servers.Remove(server);
      }
   }

   public void sendGlobalMessage (ChatInfo chatInfo) {
      server.photonView.RPC("SendGlobalChat", PhotonTargets.All, chatInfo.chatId, chatInfo.text, chatInfo.chatTime.ToBinary(), chatInfo.sender, chatInfo.senderId);
   }

   protected void checkPhotonConnection () {
      StartCoroutine(CO_checkPhotonConnection());
   }

   public void claimPlayer (int userId) {
      string keyValue = PLAYER_CLAIM + userId;

      // Store it into the custom properties
      ExitGames.Client.Photon.Hashtable customProps = new ExitGames.Client.Photon.Hashtable() {
         { keyValue, true }
      };
      PhotonNetwork.player.SetCustomProperties(customProps);
   }

   public void releaseClaim (int userId) {
      string keyValue = PLAYER_CLAIM + userId;

      // If there isn't an existing claim, then we don't have to do anything
      if (!PhotonNetwork.player.CustomProperties.ContainsKey(keyValue)) {
         return;
      }

      // Store it as null into the custom properties
      ExitGames.Client.Photon.Hashtable customProps = new ExitGames.Client.Photon.Hashtable() {
         { keyValue, null }
      };
      PhotonNetwork.player.SetCustomProperties(customProps);
   }

   public Server getServer (string ipAddress, int port) {
      foreach (Server server in servers) {
         if (server.ipAddress == ipAddress && server.port == port) {
            return server;
         }
      }

      return null;
   }

   public Server getServerWithLeastPlayers () {
      int leastPlayers = int.MaxValue;
      Server bestServer = null;

      foreach (Server server in servers) {
         if (server.playerCount < leastPlayers) {
            bestServer = server;
            leastPlayers = server.playerCount;
         }
      }

      return bestServer;
   }

   public bool isUserOnline (int userId) {
      bool isOnline = false;

      foreach (Server server in servers) {
         if (server.connectedUserIds.Contains(userId)) {
            isOnline = true;
            break;
         }
      }

      return isOnline;
   }

   public Server getServerContainingUser (int userId) {
      foreach (Server server in servers) {
         if (server.connectedUserIds.Contains(userId)) {
            return server;
         }
      }
      return null;
   }

   public Voyage getVoyage (int voyageId) {
      // Search the voyage in all the servers we know about
      foreach (Server server in servers) {
         foreach (Voyage voyage in server.voyages) {
            if (voyage.voyageId == voyageId) {
               return voyage;
            }
         }
      }

      return null;
   }

   public List<Voyage> getAllVoyages () {
      List<Voyage> voyages = new List<Voyage>();

      // Get all the voyages we know about in all the servers we know about
      foreach (Server server in servers) {
         foreach (Voyage voyage in server.voyages) {
            voyages.Add(voyage);
         }
      }

      return voyages;
   }

   protected IEnumerator CO_checkPhotonConnection () {
      // If we're not running a server, or already in a Photon room, we're done
      if (!NetworkServer.active || PhotonNetwork.room != null) {
         yield break;
      }

      // Wait a few seconds to make sure we're not in the process of connecting
      yield return new WaitForSeconds(5f);

      if (PhotonNetwork.room == null) {
         D.log("Not in any Photon room, so reconnecting to Master.");

         PhotonNetwork.Disconnect();
         yield return new WaitForSeconds(3f);
         MyNetworkManager.self.connectToPhotonMaster();
      }
   }

   #region Private Variables

   #endregion
}
