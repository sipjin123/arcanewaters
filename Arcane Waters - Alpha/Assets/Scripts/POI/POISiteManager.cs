using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System;
using MLAPI.Messaging;

public class POISiteManager : MonoBehaviour
{

   public static POISiteManager self;

   public void Awake () {
      self = this;
   }

   public void Start () {
      InvokeRepeating(nameof(clearPOISites), 10f, 5f);
   }

   [Server]
   public void warpUserToPOIArea (NetEntity player, string areaKey, string spawnKey, Direction facingDirection) {
      StartCoroutine(CO_CreatePOIInstanceAndWarpUser(player, areaKey, spawnKey, facingDirection));
   }

   [Server]
   private IEnumerator CO_CreatePOIInstanceAndWarpUser (NetEntity player, string areaKey, string spawnKey, Direction facingDirection) {
      int userId = player.userId;

      // Check if the user is in a group
      if (!VoyageGroupManager.self.tryGetGroupByUser(userId, out VoyageGroupInfo groupInfo)) {
         // Create a new group for the user
         yield return VoyageGroupManager.self.CO_CreateGroup(userId, -1, true, result => groupInfo = result);
      }
      
      int bestServerPort = -1;

      // Check if the group is already linked to an instance
      if (groupInfo.voyageId > 0 && VoyageManager.self.tryGetVoyage(groupInfo.voyageId, out Voyage currentVoyage)) {
         // Check if the instance is not a POI
         if (!VoyageManager.isPOIArea(currentVoyage.areaKey)) {
            // Remove the user from group
            VoyageGroupManager.self.removeUserFromGroup(groupInfo, userId);

            // Create a new group for the user
            yield return VoyageGroupManager.self.CO_CreateGroup(userId, -1, true, result => groupInfo = result);
         }
         // If the group is already linked to a POI instance
         else {
            // Stay in the same server
            if (ServerNetworkingManager.self.tryGetServerHostingVoyage(currentVoyage.voyageId, out NetworkedServer bestServer)) {
               bestServerPort = bestServer.networkedPort.Value;
            }
         }
      }

      if (bestServerPort < 0) {
         // Get the best server to host the POI Site
         NetworkedServer bestServer = ServerNetworkingManager.self.getRandomServerWithLeastAssignedPlayers();
         if (bestServer != null) {
            bestServerPort = bestServer.networkedPort.Value;
         }
      }

      // Request the creation of the POI instance in the best server
      ServerNetworkingManager.self.createPOIInstanceInServer(bestServerPort, groupInfo.groupId, areaKey);

      // Wait for the group to be linked to any voyage instance in the POI site
      Voyage voyage = null;
      while (!VoyageManager.self.tryGetVoyageForGroup(groupInfo.groupId, out voyage)) {
         yield return null;
      }

      // Warp the user
      if (player != null) {
         player.spawnInNewMap(voyage.voyageId, areaKey, spawnKey, facingDirection);
      }
   }

   [Server]
   public void createPOIInstanceInThisServer (int voyageGroupId, string areaKey) {
      StartCoroutine(CO_CreatePOIInstanceInThisServer(voyageGroupId, areaKey));
   }

   [Server]
   private IEnumerator CO_CreatePOIInstanceInThisServer (int voyageGroupId, string areaKey) {
      // Get the group requesting the creation
      if (!VoyageGroupManager.self.tryGetGroupById(voyageGroupId, out VoyageGroupInfo groupInfo)) {
         D.error($"Could not find the group {voyageGroupId} when creating a POI instance in area {areaKey}");
         yield break;
      }

      POISite site = null;

      // Check if the group is already linked to a POI instance
      if (tryGetPOISiteForGroup(groupInfo.groupId, out site)) {
         // Check if there is already an instance for the requested area
         if (site.voyageInstanceSet.TryGetValue(areaKey, out int existingVoyageId)) {
            // Link the group to the existing instance - there is no need to create a new one
            groupInfo.voyageId = existingVoyageId;
            VoyageGroupManager.self.updateGroup(groupInfo);
            yield break;
         }
      }

      if (site == null) {
         // Create a new POI Site object
         site = new POISite();
         site.groupId = groupInfo.groupId;
         _poiSiteList.Add(site);
      }

      // Get a new voyage id from the master server
      RpcResponse<int> response = ServerNetworkingManager.self.getNewVoyageId();
      while (!response.IsDone) {
         yield return null;
      }
      int voyageId = response.Value;

      // At this point, it is possible that another group member already created the new instance, in which case this creation is discarded
      if (VoyageManager.self.tryGetVoyageForGroup(groupInfo.groupId, out Voyage v) && string.Equals(v.areaKey, areaKey)) {
         yield break;
      }

      Voyage parameters = new Voyage {
         areaKey = areaKey,
         isPvP = false,
         isLeague = false,
         biome = AreaManager.self.getDefaultBiome(areaKey),
         difficulty = groupInfo.members.Count
      };

      // Create the instance in this server
      VoyageManager.self.createVoyageInstance(voyageId, parameters);

      // Set the voyage id in the group
      groupInfo.voyageId = voyageId;
      VoyageGroupManager.self.updateGroup(groupInfo);

      // Add the instance to the POI site
      site.voyageInstanceSet[areaKey] = voyageId;
   }

   [Server]
   public void clearPOISites () {
      List<POISite> siteToRemoveList = new List<POISite>();
      foreach (POISite site in _poiSiteList) {
         // If the group doesn't exist anymore, we can remove the site
         if (!VoyageGroupManager.self.tryGetGroupById(site.groupId, out VoyageGroupInfo groupInfo)) {
            siteToRemoveList.Add(site);
            continue;
         }

         // Check that all the group members are assigned to non-POI areas
         bool allGroupsOutsidePOI = true;
         foreach (int userId in groupInfo.members) {
            // If the user is not in this server, then he is outside of the POI site (which is fully hosted in the same server)
            if (!ServerNetworkingManager.self.server.assignedUserIds.TryGetValue(userId, out AssignedUserInfo assignedUserInfo)) {
               continue;
            }

            if (!VoyageManager.isPOIArea(assignedUserInfo.areaKey)) {
               continue;
            }

            allGroupsOutsidePOI = false;
            break;
         }

         if (allGroupsOutsidePOI) {
            // Delete the POI site
            siteToRemoveList.Add(site);

            // Verify that the group is linked to a POI instance
            if (VoyageManager.self.tryGetVoyage(groupInfo.voyageId, out Voyage voyage) && VoyageManager.isPOIArea(voyage.areaKey)) {
               // Unlink the group from the instance
               groupInfo.voyageId = -1;
               VoyageGroupManager.self.updateGroup(groupInfo);
            }
         }
      }

      foreach (POISite site in siteToRemoveList) {
         _poiSiteList.Remove(site);
      }
   }

   [Server]
   public bool tryGetInstanceForGroup (int groupId, string areaKey, out Instance instance) {
      instance = null;
      if (tryGetPOISiteForGroup(groupId, out POISite site) && site.voyageInstanceSet.TryGetValue(areaKey, out int voyageId) && VoyageManager.self.tryGetVoyage(voyageId, out Voyage voyage)) {
         instance = InstanceManager.self.getInstance(voyage.instanceId);
         return instance != null;
      }
      return false;
   }

   [Server]
   public bool doesPOISiteExistForInstance (int voyageId) {
      return _poiSiteList.Any(site => site.voyageInstanceSet.ContainsValue(voyageId));
   }

   [Server]
   public bool tryGetPOISiteForGroup (int groupId, out POISite site) {
      site = _poiSiteList.FirstOrDefault(s => s.groupId == groupId);
      return site != null;
   }

   // The list of POI sites hosted in this server
   private List<POISite> _poiSiteList = new List<POISite>();
}