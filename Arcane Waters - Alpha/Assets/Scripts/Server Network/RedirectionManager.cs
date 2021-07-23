using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using Mirror.Profiler;
using MapCreationTool.Serialization;
using System.Linq;

public class RedirectionManager : GenericGameManager
{
   #region Public Variables

   // Self
   public static RedirectionManager self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   public void startOpenAreasManagement () {
      InvokeRepeating(nameof(recalculateOpenAreas), 5f, 1f);
   }

   [Server]
   public int redirectUserToBestServer (int userId, string userName, int voyageId, bool isSinglePlayer, string destinationAreaKey, int currentServerPort, string currentAddress, string currentAreaKey) {
      // Only the master server has reliable info to perform this operation
      if (!ServerNetworkingManager.self.server.isMasterServer()) {
         D.error($"Server {ServerNetworkingManager.self.server.networkedPort.Value} is trying to determine the best server to redirect a user. Only the master server can perform this operation.");
         return Global.MASTER_SERVER_PORT;
      }

      NetworkedServer currentServer = ServerNetworkingManager.self.getServer(currentServerPort);
      if (currentServer == null) {
         D.error($"Could not find the server {currentServerPort}");
         return Global.MASTER_SERVER_PORT;
      }

      // Find the best available server
      NetworkedServer bestServer = findBestServerForConnectingUser(userId, userName, voyageId, destinationAreaKey, isSinglePlayer, currentServerPort, currentAddress);

      // Already move the user from one server to the other, so that the server load is immediately updated
      if (currentServer.assignedUserIds.ContainsKey(userId)) {
         currentServer.assignedUserIds.Remove(userId);
      }

      AssignedUserInfo assignedUserInfo = new AssignedUserInfo();
      assignedUserInfo.userId = userId;
      assignedUserInfo.lastOnlineTime = DateTime.UtcNow.ToBinary();
      assignedUserInfo.areaKey = destinationAreaKey;
      assignedUserInfo.isSinglePlayer = isSinglePlayer;
      bestServer.assignedUserIds[userId] = assignedUserInfo;

      // Players in single player mode don't have an impact on the open areas
      if (!isSinglePlayer) {
         // Update the player count in the current area
         if (!string.IsNullOrEmpty(currentAreaKey) && currentServer.playerCountPerArea.ContainsKey(currentAreaKey)) {
            if (currentServer.playerCountPerArea[currentAreaKey] > 0) {
               currentServer.playerCountPerArea[currentAreaKey]--;
            }
         }

         // Update the player count in the destination server
         if (bestServer.playerCountPerArea.ContainsKey(destinationAreaKey)) {
            bestServer.playerCountPerArea[destinationAreaKey]++;
         } else {
            bestServer.playerCountPerArea.Add(destinationAreaKey, 1);
         }
      }

      return bestServer.networkedPort.Value;
   }

   [Server]
   public NetworkedServer findBestServerForConnectingUser (int userId, string userName, int voyageId, string destinationAreaKey, bool isSinglePlayer, int currentServerPort, string currentAddress) {
      // If this player is claimed by a server, we have to return to that server
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         if (server.claimedUserIds.ContainsKey(userId)) {
            return server;
         }
      }

      // If the player is in a voyage group and warping to a voyage area or treasure site, get the unique server hosting it
      if (voyageId > 0 &&
         (VoyageManager.isAnyLeagueArea(destinationAreaKey) || VoyageManager.isPvpArenaArea(destinationAreaKey) || VoyageManager.isTreasureSiteArea(destinationAreaKey))) {
         NetworkedServer server = ServerNetworkingManager.self.getServerHostingVoyage(voyageId);
         if (server == null) {
            D.error("Couldn't find the server hosting the voyage " + voyageId);
         }
         return server;
      }

      if (!isSinglePlayer) {
         // Check if there is an open area on the server the user is currently connected to
         NetworkedServer currentServer = ServerNetworkingManager.self.getServer(currentServerPort);
         if (currentServer.hasOpenArea(destinationAreaKey)) {
            return currentServer;
         }

         // Check if there's an open area on another server
         foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
            if (server == currentServer) {
               continue;
            }

            if (server.hasOpenArea(destinationAreaKey)) {
               return server;
            }
         }
      }

      // Find the server with the least people
      NetworkedServer bestServer = ServerNetworkingManager.self.getFirstServerWithLeastAssignedPlayers();

      if (bestServer == null) {
         D.error("Couldn't find a good server to connect to, server count: " + ServerNetworkingManager.self.servers.Count);
      }

      try {
         string logMsg = string.Format("Found best server {0} with player count {1} (assigned: {6}) for {2} ({3}) ServerName:{4} ServerCount:{5}",
            bestServer.networkedPort.Value, bestServer.connectedUserIds.Count, userName, currentAddress, bestServer.name, ServerNetworkingManager.self.servers.Count, bestServer.assignedUserIds.Count);
         D.debug(logMsg);
      } catch {
         D.debug("Found best server but cannot get details");
      }

      return bestServer;
   }

   private void recalculateOpenAreas () {
      // Only the master server calculates the open areas
      NetworkedServer ourServer = ServerNetworkingManager.self.server;
      if (ourServer == null || !ourServer.isMasterServer()) {
         return;
      }

      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         server.recalculateOpenAreas();
      }
   }

   #region Private Variables

   #endregion
}

