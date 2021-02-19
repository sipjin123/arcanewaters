using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System;
using MLAPI.Messaging;

public class VoyageManager : MonoBehaviour {

   #region Public Variables

   // Self
   public static VoyageManager self;

   #endregion

   void Awake () {
      self = this;

      // Randomize the starting value of the PvP parameter
      _isNewVoyagePvP = UnityEngine.Random.Range(0, 2) == 0;
   }

   public void startVoyageManagement () {
      // At server startup, for dev builds, make all the sea maps accessible by creating one voyage for each
#if !CLOUD_BUILD
      StartCoroutine(CO_CreateInitialVoyages());
#endif

      // Regularly check that there are enough voyage instances open and create more
      InvokeRepeating(nameof(createVoyageInstanceIfNeeded), 20f, 10f);
   }

   /// <summary>
   /// Note that only the Master server can launch voyage instance creations. Use requestVoyageInstanceCreation() to ensure the Master server handles it.
   /// </summary>
   public void createVoyageInstance (int voyageId, string areaKey, bool isPvP, bool isLeague, int leagueIndex, Biome.Type biome, int difficulty) {
      // Check if the area is defined
      if (string.IsNullOrEmpty(areaKey)) {
         // Get the list of sea maps area keys
         List<string> seaMaps;
         if (isLeague) {
            seaMaps = getLeagueAreaKeys();
         } else {
            seaMaps = getVoyageAreaKeys();
         }

         // If there are no available areas, do nothing
         if (seaMaps.Count == 0) {
            return;
         }

         // Randomly choose an area
         areaKey = seaMaps[UnityEngine.Random.Range(0, seaMaps.Count)];
      }

      // Randomize the biome if it is not defined
      if (biome == Biome.Type.None) {
         biome = Util.randomEnumStartAt<Biome.Type>(1);
      }

      // Randomize the difficulty if it is not defined
      if (difficulty == 0) {
         difficulty = UnityEngine.Random.Range(1, Voyage.getMaxDifficulty() + 1);
      }

      // Create the area instance
      Instance instance = InstanceManager.self.createNewInstance(areaKey, false, true, voyageId, isPvP, isLeague, leagueIndex, difficulty, biome);

      // Immediately make the new voyage info accessible to other servers
      ServerNetworkingManager.self.server.addNewVoyageInstance(instance, 0);
   }

   [Server]
   public bool tryGetVoyage (int voyageId, out Voyage voyage) {
      voyage = default;

      // Search the voyage in all the servers we know about
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         foreach (Voyage v in server.voyages) {
            if (v.voyageId == voyageId) {
               voyage = v;
               return true;
            }
         }
      }
      
      return false;
   }

   [Server]
   public bool tryGetVoyage (string areaKey, out Voyage voyage) {
      voyage = default;

      // Search the first active voyage in the given area
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         foreach (Voyage v in server.voyages) {
            if (string.Equals(v.areaKey, areaKey, StringComparison.InvariantCultureIgnoreCase)) {
               voyage = v;
               return true;
            }
         }
      }
      
      return false;
   }

   [Server]
   public bool tryGetVoyageForGroup (int voyageGroupId, out Voyage voyage) {
      voyage = default;

      if (VoyageGroupManager.self.tryGetGroupById(voyageGroupId, out VoyageGroupInfo voyageGroup)) {
         if (voyageGroup.voyageId > 0 && tryGetVoyage(voyageGroup.voyageId, out Voyage v)) {
            voyage = v;
            return true;
         }
      }
      
      return false;
   }

   public List<Voyage> getAllVoyages () {
      List<Voyage> voyages = new List<Voyage>();

      // Get all the voyages we know about in all the servers we know about
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         foreach (Voyage voyage in server.voyages) {
            voyages.Add(voyage);
         }
      }

      return voyages;
   }

   public List<Voyage> getAllOpenVoyageInstances () {
      // Get the number of groups in all voyage instances
      Dictionary<int, int> allGroupCount = VoyageGroupManager.self.getGroupCountInAllVoyages();

      // Select the voyages that are open to new groups
      List<Voyage> allOpenVoyages = new List<Voyage>();
      foreach (Voyage voyage in getAllVoyages()) {
         allGroupCount.TryGetValue(voyage.voyageId, out int groupCount);
         if (isVoyageOpenToNewGroups(voyage, groupCount) && !voyage.isLeague) {
            allOpenVoyages.Add(voyage);
         }
      }

      return allOpenVoyages;
   }

   public bool doesVoyageExists (int voyageId) {
      return tryGetVoyage(voyageId, out Voyage voyage);
   }

   public static bool isVoyageOrLeagueArea (string areaKey) {
      return AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.Voyage || AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.League || AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.LeagueLobby;
   }

   public static bool isLeagueOrLobbyArea (string areaKey) {
      return AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.League || AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.LeagueLobby;
   }

   public static bool isLobbyArea (string areaKey) {
      return AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.LeagueLobby;
   }

   public static bool isTreasureSiteArea (string areaKey) {
      return AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.TreasureSite;
   }

   public List<string> getVoyageAreaKeys () {
      return AreaManager.self.getSeaAreaKeys().Where(k => AreaManager.self.getAreaSpecialType(k) == Area.SpecialType.Voyage).ToList();
   }

   public List<string> getLeagueAreaKeys () {
      return AreaManager.self.getSeaAreaKeys().Where(k => AreaManager.self.getAreaSpecialType(k) == Area.SpecialType.League).ToList();
   }

   public List<string> getLobbyAreaKeys () {
      return AreaManager.self.getSeaAreaKeys().Where(k => AreaManager.self.getAreaSpecialType(k) == Area.SpecialType.LeagueLobby).ToList();
   }

   public static bool isVoyageOpenToNewGroups (Voyage voyage) {
      return isVoyageOpenToNewGroups(voyage, VoyageGroupManager.self.getGroupCountInVoyage(voyage.voyageId));
   }

   public static bool isVoyageOpenToNewGroups (Voyage voyage, int groupCount) {
      // Calculate the time left until the voyage closes to new groups
      DateTime voyageCreationDate = DateTime.FromBinary(voyage.creationDate);
      TimeSpan timeLeft = voyageCreationDate.AddSeconds(Voyage.INSTANCE_OPEN_DURATION).Subtract(DateTime.UtcNow);

      // Check that the voyage is still in the open phase
      if (timeLeft.TotalSeconds <= 0) {
         return false;
      } else {
         // Check if the maximum number of groups has been reached
         if (groupCount >= Voyage.getMaxGroupsPerInstance(voyage.difficulty)) {
            return false;
         } else {
            return true;
         }
      }
   }

   public void registerUserInTreasureSite (int userId, int voyageId, int instanceId) {
      Instance seaVoyageInstance = InstanceManager.self.getVoyageInstance(voyageId);
      if (seaVoyageInstance == null) {
         D.error(string.Format("Could not find the sea voyage instance to register a user in a treasure site. userId: {0}", userId));
         return;
      }

      // Search for the treasure site object that warps to the given instance
      foreach (TreasureSite treasureSite in seaVoyageInstance.treasureSites) {
         if (treasureSite.destinationInstanceId == instanceId) {
            // Register the user as being inside the treasure site
            treasureSite.playerListInSite.Add(userId);
            break;
         }
      }
   }

   [Server]
   public void unregisterUserFromTreasureSite (int userId, int instanceId) {
      // Find the instance
      Instance instance = InstanceManager.self.getInstance(instanceId);
      if (instance == null) {
         return;
      }

      // Try to find the treasure site entrance (spawn) where the user is registered
      Instance seaVoyageInstance = InstanceManager.self.getVoyageInstance(instance.voyageId);
      if (seaVoyageInstance == null) {
         return;
      }

      foreach (TreasureSite treasureSite in seaVoyageInstance.treasureSites) {
         if (treasureSite.playerListInSite.Contains(userId)) {
            treasureSite.playerListInSite.Remove(userId);
            break;
         }
      }
   }

   public void showVoyagePanel (NetEntity entity) {
      if (Global.player == null || !Global.player.isClient || entity == null || !entity.isLocalPlayer) {
         return;
      }

      Global.player.rpc.Cmd_RequestVoyageListFromServer();
   }

   public void warpToLeague (NetEntity entity) {
      if (Global.player == null || !Global.player.isClient || entity == null || !entity.isLocalPlayer) {
         return;
      }

      Global.player.rpc.Cmd_WarpToLeague();
   }

   public void returnToTownFromLeague (NetEntity entity) {
      if (Global.player == null || !Global.player.isClient || entity == null || !entity.isLocalPlayer) {
         return;
      }

      Global.player.rpc.Cmd_ReturnToTownFromLeague();
   }

   protected void createVoyageInstanceIfNeeded () {
      // Only the master server launches the creation of voyages instances
      NetworkedServer server = ServerNetworkingManager.self.server;
      if (server == null || !server.isMasterServer()) {
         return;
      }

      // If there are missing voyages, create a new one
      if (getAllOpenVoyageInstances().Count < Voyage.OPEN_VOYAGE_INSTANCES) {
         // Alternate PvP and PvE instances
         _isNewVoyagePvP = !_isNewVoyagePvP;

         requestVoyageInstanceCreation("", _isNewVoyagePvP);
      }
   }

   private IEnumerator CO_CreateInitialVoyages () {
      // Wait until our server is defined
      while (ServerNetworkingManager.self == null || ServerNetworkingManager.self.server == null) {
         yield return null;
      }

      // Wait until our server port is initialized
      while (ServerNetworkingManager.self.server.networkedPort.Value == 0) {
         yield return null;
      }

      // Only the master server launches the creation of voyages instances
      if (ServerNetworkingManager.self.server.isMasterServer()) {

         // Create a voyage instance for each available sea map
         List<string> seaMaps = getVoyageAreaKeys();
         foreach (string areaKey in seaMaps) {
            // Alternate PvP and PvE instances
            _isNewVoyagePvP = !_isNewVoyagePvP;

            requestVoyageInstanceCreation(areaKey, _isNewVoyagePvP);
         }
      }
   }

   [Server]
   public void requestVoyageInstanceCreation (string areaKey = "", bool isPvP = false, bool isLeague = false, int leagueIndex = 0, Biome.Type biome = Biome.Type.None, int difficulty = 0) {
      // Only the master server can generate new voyage ids
      if (!ServerNetworkingManager.self.server.isMasterServer()) {
         ServerNetworkingManager.self.requestVoyageInstanceCreation(areaKey, isPvP, isLeague, leagueIndex, biome, difficulty);
         return;
      }

      int voyageId = getNewVoyageId();
      requestVoyageInstanceCreation(voyageId, areaKey, isPvP, isLeague, leagueIndex, biome, difficulty);
   }

   [Server]
   public void requestVoyageInstanceCreation (int voyageId, string areaKey = "", bool isPvP = false, bool isLeague = false, int leagueIndex = 0, Biome.Type biome = Biome.Type.None, int difficulty = 0) {
      // Find the server with the least people
      NetworkedServer bestServer = ServerNetworkingManager.self.getRandomServerWithLeastPlayers();

      if (bestServer != null) {
         ServerNetworkingManager.self.createVoyageInstanceInServer(bestServer.networkedPort.Value, voyageId, areaKey, isPvP, isLeague, leagueIndex, biome, difficulty);
      }
   }

   [Server]
   public void createLeagueInstanceAndWarpPlayer (NetEntity player, int leagueIndex, Biome.Type biome, string areaKey = "") {
      StartCoroutine(CO_CreateLeagueInstanceAndWarpPlayer(player, leagueIndex, biome, areaKey));
   }

   [Server]
   private IEnumerator CO_CreateLeagueInstanceAndWarpPlayer (NetEntity player, int leagueIndex, Biome.Type biome, string areaKey = "") {
      // Get a new voyage id from the master server
      RpcResponse<int> response = ServerNetworkingManager.self.getNewVoyageId();
      while (!response.IsDone) {
         yield return null;
      }
      int voyageId = response.Value;

      if (string.IsNullOrEmpty(areaKey)) {
         // Randomly choose an area among league maps
         List<string> mapList;
         if (leagueIndex == 0) {
            // The first league map is always a lobby
            mapList = getLobbyAreaKeys();
         } else {
            mapList = getLeagueAreaKeys();
         }
         
         if (mapList.Count == 0) {
            D.error("No league maps available!");
            yield break;
         }
         areaKey = mapList[UnityEngine.Random.Range(0, mapList.Count)];
      }

      int difficulty;
      if (!player.tryGetGroup(out VoyageGroupInfo voyageGroup)) {
         // Create a new group for the player
         VoyageGroupManager.self.createGroup(player, voyageId, true);

         difficulty = 1;
      } else {
         // Link the group to the voyage instance
         voyageGroup.voyageId = voyageId;
         VoyageGroupManager.self.updateGroup(voyageGroup);

         difficulty = voyageGroup.members.Count;
      }

      // Launch the creation of the new voyage instance
      requestVoyageInstanceCreation(voyageId, areaKey, false, true, leagueIndex, biome, difficulty);

      // Wait until the voyage instance has been created
      while (!tryGetVoyage(voyageId, out Voyage voyage)) {
         yield return null;
      }

      if (leagueIndex == 1 && voyageGroup != null) {
         // At the creation of the league, warp all the group members to the instance together
         foreach (int memberUserId in voyageGroup.members) {
            NetEntity memberEntity = EntityManager.self.getEntity(memberUserId);
            if (memberEntity != null) {
               memberEntity.spawnInNewMap(voyageId, areaKey, Direction.South);
            }
         }
      } else {
         player.spawnInNewMap(voyageId, areaKey, Direction.South);
      }
   }

   [Server]
   public int getNewVoyageId () {
      if (!ServerNetworkingManager.self.server.isMasterServer()) {
         D.error("Only the master server can generate new voyage ids.");
         return -1;
      }

      return ++_lastVoyageId;
   }

   #region Private Variables

   // Gets toggled every time a new voyage is created, to ensure an equal number of PvP and PvE instances
   private bool _isNewVoyagePvP = false;

   // The last id used to create a voyage group
   private int _lastVoyageId = 0;

   #endregion
}
