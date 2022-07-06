using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System;
using MLAPI.Messaging;

public class POISiteManager : GenericGameManager
{
   // The delay after which a warp is considered failed and a notice is sent to the client
   public static float WARP_FAILED_TIMEOUT_DELAY = 10f;

   // The inactivity delay it takes for a POI site to be unlinked from a group and deleted
   public static float POI_SITE_DELAY_BEFORE_REMOVAL = 5f;

   public static POISiteManager self;

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   public void startPOISiteManagement () {
      InvokeRepeating(nameof(clearPOISites), 10f, 2f);
   }

   [Server]
   public void warpUserToPOIArea (NetEntity player, string areaKey, string spawnKey, Direction facingDirection) {
      StartCoroutine(CO_CreatePOIInstanceAndWarpUser(player, areaKey, spawnKey, facingDirection));

      // Set a timeout to remove the player from warping status, in case the process fails
      StartCoroutine(CO_OnWarpToPOIFailed(player, areaKey));
   }

   [Server]
   private IEnumerator CO_OnWarpToPOIFailed (NetEntity player, string areaKey) {
      yield return new WaitForSeconds(WARP_FAILED_TIMEOUT_DELAY);

      if (player != null) {
         player.rpc.Target_OnWarpFailed($"The warp to POI area {areaKey} failed for player {player.entityName} (timeout)", false);
      }
   }

   [Server]
   private IEnumerator CO_CreatePOIInstanceAndWarpUser (NetEntity player, string areaKey, string spawnKey, Direction facingDirection) {
      int userId = player.userId;

      // Check if the user is in a group
      if (!GroupManager.self.tryGetGroupByUser(userId, out Group groupInfo)) {
         // Create a new group for the user
         yield return GroupManager.self.CO_CreateGroup(userId, -1, true, result => groupInfo = result);
      }
      
      int bestServerPort = -1;

      // Check if the group is already linked to an instance
      if (groupInfo.groupInstanceId > 0 && GroupInstanceManager.self.tryGetGroupInstance(groupInfo.groupInstanceId, out GroupInstance currentGroupInstance)) {
         // Check if the instance is not a POI
         if (!GroupInstanceManager.isPOIArea(currentGroupInstance.areaKey)) {
            // Remove the user from group
            GroupManager.self.removeUserFromGroup(groupInfo, userId);

            // Create a new group for the user
            yield return GroupManager.self.CO_CreateGroup(userId, -1, true, result => groupInfo = result);
         }
         // If the group is already linked to a POI instance
         else {
            // Stay in the same server
            if (ServerNetworkingManager.self.tryGetServerHostingGroupInstance(currentGroupInstance.groupInstanceId, out NetworkedServer bestServer)) {
               bestServerPort = bestServer.networkedPort.Value;
            }
         }
      }

      if (bestServerPort < 0) {
         // Get the best server to host the POI Site
         NetworkedServer bestServer = ServerNetworkingManager.self.getFirstServerWithLeastAssignedPlayers();
         if (bestServer != null) {
            bestServerPort = bestServer.networkedPort.Value;
         }
      }

      // Ask the selected server to create the instance (if needed) and perform the warp
      ServerNetworkingManager.self.warpUserToPOIInstanceInServer(bestServerPort, groupInfo.groupId, userId, areaKey, spawnKey, facingDirection);
   }

   /// <summary>
   /// This function should not be called directly. Use warpUserToPOIArea to select the correct server.
   /// </summary>
   [Server]
   public void warpUserToPOIInstanceInThisServer (int groupId, int userId, string areaKey, string spawnKey, Direction facingDirection) {
      StartCoroutine(CO_WarpUserToPOIInstanceInThisServer(groupId, userId, areaKey, spawnKey, facingDirection));
   }

   [Server]
   private IEnumerator CO_WarpUserToPOIInstanceInThisServer (int groupId, int userId, string areaKey, string spawnKey, Direction facingDirection) {
      // Get the group requesting the creation
      if (!GroupManager.self.tryGetGroupById(groupId, out Group groupInfo)) {
         D.error($"Could not find the group {groupId} when creating a POI instance in area {areaKey}");
         yield break;
      }

      POISite site = null;

      // Check if the group is already linked to a site, and if there is already an instance for the requested area
      if (tryGetPOISiteForGroup(groupInfo.groupId, out site) && site.groupInstanceSet.TryGetValue(areaKey, out int existingGroupInstanceId)) {
         // Link the group to the existing instance - there is no need to create a new one
         groupInfo.groupInstanceId = existingGroupInstanceId;
         GroupManager.self.updateGroup(groupInfo);

         // Warp the user
         ServerNetworkingManager.self.warpUser(userId, existingGroupInstanceId, areaKey, facingDirection, spawnKey);
         yield break;
      }

      // Get a new group instance id from the master server
      RpcResponse<int> response = ServerNetworkingManager.self.getNewGroupInstanceId();
      while (!response.IsDone) {
         yield return null;
      }
      int groupInstanceId = response.Value;

      GroupInstance parameters = new GroupInstance {
         areaKey = areaKey,
         isPvP = false,
         isLeague = false,
         biome = AreaManager.self.getDefaultBiome(areaKey),
         difficulty = groupInfo.members.Count
      };

      // Create the instance in this server
      GroupInstanceManager.self.createGroupInstance(groupInstanceId, parameters);

      // Wait until the group instance is added to the server network data
      while (!GroupInstanceManager.self.doesGroupInstanceExists(groupInstanceId)) {
         yield return null;
      }

      // At this point, it is possible that another group member already created the new POI instance, in which case this instance is discarded
      if (GroupInstanceManager.self.tryGetGroupInstanceForGroup(groupInfo.groupId, out GroupInstance gI) && string.Equals(gI.areaKey, areaKey)) {
         // Warp the user
         ServerNetworkingManager.self.warpUser(userId, gI.groupInstanceId, areaKey, facingDirection, spawnKey);
         yield break;
      }

      // Set the group instance id in the group
      groupInfo.groupInstanceId = groupInstanceId;
      GroupManager.self.updateGroup(groupInfo);

      if (site == null) {
         // Create a new POI Site object
         site = new POISite(groupInfo.groupId);
         _poiSiteList.Add(site);
      }

      // Add the instance to the POI site
      site.groupInstanceSet[areaKey] = groupInstanceId;

      // Warp the user
      ServerNetworkingManager.self.warpUser(userId, groupInstanceId, areaKey, facingDirection, spawnKey);
   }

   [Server]
   public void clearPOISites () {
      if (!NetworkServer.active) {
         return;
      }

      // Update the active time of each site
      foreach (POISite site in _poiSiteList) {
         // If the group doesn't exist, the site is not active anymore
         if (!GroupManager.self.tryGetGroupById(site.groupId, out Group groupInfo)) {
            continue;
         }

         // Check that at least one group member is in a POI area
         foreach (int userId in groupInfo.members) {
            // If the user is not in this server, then he is outside of the POI site (which is fully hosted in the same server)
            if (!ServerNetworkingManager.self.server.assignedUserIds.TryGetValue(userId, out AssignedUserInfo assignedUserInfo)) {
               continue;
            }

            if (GroupInstanceManager.isPOIArea(assignedUserInfo.areaKey)) {
               site.lastActiveTime = Time.time;
               break;
            }
         }
      }

      // Select the sites that have been inactive long enough
      List<POISite> siteToRemoveList = new List<POISite>();
      foreach (POISite site in _poiSiteList) {
         if (Time.time > site.lastActiveTime + POI_SITE_DELAY_BEFORE_REMOVAL) {
            siteToRemoveList.Add(site);
         }
      }

      // Remove inactive sites
      foreach (POISite site in siteToRemoveList) {
         // Verify that the group is linked to a POI instance (any POI instance will be part of the current site being removed)
         if (GroupManager.self.tryGetGroupById(site.groupId, out Group groupInfo) && GroupInstanceManager.self.tryGetGroupInstance(groupInfo.groupInstanceId, out GroupInstance groupInstance) && GroupInstanceManager.isPOIArea(groupInstance.areaKey)) {
            // Unlink the group from the instance
            groupInfo.groupInstanceId = -1;
            GroupManager.self.updateGroup(groupInfo);
         }

         _poiSiteList.Remove(site);
      }
   }

   /// <summary>
   /// This can only be called in the server where the POI site and areas are hosted
   /// </summary>
   [Server]
   public bool tryGetInstanceForGroup (int groupId, string areaKey, out Instance instance) {
      instance = null;
      if (tryGetPOISiteForGroup(groupId, out POISite site) && site.groupInstanceSet.TryGetValue(areaKey, out int groupInstanceId)) {
         InstanceManager.self.tryGetGroupInstance(groupInstanceId, out instance);
      }
      return instance != null;
   }

   [Server]
   public bool doesPOISiteExistForInstance (int groupInstanceId) {
      return _poiSiteList.Any(site => site.groupInstanceSet.ContainsValue(groupInstanceId));
   }

   [Server]
   public bool tryGetPOISiteForGroup (int groupId, out POISite site) {
      site = _poiSiteList.FirstOrDefault(s => s.groupId == groupId);
      return site != null;
   }

   // The list of POI sites hosted in this server
   private List<POISite> _poiSiteList = new List<POISite>();
}