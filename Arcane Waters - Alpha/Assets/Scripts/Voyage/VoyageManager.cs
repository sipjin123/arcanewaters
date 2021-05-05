using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System;
using MLAPI.Messaging;

public class VoyageManager : GenericGameManager {

   #region Public Variables

   // Self
   public static VoyageManager self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;

      // Randomize the starting value of the PvP parameter
      _isNewVoyagePvP = UnityEngine.Random.Range(0, 2) == 0;
   }

   public void startVoyageManagement () {
      // At server startup, for dev builds, make all the sea maps accessible by creating one voyage for each
      if (!Util.isCloudBuild()) {
         StartCoroutine(CO_CreateInitialVoyages());
      }

      // Regularly check that there are enough voyage instances open and create more
      InvokeRepeating(nameof(createVoyageInstanceIfNeeded), 20f, 10f);
   }

   /// <summary>
   /// Note that only the Master server can launch voyage instance creations. Use requestVoyageInstanceCreation() to ensure the Master server handles it.
   /// </summary>
   [Server]
   public void createVoyageInstance (int voyageId, string areaKey, bool isPvP, bool isLeague, int leagueIndex, int leagueRandomSeed, Biome.Type biome, int difficulty) {
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
      Instance instance = InstanceManager.self.createNewInstance(areaKey, false, true, voyageId, isPvP, isLeague, leagueIndex, leagueRandomSeed, difficulty, biome);

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

   [Server]
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

   [Server]
   public List<Voyage> getAllTreasureSiteInstancesLinkedToVoyages () {
      List<Voyage> voyages = new List<Voyage>();

      // Get all the voyages we know about in all the servers we know about
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         foreach (Voyage voyage in server.treasureSites) {
            voyages.Add(voyage);
         }
      }

      return voyages;
   }

   [Server]
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

   [Server]
   public bool doesVoyageExists (int voyageId) {
      return tryGetVoyage(voyageId, out Voyage voyage);
   }

   public static bool isVoyageOrLeagueArea (string areaKey) {
      return AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.Voyage || AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.League || AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.LeagueLobby;
   }

   public static bool isLeagueOrLobbyArea (string areaKey) {
      return AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.League || AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.LeagueLobby;
   }

   public static bool isLeagueArea (string areaKey) {
      return AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.League;
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

   [Server]
   public static bool isVoyageOpenToNewGroups (Voyage voyage) {
      return isVoyageOpenToNewGroups(voyage, VoyageGroupManager.self.getGroupCountInVoyage(voyage.voyageId));
   }

   [Server]
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

   [Server]
   public void registerUserInTreasureSite (int userId, int voyageId, int instanceId) {
      if (!InstanceManager.self.tryGetVoyageInstance(voyageId, out Instance seaVoyageInstance)) {
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
      if (!InstanceManager.self.tryGetVoyageInstance(instance.voyageId, out Instance seaVoyageInstance)) {
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

      if (!Global.player.isAboutToWarpOnClient) {
         Global.player.setupForWarpClient();
         Global.player.rpc.Cmd_WarpToLeague();
      }
   }

   public void returnToTownFromLeague (NetEntity entity) {
      if (Global.player == null || !Global.player.isClient || entity == null || !entity.isLocalPlayer) {
         return;
      }

      Global.player.rpc.Cmd_ReturnToTownFromLeague();
   }

   [Server]
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

   [Server]
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
   public void forceAdminWarpToVoyageAreas (NetEntity admin, string newArea) {
      if (isVoyageOrLeagueArea(newArea)) {
         // Try to warp the admin to his current voyage if the area parameter fits
         if (admin.tryGetVoyage(out Voyage voyage) && voyage.areaKey.Equals(newArea)) {
            admin.spawnInNewMap(voyage.voyageId, newArea, Direction.South);
            return;
         }

         // If the destination is a league map, always create a new instance
         if (isLeagueOrLobbyArea(newArea)) {
            if (VoyageGroupManager.self.tryGetGroupById(admin.voyageGroupId, out VoyageGroupInfo voyageGroup)) {
               VoyageGroupManager.self.removeUserFromGroup(voyageGroup, admin);
            }
            Instance instance = InstanceManager.self.getInstance(admin.instanceId);

            if (isLobbyArea(newArea)) {
               createLeagueInstanceAndWarpPlayer(admin, 0, instance.biome, -1, newArea);
            } else {
               createLeagueInstanceAndWarpPlayer(admin, 1, instance.biome, -1, newArea);
            }
            return;
         }

         // Find an active voyage in this area
         if (tryGetVoyage(newArea, out voyage)) {
            VoyageGroupManager.self.forceAdminJoinVoyage(admin, voyage.voyageId);
         } else {
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, admin, "No active voyage found in area " + newArea + ". Use '/admin create_voyage' first.");
            return;
         }

         // Warp the admin to the voyage instance
         admin.spawnInNewMap(voyage.voyageId, newArea, Direction.South);
         return;
      } else if (isTreasureSiteArea(newArea)) {
         if (admin.tryGetVoyage(out Voyage v)) {
            // If the admin already belongs to a voyage, warp him directly - the treasure site instance will be created automatically
            admin.spawnInNewMap(v.voyageId, newArea, Direction.South);
         } else {
            Instance instance = InstanceManager.self.getInstance(admin.instanceId);
            StartCoroutine(CO_ForceAdminWarpToTreasureSite(admin, newArea, instance.biome));
         }
      }
   }

   [Server]
   private IEnumerator CO_ForceAdminWarpToTreasureSite (NetEntity admin, string treasureSiteAreaKey, Biome.Type biome) {
      // Get a new voyage id from the master server
      RpcResponse<int> response = ServerNetworkingManager.self.getNewVoyageId();
      while (!response.IsDone) {
         yield return null;
      }
      int voyageId = response.Value;

      // Make the admin and its group (if possible) join the voyage
      VoyageGroupManager.self.forceAdminJoinVoyage(admin, voyageId);

      // Set the difficulty to the number of members in the group
      int difficulty = 1;
      if (admin.tryGetGroup(out VoyageGroupInfo voyageGroup)) {
         difficulty = voyageGroup.members.Count;
      }

      // Launch the creation of a random sea map instance, to be the parent of the treasure site instance
      requestVoyageInstanceCreation(voyageId, "", false, true, Voyage.MAPS_PER_LEAGUE, -1, biome, difficulty);

      // Wait until the voyage instance has been created
      while (!tryGetVoyage(voyageId, out Voyage voyage)) {
         yield return null;
      }

      // Warp the admin to the treasure site area
      admin.spawnInNewMap(voyageId, treasureSiteAreaKey, Direction.South);
   }

   [Server]
   public void requestVoyageInstanceCreation (string areaKey = "", bool isPvP = false, bool isLeague = false, int leagueIndex = 0, int leagueRandomSeed = -1, Biome.Type biome = Biome.Type.None, int difficulty = 0) {
      // Only the master server can generate new voyage ids
      if (!ServerNetworkingManager.self.server.isMasterServer()) {
         ServerNetworkingManager.self.requestVoyageInstanceCreation(areaKey, isPvP, isLeague, leagueIndex, leagueRandomSeed, biome, difficulty);
         return;
      }

      int voyageId = getNewVoyageId();
      requestVoyageInstanceCreation(voyageId, areaKey, isPvP, isLeague, leagueIndex, leagueRandomSeed, biome, difficulty);
   }

   [Server]
   public void requestVoyageInstanceCreation (int voyageId, string areaKey = "", bool isPvP = false, bool isLeague = false, int leagueIndex = 0, int leagueRandomSeed = -1, Biome.Type biome = Biome.Type.None, int difficulty = 0) {
      // Find the server with the least people
      NetworkedServer bestServer = ServerNetworkingManager.self.getRandomServerWithLeastPlayers();

      if (bestServer != null) {
         ServerNetworkingManager.self.createVoyageInstanceInServer(bestServer.networkedPort.Value, voyageId, areaKey, isPvP, isLeague, leagueIndex, leagueRandomSeed, biome, difficulty);
      }
   }

   [Server]
   public void createLeagueInstanceAndWarpPlayer (NetEntity player, int leagueIndex, Biome.Type biome, int randomSeed = -1, string areaKey = "") {
      StartCoroutine(CO_CreateLeagueInstanceAndWarpPlayer(player, leagueIndex, biome, randomSeed, areaKey));
   }

   [Server]
   private IEnumerator CO_CreateLeagueInstanceAndWarpPlayer (NetEntity player, int leagueIndex, Biome.Type biome, int randomSeed = -1, string areaKey = "") {      
      // Get a new voyage id from the master server
      RpcResponse<int> response = ServerNetworkingManager.self.getNewVoyageId();
      while (!response.IsDone) {
         yield return null;
      }
      int voyageId = response.Value;

      if (string.IsNullOrEmpty(areaKey)) {
         if (leagueIndex == 0) {
            // The first league map is always a lobby
            areaKey = getLobbyAreaKeys()[UnityEngine.Random.Range(0, getLobbyAreaKeys().Count)];
         } else {
            // Generate a random list of league maps indexes to ensure there is no repetition in the same league
            List<int> mapIndexes = new List<int>();
            for (int i = 0; i < Voyage.MAPS_PER_LEAGUE; i++) {
               mapIndexes.Add(i);
            }

            // The seed will be saved in the voyage to generate the same map series when warping to the next map
            if (randomSeed < 0) {
               randomSeed = new System.Random().Next();
            }

            System.Random r = new System.Random(randomSeed);
            mapIndexes = mapIndexes.OrderBy(x => r.Next()).ToList();

            // Get the list of league maps
            List<string> mapList = getLeagueAreaKeys();

            if (mapList.Count == 0) {
               D.error("No league maps available!");
               player.rpc.Target_OnWarpFailed("No league maps available");
               yield break;
            }

            // Pick the index corresponding to the league index
            if (mapIndexes[leagueIndex - 1] < mapList.Count) {
               areaKey = mapList[mapIndexes[leagueIndex - 1]];
            } else {
               D.error("Not enough league maps. Some will be repeated!");
               areaKey = mapList[UnityEngine.Random.Range(0, mapList.Count)];
            }
         }
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
      if (leagueIndex == 0) {
         // The first instance is created in the best available server
         requestVoyageInstanceCreation(voyageId, areaKey, false, true, leagueIndex, randomSeed, biome, difficulty);
      } else {
         // Keep all instances of the same league in the same server
         int serverPort = ServerNetworkingManager.self.server.networkedPort.Value;
         ServerNetworkingManager.self.createVoyageInstanceInServer(serverPort, voyageId, areaKey, false, true, leagueIndex, randomSeed, biome, difficulty);
      }

      // Wait until the voyage instance has been created
      while (!tryGetVoyage(voyageId, out Voyage voyage)) {
         yield return null;
      }

      // At the creation of the league lobby, clear powerups of all users
      if (leagueIndex == 0) {
         if (voyageGroup != null) {
            foreach (int memberUserId in voyageGroup.members) {
               NetEntity memberEntity = EntityManager.self.getEntity(memberUserId);
               if (memberEntity != null) {
                  PowerupManager.self.clearPowerupsForUser(memberUserId);
               }
            }
         } else {
            PowerupManager.self.clearPowerupsForUser(player.userId);
         }
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

   public void closeVoyageCompleteNotificationWhenLeavingArea () {
      StartCoroutine(CO_CloseVoyageCompleteNotificationWhenLeavingArea());
   }

   private IEnumerator CO_CloseVoyageCompleteNotificationWhenLeavingArea () {
      if (Global.player == null) {
         yield break;
      }

      string currentArea = Global.player.areaKey;

      while (Global.player == null || string.Equals(Global.player.areaKey, currentArea)) {
         yield return new WaitForSeconds(1f);
      }

      NotificationManager.self.removeAllTypes(Notification.Type.VoyageCompleted);
      NotificationManager.self.removeAllTypes(Notification.Type.NewLocationUnlocked);
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
