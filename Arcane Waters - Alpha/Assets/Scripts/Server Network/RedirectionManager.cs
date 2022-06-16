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
   public int redirectUserToBestServer (int userId, string userName, int voyageId, bool isSinglePlayer, string destinationAreaKey, int currentServerPort, string currentAddress, string currentAreaKey, int targetInstanceId = -1, int targetServerPort = -1) {
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

      // If a port was specified during redirection, get that server
      if (targetServerPort > 0) {
         D.adminLog("--> Target server port is [" + targetServerPort + "] Overriding current best server: [" + bestServer.networkedPort.Value + "]", D.ADMIN_LOG_TYPE.Redirecting);
         bestServer = ServerNetworkingManager.self.getServer(targetServerPort);
      }

      // Already move the user from one server to the other, so that the server load is immediately updated
      if (currentServer.assignedUserIds.ContainsKey(userId)) {
         currentServer.assignedUserIds.Remove(userId);
      }

      AssignedUserInfo assignedUserInfo = new AssignedUserInfo();
      assignedUserInfo.userId = userId;
      assignedUserInfo.lastOnlineTime = DateTime.UtcNow.ToBinary();
      assignedUserInfo.areaKey = destinationAreaKey;
      assignedUserInfo.isSinglePlayer = isSinglePlayer;
      assignedUserInfo.instanceId = targetInstanceId;
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

      try {
         string singlePlayerString = isSinglePlayer ? "[single player]. " : "";
         string logMsg = string.Format($"Redirecting user {userName} (id {userId}, {currentAddress}) from [{currentAreaKey}, {currentServerPort}] to [{destinationAreaKey}, {bestServer.networkedPort.Value}]. {singlePlayerString}Destination server has {bestServer.connectedUserIds.Count} connected players ({bestServer.assignedUserIds.Count} assigned). Total server count: {ServerNetworkingManager.self.servers.Count}");
         D.debug(logMsg);
      } catch {
         D.debug("Found best server but cannot get details");
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

      bool isLeagueArea = VoyageManager.isAnyLeagueArea(destinationAreaKey);
      bool isPvpArenaArea = VoyageManager.isPvpArenaArea(destinationAreaKey);
      bool isTreasureSiteArea = VoyageManager.isTreasureSiteArea(destinationAreaKey);

      // If the player is in a voyage group and warping to a voyage area or treasure site, get the unique server hosting it
      if (voyageId > 0 && (isLeagueArea || isPvpArenaArea || isTreasureSiteArea)) {
         if (!ServerNetworkingManager.self.tryGetServerHostingVoyage(voyageId, out NetworkedServer server)) {
            D.error("Couldn't find the server hosting the voyage: {" + voyageId + "}");
            D.adminLog("From [" + currentServerPort + "] FAIL: Failed to get Voyage/PVP: {" + server.networkedPort.Value + "} for player {" + userId + ":" + userName + ":" + voyageId + "} " +
               "area:{" + destinationAreaKey + "}" + "isPvp:{" + isPvpArenaArea + "} " + "isLeague:{" + isLeagueArea + "} " + "isTreasure:{" + isTreasureSiteArea + "}", D.ADMIN_LOG_TYPE.Redirecting);
         } else {
            D.adminLog("From [" + currentServerPort + "] Found the best server {" + server.networkedPort.Value + "} for player {" + userId + ":" + userName + ":" + voyageId + "} " +
               "area:{" + destinationAreaKey + "}" + "isPvp:{" + isPvpArenaArea + "} " + "isLeague:{" + isLeagueArea + "} " + "isTreasure:{" + isTreasureSiteArea + "}", D.ADMIN_LOG_TYPE.Redirecting);
         }
         return server;
      }

      if (!isSinglePlayer) {
         // Make sure the servers are ordered by port
         List<NetworkedServer> serverList = ServerNetworkingManager.self.servers.OrderBy(s => s.networkedPort.Value).ToList();

         // Check in all servers if there's an open area
         foreach (NetworkedServer server in serverList) {
            if (server.hasOpenArea(destinationAreaKey) || server.areaBeingGenerated.ContainsKey(destinationAreaKey)) {
               string message = "[Other] Server has {Open Area}, Switching from";
               if (server.areaBeingGenerated.ContainsKey(destinationAreaKey)) {
                  message = "[Other] server has an {Area being Generated}, Switching from";
                  D.adminLog("Here are the Other server areas being generated: " + server.areaBeingGenerated.Count, D.ADMIN_LOG_TYPE.AreaClearing);
                  foreach (KeyValuePair<string, double> temp in server.areaBeingGenerated) {
                     if (WorldMapManager.isWorldMapArea(destinationAreaKey)) {
                        D.adminLog("--> " + temp.Key, D.ADMIN_LOG_TYPE.AreaClearing);
                     }
                  }
               }

               D.adminLog(message + " [" + currentServerPort + "] to [" + server.networkedPort.Value + "] Player {" + userId + ":" + userName + ":" + voyageId + "} " +
                  "area:{" + destinationAreaKey + "}" + "isPvp:{" + isPvpArenaArea + "} " + "isLeague:{" + isLeagueArea + "} " + "isTreasure:{" + isTreasureSiteArea + "}", D.ADMIN_LOG_TYPE.Redirecting);

               return server;
            }
         }
      }

      // Find the server with the least people
      NetworkedServer bestServer = ServerNetworkingManager.self.getFirstServerWithLeastAssignedPlayers();

      if (bestServer == null) {
         D.error("Couldn't find a good server to connect to, server count: " + ServerNetworkingManager.self.servers.Count);
      } else {
         if (!bestServer.areaToInstanceCount.ContainsKey(destinationAreaKey)) {
            if (WorldMapManager.isWorldMapArea(destinationAreaKey)) {
               D.adminLog("Redirecting: Server [" + bestServer.networkedPort.Value.ToString() + "] registered Queued Network Area {" + destinationAreaKey + "}", D.ADMIN_LOG_TYPE.AreaClearing);
               bestServer.areaBeingGenerated.Add(destinationAreaKey, NetworkTime.time);
            }
         } else {
            D.adminLog("Redirecting: Server [" + bestServer.networkedPort.Value.ToString() + "] already has area {" + destinationAreaKey + "} generated", D.ADMIN_LOG_TYPE.AreaClearing);
         }
      }

      D.adminLog("From [" + currentServerPort + "] Searched for Server with least player {" + bestServer.networkedPort.Value + "} for player {" + userId + ":" + userName + ":" + voyageId + "} " +
         "area:{" + destinationAreaKey + "}" + "isPvp:{" + isPvpArenaArea + "} " + "isLeague:{" + isLeagueArea + "} " + "isTreasure:{" + isTreasureSiteArea + "}", D.ADMIN_LOG_TYPE.Redirecting);
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

