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
   }

   /// <summary>
   /// Note that only the Master server can launch voyage instance creations. Use requestVoyageInstanceCreation() to ensure the Master server handles it.
   /// </summary>
   [Server]
   public void createVoyageInstance (int voyageId, Voyage parameters) {
      // Check if the area is defined
      if (string.IsNullOrEmpty(parameters.areaKey)) {
         // Get the list of sea maps area keys
         List<string> seaMaps;
         if (parameters.isLeague) {
            seaMaps = getLeagueAreaKeys();
         } else if (parameters.isPvP) {
            seaMaps = getPvpArenaAreaKeys();
         } else {
            seaMaps = getVoyageAreaKeys();
         }

         // If there are no available areas, do nothing
         if (seaMaps.Count == 0) {
            return;
         }

         // Randomly choose an area
         parameters.areaKey = seaMaps[UnityEngine.Random.Range(0, seaMaps.Count)];
      }

      // Randomize the biome if it is not defined
      if (parameters.biome == Biome.Type.None) {
         parameters.biome = Util.randomEnumStartAt<Biome.Type>(1);
      }

      // Randomize the difficulty if it is not defined
      if (parameters.difficulty == 0) {
         parameters.difficulty = UnityEngine.Random.Range(1, Voyage.getMaxDifficulty() + 1);
      }

      // Create the area instance
      Instance instance = InstanceManager.self.createNewInstance(parameters.areaKey, false, true, voyageId, parameters.isPvP, parameters.isLeague, parameters.leagueIndex, parameters.leagueRandomSeed, parameters.leagueExitAreaKey, parameters.leagueExitSpawnKey, parameters.leagueExitFacingDirection, parameters.difficulty, parameters.biome);

      // Immediately make the new voyage info accessible to other servers
      if (ServerNetworkingManager.self != null && ServerNetworkingManager.self.server != null) {
         ServerNetworkingManager.self.server.updateVoyageInstance(instance);
      }

      // For pvp instances, create the associated pvp game
      if (parameters.isPvP) {
         PvpManager.self.createNewGameForPvpInstance(instance);
      }
   }

   [Server]
   public bool tryGetVoyage (int voyageId, out Voyage voyage, bool includeTreasureSites = false) {
      voyage = default;

      // Search the voyage in all the servers we know about
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         if (includeTreasureSites) {
            if (server.treasureSites.TryGetValue(voyageId, out voyage)) {
               return true;
            }
         }

         if (server.voyages.TryGetValue(voyageId, out voyage)) {
            return true;
         }
      }
      
      return false;
   }

   [Server]
   public bool tryGetVoyage (string areaKey, out Voyage voyage) {
      voyage = default;

      // Search the first active voyage in the given area
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         foreach (Voyage v in server.voyages.Values) {
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
         voyages.AddRange(server.voyages.Values);
      }

      return voyages;
   }

   [Server]
   public List<Voyage> getAllPvpInstances () {
      List<Voyage> voyages = new List<Voyage>();

      // Get all the voyages we know about in all the servers we know about
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         foreach (Voyage voyage in server.voyages.Values) {
            if (voyage.isPvP) {
               voyages.Add(voyage);
            }
         }
      }

      return voyages;
   }

   [Server]
   public int getPvpInstanceId (string areaKey) {
      List<Voyage> voyages = new List<Voyage>();

      // Get all the voyages we know about in all the servers we know about
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         foreach (Voyage voyage in server.voyages.Values) {
            if (voyage.isPvP && voyage.areaKey == areaKey) {
               return voyage.instanceId;
            }
         }
      }

      return -1;
   }

   [Server]
   public List<Voyage> getAllTreasureSiteInstancesLinkedToVoyages () {
      List<Voyage> voyages = new List<Voyage>();

      // Get all the voyages we know about in all the servers we know about
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         voyages.AddRange(server.treasureSites.Values);
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

   public static bool isAnyLeagueArea (string areaKey) {
      return AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.League
         || AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.LeagueLobby
         || AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.LeagueSeaBoss;
   }

   public static bool isLeagueArea (string areaKey) {
      return AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.League;
   }

   public static bool isLobbyArea (string areaKey) {
      return AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.LeagueLobby;
   }

   public static bool isLeagueSeaBossArea (string areaKey) {
      return AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.LeagueSeaBoss;
   }

   public static bool isTreasureSiteArea (string areaKey) {
      return AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.TreasureSite;
   }

   public static bool isPOIArea (string areaKey) {
      return AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.POI;
   }

   public static bool isWorldMapArea (string areaKey) {
      return AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.WorldMap;
   }

   public static bool isPvpArenaArea (string areaKey) {
      return AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.PvpArena;
   }

   public List<string> getVoyageAreaKeys () {
      return AreaManager.self.getSeaAreaKeys().Where(k => AreaManager.self.getAreaSpecialType(k) == Area.SpecialType.Voyage).ToList();
   }
   
   public List<string> getPvpArenaAreaKeys () {
      return AreaManager.self.getSeaAreaKeys().Where(k => AreaManager.self.getAreaSpecialType(k) == Area.SpecialType.PvpArena && !k.StartsWith(WorldMapManager.WORLD_MAP_PREFIX)).ToList();
   }

   public List<string> getLeagueAreaKeys (Biome.Type biomeType = Biome.Type.None) {
      List<string> allLeagueArea = AreaManager.self.getSeaAreaKeys().Where(k => AreaManager.self.getAreaSpecialType(k) == Area.SpecialType.League).ToList();
      if (biomeType == Biome.Type.None) {
         return allLeagueArea;
      } else {
         List<string> biomedLeagueArea = AreaManager.self.getSeaAreaKeys().Where(k => AreaManager.self.getAreaSpecialType(k) == Area.SpecialType.League 
         && (AreaManager.self.getMapInfo(k) != null && (AreaManager.self.getMapInfo(k).biome == biomeType || AreaManager.self.getMapInfo(k).biome == Biome.Type.None))).ToList();
         if (biomedLeagueArea.Count > 0) {
            return biomedLeagueArea;
         } else {
            D.debug("There are no league areas with biome type {" + biomeType + "}, gather all biomes instead");
            return allLeagueArea;
         }
      }
   }

   public List<string> getLobbyAreaKeys () {
      return AreaManager.self.getSeaAreaKeys().Where(k => AreaManager.self.getAreaSpecialType(k) == Area.SpecialType.LeagueLobby).ToList();
   }

   public List<string> getLeagueSeaBossAreaKeys (Biome.Type biomeType = Biome.Type.None) {
      List<string> allBossLeagueArea = AreaManager.self.getSeaAreaKeys().Where(k => AreaManager.self.getAreaSpecialType(k) == Area.SpecialType.LeagueSeaBoss).ToList();
      if (biomeType == Biome.Type.None) {
         return allBossLeagueArea;
      } else {
         List<string> biomedLeagueArea = AreaManager.self.getSeaAreaKeys().Where(k => 
         // Fetch all league boss areas
         AreaManager.self.getAreaSpecialType(k) == Area.SpecialType.LeagueSeaBoss
         // Make sure map info is not null
         && (AreaManager.self.getMapInfo(k) != null 
         // Retrieve all boss maps with biome type or none biome type
         && (AreaManager.self.getMapInfo(k).biome == biomeType || AreaManager.self.getMapInfo(k).biome == Biome.Type.None))).ToList();
         return biomedLeagueArea;
      }
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
   public void forceAdminWarpToVoyageAreas (NetEntity admin, string newArea) {
      if (isAnyLeagueArea(newArea)) {
         // Try to warp the admin to his current voyage if the area parameter fits
         if (admin.tryGetVoyage(out Voyage voyage) && voyage.areaKey.Equals(newArea)) {
            admin.spawnInNewMap(voyage.voyageId, newArea, Direction.South);
            return;
         }

         // If the destination is a league map, always create a new instance
         if (isAnyLeagueArea(newArea)) {
            if (VoyageGroupManager.self.tryGetGroupById(admin.voyageGroupId, out VoyageGroupInfo voyageGroup)) {
               VoyageGroupManager.self.removeUserFromGroup(voyageGroup, admin);
            }
            Instance instance = InstanceManager.self.getInstance(admin.instanceId);

            createLeagueInstanceAndWarpPlayer(admin, 0, instance.biome, -1, newArea);
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
      Voyage parameters = new Voyage {
         areaKey = "",
         isPvP = false,
         isLeague = true,
         leagueIndex = Voyage.MAPS_PER_LEAGUE - 1,
         biome = biome,
         difficulty = difficulty
      };

      requestVoyageInstanceCreation(voyageId, parameters);

      // Wait until the voyage instance has been created
      while (!tryGetVoyage(voyageId, out Voyage voyage)) {
         yield return null;
      }

      // Warp the admin to the treasure site area
      admin.spawnInNewMap(voyageId, treasureSiteAreaKey, Direction.South);
   }

   [Server]
   public void requestVoyageInstanceCreation (Voyage parameters) {
      // Only the master server can generate new voyage ids
      if (!ServerNetworkingManager.self.server.isMasterServer()) {
         ServerNetworkingManager.self.requestVoyageInstanceCreation(parameters);
         return;
      }

      int voyageId = getNewVoyageId();
      requestVoyageInstanceCreation(voyageId, parameters);
   }

   [Server]
   public void requestVoyageInstanceCreation (int voyageId, Voyage parameters) {
      // Find the server with the least people
      NetworkedServer bestServer = ServerNetworkingManager.self.getRandomServerWithLeastAssignedPlayers();

      if (bestServer != null) {
         ServerNetworkingManager.self.createVoyageInstanceInServer(bestServer.networkedPort.Value, voyageId, parameters);
      }
   }

   [Server]
   public void createLeagueInstanceAndWarpPlayer (NetEntity player, int leagueIndex, Biome.Type biome, int randomSeed = -1, string areaKey = "", string exitAreaKey = "", string exitSpawnKey = "", Direction exitFacingDirection = Direction.South) {
      // Determine the league difficulty and hosting server
      int difficulty = 1;
      int serverPort = ServerNetworkingManager.self.server.networkedPort.Value;
      if (player.tryGetGroup(out VoyageGroupInfo voyageGroup)) {
         // Keep all instances of the same league in the same server
         if (voyageGroup.voyageId > 0 && ServerNetworkingManager.self.tryGetServerHostingVoyage(voyageGroup.voyageId, out NetworkedServer hostingServer)) {
            serverPort = hostingServer.networkedPort.Value;
         }
         difficulty = voyageGroup.members.Count;
      } else {
         NetworkedServer bestServer = ServerNetworkingManager.self.getRandomServerWithLeastAssignedPlayers();
         if (bestServer != null) {
            serverPort = bestServer.networkedPort.Value;
         }
      }

      StartCoroutine(CO_CreateLeagueInstance(leagueIndex, biome, difficulty, serverPort, () => player.rpc.Target_OnWarpFailed("No league maps available", false), (voyage) => warpPlayerToLeagueInstance(player, voyage), randomSeed, areaKey, exitAreaKey, exitSpawnKey, exitFacingDirection));
   }

   [Server]
   private void warpPlayerToLeagueInstance (NetEntity player, Voyage voyage) {
      if (player.tryGetGroup(out VoyageGroupInfo voyageGroup)) {
         if (player.tryGetVoyage(out Voyage currentVoyage) && currentVoyage.leagueIndex == voyage.leagueIndex) {
            // It can happen that two group members launch the creation of the next league map at exactly the same time. If the next instance has already been assigned to the group, ignore this new voyage and stop the process.
            return;
         }

         // Link the group to the voyage instance
         voyageGroup.voyageId = voyage.voyageId;
         VoyageGroupManager.self.updateGroup(voyageGroup);

         if (Voyage.isFirstLeagueMap(voyage.leagueIndex)) {
            // At the creation of the first league map, clear powerups of all users
            foreach (int memberUserId in voyageGroup.members) {
               NetEntity memberEntity = EntityManager.self.getEntity(memberUserId);
               if (memberEntity != null) {
                  PowerupManager.self.clearPowerupsForUser(memberUserId);
               }
            }

            // At the creation of the league, warp all the group members to the instance together
            foreach (int memberUserId in voyageGroup.members) {
               NetEntity memberEntity = EntityManager.self.getEntity(memberUserId);
               if (memberEntity != null) {
                  if (memberEntity.userId == player.userId) {
                     memberEntity.spawnInNewMap(voyage.voyageId, voyage.areaKey, Direction.South);
                  } else {
                     // Do logic here for allies that will be entering the league
                  }
               }
            }
         } else {
            player.spawnInNewMap(voyage.voyageId, voyage.areaKey, Direction.South);
         }
      } else {
         // Create a new group for the player and warp him
         VoyageGroupManager.self.createGroup(player.userId, voyage.voyageId, true);
         PowerupManager.self.clearPowerupsForUser(player.userId);
         player.spawnInNewMap(voyage.voyageId, voyage.areaKey, Direction.South);
      }
   }

   [Server]
   public void recreateLeagueInstanceAndAddUserToGroup (int groupId, int userId, string userName) {
      if (!VoyageGroupManager.self.tryGetGroupById(groupId, out VoyageGroupInfo voyageGroup)) {
         D.error("Error when recreating a league instance: could not find the group");
         return;
      }

      // Redirect the action to the server hosting the instance
      if (ServerNetworkingManager.self.tryGetServerHostingVoyage(voyageGroup.voyageId, out NetworkedServer hostingServer)) {
         if (hostingServer != ServerNetworkingManager.self.server) {
            ServerNetworkingManager.self.recreateLeagueInstanceAndAddUserToGroup(voyageGroup.voyageId, groupId, userId, userName);
            return;
         }
      } else {
         D.error("Error when recreating a league instance: could not find the server hosting the voyage");
         return;
      }

      if (!tryGetVoyage(voyageGroup.voyageId, out Voyage oldVoyage)) {
         D.error("Error when recreating a league instance: could not find the voyage to recreate");
         return;
      }

      if (!oldVoyage.isLeague) {
         D.error("Error when recreating a league instance: the instance is not a league");
         return;
      }

      Instance oldInstance = InstanceManager.self.getInstance(oldVoyage.instanceId);
      if (oldInstance == null) {
         D.error("Error when recreating a league instance: could not find the instance to recreate");
         return;
      }

      // Count the number of players in the treasure sites, if any
      int playerInTreasureSiteCount = 0;
      foreach (TreasureSite site in oldInstance.treasureSites) {
         Instance treasureSiteInstance = InstanceManager.self.getInstance(site.destinationInstanceId);
         if (treasureSiteInstance != null) {
            playerInTreasureSiteCount += treasureSiteInstance.getPlayerCount();
         }
      }

      // Check that the instance and treasure sites have no players in it
      if (oldInstance.getPlayerCount() > 0 || playerInTreasureSiteCount > 0) {
         ServerNetworkingManager.self.sendConfirmationMessage(ConfirmMessage.Type.General, userId, "Cannot join the group while group members are close to danger!");
         return;
      }

      int activeTreasureSiteCount = 0;
      foreach (TreasureSite site in oldInstance.treasureSites) {
         if (site.isActive()) {
            activeTreasureSiteCount++;
         }
      }

      // If the sea instance has been cleared and there are active treasure sites, we only need to recreate the treasure site instances
      if (oldInstance.aliveNPCEnemiesCount == 0 && activeTreasureSiteCount > 0) {
         foreach (TreasureSite site in oldInstance.treasureSites) {
            Instance oldTreasureSiteInstance = InstanceManager.self.getInstance(site.destinationInstanceId);
            if (oldTreasureSiteInstance != null) {
               Instance newTreasureSiteInstance = InstanceManager.self.createNewInstance(oldTreasureSiteInstance.areaKey, false, oldTreasureSiteInstance.voyageId, voyageGroup.members.Count + 1, oldTreasureSiteInstance.leagueExitAreaKey, oldTreasureSiteInstance.leagueExitSpawnKey, oldTreasureSiteInstance.leagueExitFacingDirection);
               InstanceManager.self.removeEmptyInstance(oldTreasureSiteInstance);
               site.destinationInstanceId = newTreasureSiteInstance.id;
            }
         }

         // Add the new member to the group
         VoyageGroupManager.self.addUserToGroup(voyageGroup, userId, userName);

         return;
      }
      
      // Create the new league instance
      StartCoroutine(CO_CreateLeagueInstance(oldVoyage.leagueIndex, oldVoyage.biome, voyageGroup.members.Count + 1, ServerNetworkingManager.self.server.networkedPort.Value, null, (newVoyage) => onRecreateLeagueInstanceSuccess(newVoyage, oldInstance, groupId, userId, userName), oldVoyage.leagueRandomSeed, oldVoyage.areaKey, oldVoyage.leagueExitAreaKey, oldVoyage.leagueExitSpawnKey, oldVoyage.leagueExitFacingDirection));
   }

   [Server]
   private void onRecreateLeagueInstanceSuccess (Voyage newVoyage, Instance oldInstance, int groupId, int userId, string userName) {
      if (VoyageGroupManager.self.tryGetGroupById(groupId, out VoyageGroupInfo voyageGroup)) {
         // Relink the group to the new instance
         voyageGroup.voyageId = newVoyage.voyageId;

         // Add the new member to the group
         VoyageGroupManager.self.addUserToGroup(voyageGroup, userId, userName);
      }

      // Remove treasure sites (if any)
      foreach (TreasureSite site in oldInstance.treasureSites) {
         Instance treasureSiteInstance = InstanceManager.self.getInstance(site.destinationInstanceId);
         if (treasureSiteInstance != null) {
            InstanceManager.self.removeEmptyInstance(treasureSiteInstance);
         }
      }

      // Remove the old instance
      if (oldInstance != null) {
         InstanceManager.self.removeEmptyInstance(oldInstance);
      }
   }
   
   [Server]
   private IEnumerator CO_CreateLeagueInstance (int leagueIndex, Biome.Type biome, int difficulty, int serverPort, Action onFailureAction, Action<Voyage> onSuccessAction, int randomSeed = -1, string areaKey = "", string exitAreaKey = "", string exitSpawnKey = "", Direction exitFacingDirection = Direction.South) {
      // Get a new voyage id from the master server
      RpcResponse<int> response = ServerNetworkingManager.self.getNewVoyageId();
      while (!response.IsDone) {
         yield return null;
      }
      int voyageId = response.Value;

      if (string.IsNullOrEmpty(areaKey)) {
         if (Voyage.isLastLeagueMap(leagueIndex)) {
            // The last league map is always a boss
            int totalBossAreas = getLeagueSeaBossAreaKeys(biome).Count;
            areaKey = getLeagueSeaBossAreaKeys(biome)[UnityEngine.Random.Range(0, totalBossAreas)];
         } else {
            // Get the list of league maps
            List<string> mapList = getLeagueAreaKeys(biome);

            if (mapList.Count == 0) {
               D.error("No league maps available!");
               onFailureAction?.Invoke();
               yield break;
            }

            // Generate a random list of league maps indexes to ensure there is no repetition in the same league
            List<int> mapIndexes = new List<int>();
            for (int i = 0; i < mapList.Count; i++) {
               mapIndexes.Add(i);
            }

            // The seed will be saved in the voyage to generate the same map series when warping to the next map
            if (randomSeed < 0) {
               randomSeed = new System.Random().Next();
            }

            System.Random r = new System.Random(randomSeed);
            mapIndexes = mapIndexes.OrderBy(x => r.Next()).ToList();

            // Pick the index corresponding to the league index
            if (mapIndexes[leagueIndex] < mapList.Count) {
               areaKey = mapList[mapIndexes[leagueIndex]];
            } else {
               D.error("Not enough league maps. Some will be repeated!");
               areaKey = mapList[UnityEngine.Random.Range(0, mapList.Count)];
            }
         }
      }

      Voyage parameters = new Voyage {
         areaKey = areaKey,
         isPvP = false,
         isLeague = true,
         leagueIndex = leagueIndex,
         leagueRandomSeed = randomSeed,
         leagueExitAreaKey = exitAreaKey,
         leagueExitSpawnKey = exitSpawnKey,
         leagueExitFacingDirection = exitFacingDirection,
         biome = biome,
         difficulty = difficulty
      };

      // Launch the creation of the new voyage instance
      if (leagueIndex == 0) {
         // The first instance is created in the best available server
         requestVoyageInstanceCreation(voyageId, parameters);
      } else {
         // Keep all instances of the same league in the same server
         ServerNetworkingManager.self.createVoyageInstanceInServer(serverPort, voyageId, parameters);
      }

      // Wait until the voyage instance has been created
      Voyage voyage = null;
      while (!tryGetVoyage(voyageId, out voyage)) {
         yield return null;
      }

      // Execute the custom action
      onSuccessAction?.Invoke(voyage);
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

   public void requestExitCompletedLeague () {
      if (Global.player == null) {
         return;
      }

      Global.player.rpc.Cmd_RequestExitCompletedLeague();
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

   // The last id used to create a voyage
   private int _lastVoyageId = 0;

   #endregion
}
