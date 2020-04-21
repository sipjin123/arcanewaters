using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class ServerNetwork : MonoBehaviour {
   #region Public Variables

   // TODO: Confirm Claim Functionality Replacement
   // String constants to store details about this specific server in our Photon custom properties
   //public static string PLAYER_CLAIM = "player_claim-";

   // The Server object we have control of
   public Server server;

   // The Servers we know about
   public HashSet<Server> servers = new HashSet<Server>();

   // Self
   public static ServerNetwork self;

   // Determines if the best server selection should be overridden for simulation purposes
   public bool overrideBestServerConnection = false;

   // The server index to connect to for simulation purposes
   public int overrideConnectServerIndex = 0;

   #endregion

   void Awake () {
      self = this;
   }

   public Server findBestServerForConnectingPlayer (string areaKey, string username, int userId, string address,
      bool isSinglePlayer, int voyageId) {
      if (overrideBestServerConnection) {
         return servers.ToList()[overrideConnectServerIndex];
      }

      // Always return the current server if single player
      if (isSinglePlayer) {
         return server;
      }

      // If the player is in a voyage group and warping to a voyage area, get the unique server hosting it
      if (voyageId != -1 && VoyageManager.self.isVoyageArea(areaKey)) {
         Server server = getServerHostingVoyage(voyageId);
         if (server == null) {
            D.error("Couldn't find the server hosting the voyage " + voyageId);
         }
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
         if (server.claimedUserIds.Contains(userId)) {
            bestServer = server;
            break;
         }
      }

      if (bestServer == null) {
         D.editorLog("Couldn't find a good server to connect to, server count:", Color.red);
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
      server.SendGlobalChat(chatInfo.text, chatInfo.senderId);
   }

   public void addPlayer (int userId, Server server) {
      ServerCommunicationHandler.self.addPlayer(userId, server);
   }

   public void claimPlayer (int userId) {
      ServerCommunicationHandler.self.claimPlayer(userId);
   }

   public void releaseClaim (int userId) {
      ServerCommunicationHandler.self.releasePlayerClaim(userId);
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
      List<Server> bestServers = new List<Server>();

      foreach (Server server in servers) {
         if (server.playerCount == leastPlayers) {
            bestServers.Add(server);
         } else if (server.playerCount < leastPlayers) {
            bestServers.Clear();
            bestServers.Add(server);
            leastPlayers = server.playerCount;
         }
      }

      if (bestServers.Count  == 0) {
         return null;
      } else {
         // If many servers have the same lowest number of players, randomly choose one
         return bestServers[Random.Range(0, bestServers.Count)];
      }
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

   #region Private Variables

   #endregion
}
