﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System;
using MLAPI.Messaging;

public class GroupInstanceManager : GenericGameManager {

   #region Public Variables

   // Self
   public static GroupInstanceManager self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   public void startGroupInstanceManagement () {
   }

   /// <summary>
   /// Note that only the Master server can launch group instance creations. Use requestGroupInstanceCreation() to ensure the Master server handles it.
   /// </summary>
   [Server]
   public void createGroupInstance (int groupInstanceId, GroupInstance parameters) {
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
         parameters.difficulty = UnityEngine.Random.Range(1, GroupInstance.getMaxDifficulty() + 1);
      }

      // Create the area instance
      Instance instance = InstanceManager.self.createNewInstance(parameters.areaKey, false, true, groupInstanceId, parameters.isPvP, parameters.isLeague, parameters.leagueIndex, parameters.leagueRandomSeed, parameters.voyageExitAreaKey, parameters.voyageExitSpawnKey, parameters.voyageExitFacingDirection, parameters.difficulty, parameters.biome);

      // For pvp instances, create the associated pvp game
      if (parameters.isPvP) {
         PvpManager.self.createNewGameForPvpInstance(instance);
      }
   }

   [Server]
   public bool tryGetGroupInstance (int groupInstanceId, out GroupInstance groupInstance, bool includeTreasureSites = false) {
      groupInstance = default;

      // Search the instance in all the servers we know about
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         if (includeTreasureSites) {
            if (server.treasureSites.TryGetValue(groupInstanceId, out groupInstance)) {
               return true;
            }
         }

         if (server.groupInstances.TryGetValue(groupInstanceId, out groupInstance)) {
            return true;
         }
      }
      
      return false;
   }

   [Server]
   public bool tryGetGroupInstance (string areaKey, out GroupInstance groupInstance) {
      groupInstance = default;

      // Search the first active group instance in the given area
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         foreach (GroupInstance gI in server.groupInstances.Values) {
            if (string.Equals(gI.areaKey, areaKey, StringComparison.InvariantCultureIgnoreCase)) {
               groupInstance = gI;
               return true;
            }
         }
      }
      
      return false;
   }

   [Server]
   public bool tryGetGroupInstanceForGroup (int groupId, out GroupInstance groupInstance) {
      groupInstance = default;

      if (GroupManager.self.tryGetGroupById(groupId, out Group groupInfo)) {
         if (groupInfo.groupInstanceId > 0 && tryGetGroupInstance(groupInfo.groupInstanceId, out GroupInstance gI)) {
            groupInstance = gI;
            return true;
         }
      }
      
      return false;
   }

   [Server]
   public List<GroupInstance> getAllGroupInstances () {
      List<GroupInstance> groupInstances = new List<GroupInstance>();

      // Get all the group instances we know about in all the servers we know about
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         groupInstances.AddRange(server.groupInstances.Values);
      }

      return groupInstances;
   }

   [Server]
   public List<GroupInstance> getAllPvpInstances () {
      List<GroupInstance> groupInstances = new List<GroupInstance>();

      // Get all the pvp instances we know about in all the servers we know about
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         foreach (GroupInstance groupInstance in server.groupInstances.Values) {
            if (groupInstance.isPvP) {
               groupInstances.Add(groupInstance);
            }
         }
      }

      return groupInstances;
   }

   [Server]
   public int getPvpInstanceId (string areaKey) {
      // Search in all the group instances we know about in all the servers we know about
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         foreach (GroupInstance groupInstance in server.groupInstances.Values) {
            if (groupInstance.isPvP && groupInstance.areaKey == areaKey) {
               return groupInstance.instanceId;
            }
         }
      }

      return -1;
   }

   [Server]
   public List<GroupInstance> getAllTreasureSiteInstancesLinkedToVoyages () {
      List<GroupInstance> groupInstances = new List<GroupInstance>();

      // Get all the group instances we know about in all the servers we know about
      foreach (NetworkedServer server in ServerNetworkingManager.self.servers) {
         groupInstances.AddRange(server.treasureSites.Values);
      }

      return groupInstances;
   }

   [Server]
   public List<GroupInstance> getAllOpenGroupInstances () {
      // Get the number of groups in all group instances
      Dictionary<int, int> allGroupCount = GroupManager.self.getGroupCountInAllGroupInstances();

      // Select the group instances that are open to new groups
      List<GroupInstance> allOpenGroupInstances = new List<GroupInstance>();
      foreach (GroupInstance groupInstance in getAllGroupInstances()) {
         allGroupCount.TryGetValue(groupInstance.groupInstanceId, out int groupCount);
         if (isGroupInstanceOpenToNewGroups(groupInstance, groupCount) && !groupInstance.isLeague) {
            allOpenGroupInstances.Add(groupInstance);
         }
      }

      return allOpenGroupInstances;
   }

   [Server]
   public bool doesGroupInstanceExists (int groupInstanceId) {
      return tryGetGroupInstance(groupInstanceId, out GroupInstance groupInstance);
   }

   public static bool isAnyGroupSpecificArea (string areaKey) {
      return isAnyLeagueArea(areaKey) || isTreasureSiteArea(areaKey) || isPvpArenaArea(areaKey) || isPOIArea(areaKey);
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
   public static bool isGroupInstanceOpenToNewGroups (GroupInstance groupInstance) {
      return isGroupInstanceOpenToNewGroups(groupInstance, GroupManager.self.getGroupCountInGroupInstance(groupInstance.groupInstanceId));
   }

   [Server]
   public static bool isGroupInstanceOpenToNewGroups (GroupInstance groupInstance, int groupCount) {
      // Calculate the time left until the group instance closes to new groups
      DateTime instanceCreationDate = DateTime.FromBinary(groupInstance.creationDate);
      TimeSpan timeLeft = instanceCreationDate.AddSeconds(GroupInstance.INSTANCE_OPEN_DURATION).Subtract(DateTime.UtcNow);

      // Check that the group instance is still in the open phase
      if (timeLeft.TotalSeconds <= 0) {
         return false;
      } else {
         // Check if the maximum number of groups has been reached
         if (groupCount >= GroupInstance.getMaxGroupsPerInstance(groupInstance.difficulty)) {
            return false;
         } else {
            return true;
         }
      }
   }

   [Server]
   public void registerUserInTreasureSite (int userId, int groupInstanceId, int instanceId) {
      if (!InstanceManager.self.tryGetGroupInstance(groupInstanceId, out Instance seaGroupInstance)) {
         D.error(string.Format("Could not find the sea group instance to register a user in a treasure site. userId: {0}", userId));
         return;
      }

      // Search for the treasure site object that warps to the given instance
      foreach (TreasureSite treasureSite in seaGroupInstance.treasureSites) {
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
      if (!InstanceManager.self.tryGetGroupInstance(instance.groupInstanceId, out Instance seaGroupInstance)) {
         return;
      }

      foreach (TreasureSite treasureSite in seaGroupInstance.treasureSites) {
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

      Global.player.rpc.Cmd_RequestGroupInstanceListFromServer();
   }

   public void warpToVoyage (NetEntity entity) {
      if (Global.player == null || !Global.player.isClient || entity == null || !entity.isLocalPlayer) {
         return;
      }

      if (!Global.player.isAboutToWarpOnClient) {
         Global.player.setupForWarpClient();
         Global.player.rpc.Cmd_WarpToVoyage();
      }
   }

   public void returnToTownFromVoyage (NetEntity entity) {
      if (Global.player == null || !Global.player.isClient || entity == null || !entity.isLocalPlayer) {
         return;
      }

      Global.player.rpc.Cmd_ReturnToTownFromVoyage();
   }

   [Server]
   public void forceAdminWarpToGroupSpecificAreas (NetEntity admin, string newArea) {
      if (isAnyLeagueArea(newArea)) {
         // Try to warp the admin to his current group instance if the area parameter fits
         if (admin.tryGetGroupInstance(out GroupInstance groupInstance) && groupInstance.areaKey.Equals(newArea)) {
            admin.spawnInNewMap(groupInstance.groupInstanceId, newArea, Direction.South);
            return;
         }

         // If the destination is a league map, always create a new instance
         if (isAnyLeagueArea(newArea)) {
            if (GroupManager.self.tryGetGroupById(admin.groupId, out Group groupInfo)) {
               GroupManager.self.removeUserFromGroup(groupInfo, admin);
            }
            Instance instance = InstanceManager.self.getInstance(admin.instanceId);

            createLeagueInstanceAndWarpPlayer(admin, 0, instance.biome, -1, newArea);
            return;
         }

         // Find an active group instance in this area
         if (tryGetGroupInstance(newArea, out groupInstance)) {
            GroupManager.self.forceAdminJoinGroupInstance(admin, groupInstance.groupInstanceId);
         } else {
            ServerMessageManager.sendConfirmation(ConfirmMessage.Type.General, admin, "No active group instance found in area " + newArea + ". Use '/admin create_voyage' first.");
            return;
         }

         // Warp the admin to the group instance
         admin.spawnInNewMap(groupInstance.groupInstanceId, newArea, Direction.South);
         return;
      } else if (isTreasureSiteArea(newArea)) {
         if (admin.tryGetGroupInstance(out GroupInstance gI)) {
            // If the admin already belongs to a group instance, warp him directly - the treasure site instance will be created automatically
            admin.spawnInNewMap(gI.groupInstanceId, newArea, Direction.South);
         } else {
            Instance instance = InstanceManager.self.getInstance(admin.instanceId);
            StartCoroutine(CO_ForceAdminWarpToTreasureSite(admin, newArea, instance.biome));
         }
      } else if (isPOIArea(newArea)) {
         POISiteManager.self.warpUserToPOIArea(admin, newArea, "", Direction.South);
      }
   }

   [Server]
   private IEnumerator CO_ForceAdminWarpToTreasureSite (NetEntity admin, string treasureSiteAreaKey, Biome.Type biome) {
      // Get a new group instance id from the master server
      RpcResponse<int> response = ServerNetworkingManager.self.getNewGroupInstanceId();
      while (!response.IsDone) {
         yield return null;
      }
      int groupInstanceId = response.Value;

      // Make the admin and its group (if possible) join the group instance
      GroupManager.self.forceAdminJoinGroupInstance(admin, groupInstanceId);

      // Set the difficulty to the number of members in the group
      int difficulty = 1;
      if (admin.tryGetGroup(out Group groupInfo)) {
         difficulty = groupInfo.members.Count;
      }

      // Launch the creation of a random sea map instance, to be the parent of the treasure site instance
      GroupInstance parameters = new GroupInstance {
         areaKey = "",
         isPvP = false,
         isLeague = true,
         leagueIndex = GroupInstance.MAPS_PER_VOYAGE - 1,
         biome = biome,
         difficulty = difficulty
      };

      requestGroupInstanceCreation(groupInstanceId, parameters);

      // Wait until the group instance has been created
      while (!tryGetGroupInstance(groupInstanceId, out GroupInstance groupInstance)) {
         yield return null;
      }

      // Warp the admin to the treasure site area
      admin.spawnInNewMap(groupInstanceId, treasureSiteAreaKey, Direction.South);
   }

   [Server]
   public void requestGroupInstanceCreation (GroupInstance parameters) {
      // Only the master server can generate new group instance ids
      if (!ServerNetworkingManager.self.server.isMasterServer()) {
         ServerNetworkingManager.self.requestGroupInstanceCreation(parameters);
         return;
      }

      int groupInstanceId = getNewGroupInstanceId();
      requestGroupInstanceCreation(groupInstanceId, parameters);
   }

   [Server]
   public void requestGroupInstanceCreation (int groupInstanceId, GroupInstance parameters) {
      // Find the server with the least people
      NetworkedServer bestServer = ServerNetworkingManager.self.getRandomServerWithLeastAssignedPlayers();

      if (bestServer != null) {
         ServerNetworkingManager.self.createGroupInstanceInServer(bestServer.networkedPort.Value, groupInstanceId, parameters);
      }
   }

   [Server]
   public void createLeagueInstanceAndWarpPlayer (NetEntity player, int leagueIndex, Biome.Type biome, int randomSeed = -1, string areaKey = "", string exitAreaKey = "", string exitSpawnKey = "", Direction exitFacingDirection = Direction.South) {
      // Determine the league difficulty and hosting server
      int difficulty = 1;
      int serverPort = ServerNetworkingManager.self.server.networkedPort.Value;
      if (player.tryGetGroup(out Group groupInfo)) {
         // Keep all instances of the same league in the same server
         if (groupInfo.groupInstanceId > 0 && ServerNetworkingManager.self.tryGetServerHostingGroupInstance(groupInfo.groupInstanceId, out NetworkedServer hostingServer)) {
            serverPort = hostingServer.networkedPort.Value;
         }
         difficulty = groupInfo.members.Count;
      } else {
         NetworkedServer bestServer = ServerNetworkingManager.self.getRandomServerWithLeastAssignedPlayers();
         if (bestServer != null) {
            serverPort = bestServer.networkedPort.Value;
         }
      }

      StartCoroutine(CO_CreateLeagueInstance(leagueIndex, biome, difficulty, serverPort, () => player.rpc.Target_OnWarpFailed("No league maps available", false), (groupInstance) => warpPlayerToLeagueInstance(player, groupInstance), randomSeed, areaKey, exitAreaKey, exitSpawnKey, exitFacingDirection));
   }

   [Server]
   private void warpPlayerToLeagueInstance (NetEntity player, GroupInstance groupInstance) {
      if (player.tryGetGroup(out Group groupInfo)) {
         if (player.tryGetGroupInstance(out GroupInstance currentGroupInstance) && currentGroupInstance.leagueIndex == groupInstance.leagueIndex) {
            // It can happen that two group members launch the creation of the next league map at exactly the same time. If the next instance has already been assigned to the group, warp the user to that one and discard the one just created.
            player.spawnInNewMap(currentGroupInstance.groupInstanceId, currentGroupInstance.areaKey, Direction.South);
            return;
         }

         // Link the group to the instance
         groupInfo.groupInstanceId = groupInstance.groupInstanceId;
         GroupManager.self.updateGroup(groupInfo);

         if (GroupInstance.isFirstVoyageMap(groupInstance.leagueIndex)) {
            // At the creation of the first voyage map, clear powerups of all users
            foreach (int memberUserId in groupInfo.members) {
               NetEntity memberEntity = EntityManager.self.getEntity(memberUserId);
               if (memberEntity != null) {
                  PowerupManager.self.clearPowerupsForUser(memberUserId);
               }
            }

            // At the creation of the voyage, warp all the group members to the instance together
            foreach (int memberUserId in groupInfo.members) {
               NetEntity memberEntity = EntityManager.self.getEntity(memberUserId);
               if (memberEntity != null) {
                  if (memberEntity.userId == player.userId) {
                     memberEntity.spawnInNewMap(groupInstance.groupInstanceId, groupInstance.areaKey, Direction.South);
                  } else {
                     // Do logic here for allies that will be entering the league
                  }
               }
            }
         } else {
            player.spawnInNewMap(groupInstance.groupInstanceId, groupInstance.areaKey, Direction.South);
         }
      } else {
         // Create a new group for the player and warp him
         GroupManager.self.createGroup(player.userId, groupInstance.groupInstanceId, true);
         PowerupManager.self.clearPowerupsForUser(player.userId);
         player.spawnInNewMap(groupInstance.groupInstanceId, groupInstance.areaKey, Direction.South);
      }
   }

   [Server]
   public void recreateLeagueInstanceAndAddUserToGroup (int groupId, int userId, string userName) {
      if (!GroupManager.self.tryGetGroupById(groupId, out Group groupInfo)) {
         D.error("Error when recreating a league instance: could not find the group");
         return;
      }

      // Redirect the action to the server hosting the instance
      if (ServerNetworkingManager.self.tryGetServerHostingGroupInstance(groupInfo.groupInstanceId, out NetworkedServer hostingServer)) {
         if (hostingServer != ServerNetworkingManager.self.server) {
            ServerNetworkingManager.self.recreateLeagueInstanceAndAddUserToGroup(groupInfo.groupInstanceId, groupId, userId, userName);
            return;
         }
      } else {
         D.error("Error when recreating a league instance: could not find the server hosting the instance");
         return;
      }

      if (!tryGetGroupInstance(groupInfo.groupInstanceId, out GroupInstance oldGroupInstance)) {
         D.error("Error when recreating a league instance: could not find the group instance to recreate");
         return;
      }

      if (!oldGroupInstance.isLeague) {
         D.error("Error when recreating a league instance: the instance is not a league");
         return;
      }

      Instance oldInstance = InstanceManager.self.getInstance(oldGroupInstance.instanceId);
      if (oldInstance == null) {
         D.error("Error when recreating a league instance: could not find the local instance to recreate");
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
               Instance newTreasureSiteInstance = InstanceManager.self.createNewInstance(oldTreasureSiteInstance.areaKey, false, oldTreasureSiteInstance.groupInstanceId, groupInfo.members.Count + 1, oldTreasureSiteInstance.voyageExitAreaKey, oldTreasureSiteInstance.voyageExitSpawnKey, oldTreasureSiteInstance.voyageExitFacingDirection);
               InstanceManager.self.removeEmptyInstance(oldTreasureSiteInstance);
               site.destinationInstanceId = newTreasureSiteInstance.id;
            }
         }

         // Add the new member to the group
         GroupManager.self.addUserToGroup(groupInfo, userId, userName);

         return;
      }
      
      // Create the new league instance
      StartCoroutine(CO_CreateLeagueInstance(oldGroupInstance.leagueIndex, oldGroupInstance.biome, groupInfo.members.Count + 1, ServerNetworkingManager.self.server.networkedPort.Value, null, (newGroupInstance) => onRecreateLeagueInstanceSuccess(newGroupInstance, oldInstance, groupId, userId, userName), oldGroupInstance.leagueRandomSeed, oldGroupInstance.areaKey, oldGroupInstance.voyageExitAreaKey, oldGroupInstance.voyageExitSpawnKey, oldGroupInstance.voyageExitFacingDirection));
   }

   [Server]
   private void onRecreateLeagueInstanceSuccess (GroupInstance newGroupInstance, Instance oldInstance, int groupId, int userId, string userName) {
      if (GroupManager.self.tryGetGroupById(groupId, out Group groupInfo)) {
         // Relink the group to the new instance
         groupInfo.groupInstanceId = newGroupInstance.groupInstanceId;

         // Add the new member to the group
         GroupManager.self.addUserToGroup(groupInfo, userId, userName);
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
   private IEnumerator CO_CreateLeagueInstance (int leagueIndex, Biome.Type biome, int difficulty, int serverPort, Action onFailureAction, Action<GroupInstance> onSuccessAction, int randomSeed = -1, string areaKey = "", string exitAreaKey = "", string exitSpawnKey = "", Direction exitFacingDirection = Direction.South) {
      // Get a new group instance id from the master server
      RpcResponse<int> response = ServerNetworkingManager.self.getNewGroupInstanceId();
      while (!response.IsDone) {
         yield return null;
      }
      int groupInstanceId = response.Value;

      if (string.IsNullOrEmpty(areaKey)) {
         if (GroupInstance.isLastVoyageMap(leagueIndex)) {
            // The last map in a voyage is always a boss
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

            // The seed will be saved in the group instance to generate the same map series when warping to the next map
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

      GroupInstance parameters = new GroupInstance {
         areaKey = areaKey,
         isPvP = false,
         isLeague = true,
         leagueIndex = leagueIndex,
         leagueRandomSeed = randomSeed,
         voyageExitAreaKey = exitAreaKey,
         voyageExitSpawnKey = exitSpawnKey,
         voyageExitFacingDirection = exitFacingDirection,
         biome = biome,
         difficulty = difficulty
      };

      // Launch the creation of the new group instance
      if (leagueIndex == 0) {
         // The first instance is created in the best available server
         requestGroupInstanceCreation(groupInstanceId, parameters);
      } else {
         // Keep all instances of the same league in the same server
         ServerNetworkingManager.self.createGroupInstanceInServer(serverPort, groupInstanceId, parameters);
      }

      // Wait until the group instance has been created
      GroupInstance groupInstance = null;
      while (!tryGetGroupInstance(groupInstanceId, out groupInstance)) {
         yield return null;
      }

      // Execute the custom action
      onSuccessAction?.Invoke(groupInstance);
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

   public void requestExitCompletedVoyage () {
      if (Global.player == null) {
         return;
      }

      Global.player.rpc.Cmd_RequestExitCompletedVoyage();
   }

   [Server]
   public int getNewGroupInstanceId () {
      if (!ServerNetworkingManager.self.server.isMasterServer()) {
         D.error("Only the master server can generate new group instance ids.");
         return -1;
      }

      return ++_lastGroupInstanceId;
   }

   #region Private Variables

   // The last id used to create a group instance
   private int _lastGroupInstanceId = 0;

   #endregion
}