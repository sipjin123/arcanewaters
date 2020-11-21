using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MLAPI.Serialization;
using MLAPI.Serialization.Pooled;
using System.IO;
using Newtonsoft.Json;
using MLAPI;
using System.Linq;

public class ServerNetworkingManager : MonoBehaviour {
   #region Public Variables

   // The server we have control of
   public NetworkedServer server;

   // The Servers we know about
   public HashSet<NetworkedServer> servers = new HashSet<NetworkedServer>();

   // Self
   public static ServerNetworkingManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      // This is where we register any classes that we want to serialize between the Server processes
      RegisterSerializableClass<VoyageGroupInfo>();
      RegisterSerializableClass<Voyage>();
   }

   public static NetworkingManager get () {
      // This wrapper function is just to help clarify the difference between the "Server" Networking Manager, and the regular Network Manager
      return NetworkingManager.Singleton;
   }

   private static void RegisterSerializableClass<T> () {
      // This is just a long-winded way of saying that we want to use Json to serialize and deserialize an object
      SerializationManager.RegisterSerializationHandlers<T>(
         (Stream stream, T objectInstance) => {
            using (PooledBitWriter writer = PooledBitWriter.Get(stream)) {
               writer.WriteStringPacked(JsonConvert.SerializeObject(objectInstance));
            }
         },
         (Stream stream) => {
            using (PooledBitReader reader = PooledBitReader.Get(stream)) {
               return JsonConvert.DeserializeObject<T>(reader.ReadStringPacked().ToString());
            }
         }
      );
   }

   public NetworkedServer findBestServerForConnectingPlayer (string areaKey, string username, int userId, string address,
      bool isSinglePlayer, int voyageId) {
      // Always return the current server if single player
      if (isSinglePlayer) {
         return server;
      }

      // If the player is in a voyage group and warping to a voyage area, get the unique server hosting it
      if (voyageId != -1 && VoyageManager.self.isVoyageArea(areaKey)) {
         NetworkedServer server = getServerHostingVoyage(voyageId);
         if (server == null) {
            D.error("Couldn't find the server hosting the voyage " + voyageId);
         }
         return server;
      }

      // Check if there's an open area already on one of the servers
      foreach (NetworkedServer server in servers) {
         List<string> openAreas = new List<string>(server.openAreas);
         if (openAreas.Contains(areaKey)) {
            return server;
         }
      }

      // Find the server with the least people
      NetworkedServer bestServer = getFirstServerWithLeastPlayers();
      try {
         string logMsg = string.Format("Found best server {0} with player count {1} for {2} ({3})",
            bestServer.networkedPort.Value, bestServer.connectedUserIds.Count, username, address);
         D.debug(logMsg);
      } catch {
         D.debug("Found best server but cannot get details");
      }

      // If this player is claimed by a server, we have to return to that server
      foreach (NetworkedServer server in servers) {
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

   public NetworkedServer getServerContainingUser (int userId) {
      foreach (NetworkedServer server in servers) {
         if (server.connectedUserIds.Contains(userId)) {
            return server;
         }
      }
      return null;
   }

   public NetworkedServer getServer (int port) {
      foreach (NetworkedServer server in servers) {
         if (server.networkedPort.Value == port) {
            return server;
         }
      }
      return null;
   }

   public NetworkedServer getFirstServerWithLeastPlayers () {
      List<NetworkedServer> bestServers = getServersWithLeastPlayers();

      if (bestServers.Count == 0) {
         return null;
      } else {
         // If many servers have the same lowest number of players, select the first port
         List<NetworkedServer> similarServers = bestServers.OrderBy(_ => _.networkedPort.Value).ToList();
         return similarServers[0];
      }
   }

   public NetworkedServer getRandomServerWithLeastPlayers () {
      List<NetworkedServer> bestServers = getServersWithLeastPlayers();

      if (bestServers.Count == 0) {
         return null;
      } else {
         return bestServers.ChooseRandom();
      }
   }

   private List<NetworkedServer> getServersWithLeastPlayers () {
      int leastPlayers = int.MaxValue;
      List<NetworkedServer> bestServers = new List<NetworkedServer>();

      foreach (NetworkedServer server in servers) {
         if (server.connectedUserIds.Count == leastPlayers) {
            bestServers.Add(server);
         } else if (server.connectedUserIds.Count < leastPlayers) {
            bestServers.Clear();
            bestServers.Add(server);
            leastPlayers = server.connectedUserIds.Count;
         }
      }

      return bestServers;
   }

   public NetworkedServer getServerHostingVoyage (int voyageId) {
      // Search the voyage in all the servers we know about
      foreach (NetworkedServer server in servers) {
         foreach (Voyage voyage in server.voyages) {
            if (voyage.voyageId == voyageId) {
               return server;
            }
         }
      }

      return null;
   }

   public void updateHostedVoyageInstances (List<Voyage> allVoyages) {
      server.voyages.Clear();
      foreach (Voyage voyage in allVoyages) {
         server.voyages.Add(voyage);
      }
   }

   public bool isUserOnline (int userId) {
      bool isOnline = false;

      foreach (NetworkedServer server in servers) {
         if (server.connectedUserIds.Contains(userId)) {
            isOnline = true;
            break;
         }
      }

      return isOnline;
   }

   public void claimPlayer (int userId) {
      if (!server.claimedUserIds.Contains(userId)) {
         server.claimedUserIds.Add(userId);
      }
   }

   public void releasePlayerClaim (int userId) {
      server.claimedUserIds.Remove(userId);
   }

   public void sendGlobalChatMessage (ChatInfo chatInfo) {
      server.InvokeServerRpc(server.MasterServer_SendGlobalMessage, chatInfo.chatId, chatInfo.text, chatInfo.chatTime.ToBinary(), chatInfo.sender, chatInfo.senderId);
   }

   public void sendVoyageGroupInvitation (int groupId, int inviterUserId, string inviterName, int inviteeUserId) {
      server.InvokeServerRpc(server.MasterServer_SendVoyageGroupInvitation, groupId, inviterUserId, inviterName, inviteeUserId);
   }

   public void sendVoyageInstanceCreation (int serverPort, string areaKey, bool isPvP, Biome.Type biome) {
      server.InvokeServerRpc(server.MasterServer_SendVoyageInstanceCreation, serverPort, areaKey, isPvP, biome);
   }

   #region Private Variables

   #endregion
}
