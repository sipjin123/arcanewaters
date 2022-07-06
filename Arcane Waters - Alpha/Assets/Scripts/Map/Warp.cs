using UnityEngine;
using MapCreationTool.Serialization;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using System;

public class Warp : MonoBehaviour, IMapEditorDataReceiver
{
   #region Public Variables

   // The area for this warp
   public string areaTarget;

   // The spawn for this warp
   public string spawnTarget;

   // Information about targeted map, can be null if unset
   [Obsolete("Andrius: I think it's best to not use this. This field has to be rebaked from map editor after some changes are made." +
      "'AreaManager.self.tryGetAreaInfo(areaKey, out Map mapInfo)' Should accomplish the same thing while being more dynamic about changes.")]
   public Map targetInfo;

   // The facing direction we should have after spawning
   public Direction newFacingDirection = Direction.South;

   // The animated arrow
   public GameObject arrow;

   // The circle
   public GameObject circle;

   // The blocked icon
   public SpriteRenderer blockedIcon;

   // Does this warp lead to a town
   public bool leadsToTown = false;

   // Does this warp lead to a land map
   public bool leadsToLand = false;

   // The canvas and text we use for town visual
   public Canvas townVisual = null;
   public TMPro.TextMeshProUGUI townVisualText = null;

   // Hard coded quest index
   public const int GET_DRESSED_QUEST_INDEX = 1;
   public const int HEAD_TO_DOCKS_QUEST_INDEX = 8;
   public const int ENTER_TREASURE_SITE_QUEST_INDEX = 14;

   #endregion

   protected virtual void Awake () {
      _collider = GetComponent<Collider2D>();
   }

   void Start () {
      blockedIcon.enabled = false;
      try {
         InvokeRepeating(nameof(showOrHideArrow), UnityEngine.Random.Range(0f, 1f), 0.5f);
      } catch {
         CancelInvoke();
      }
   }

   void OnTriggerEnter2D (Collider2D other) {
      NetEntity player = other.transform.GetComponent<NetEntity>();

      if (player == null) {
         return;
      }

      if (player.isLocalPlayer && canPlayerUseWarp(player) && !player.isAboutToWarpOnClient) {

         // If it's a custom map, we have to own it, otherwise let server prompt us with map selection panel
         if (!string.IsNullOrEmpty(areaTarget) && AreaManager.self.tryGetCustomMapManager(areaTarget, out CustomMapManager customMapManager)) {
            if (!customMapManager.canUserWarpInto(player, areaTarget, out System.Action<NetEntity> denyWarpHandler)) {
               denyWarpHandler?.Invoke(player);
               return;
            }
         }

         // If a player is client, show loading screen and stop the player 
         D.adminLog("Sending warp request to server", D.ADMIN_LOG_TYPE.Warp);
         player.setupForWarpClient();

         if (PanelManager.self.loadingScreen != null) {
            PanelManager.self.loadingScreen.show(LoadingScreen.LoadingType.MapCreation);

            // Play door sound effect, only when we're leaving an interior area
            Instance instanceRef = InstanceManager.self.getInstance(player.instanceId);
            if (AreaManager.self.isInteriorArea(instanceRef.areaKey)) {
               SoundEffectManager.self.playDoorSfx(SoundEffectManager.DoorAction.Open, instanceRef.biome, this.transform.position);
            }

            LoadingUtil.executeAfterFade(() => {
               if (Global.player) {
                  Global.player.rpc.Cmd_RequestWarp(areaTarget, spawnTarget);
               }
            });
         } else {
            Global.player.rpc.Cmd_RequestWarp(areaTarget, spawnTarget);
         }
      } else {
         D.adminLog("Failed to warp! " +
            "CanUseWarp?: {" + " : " + canPlayerUseWarp(player) + "} " +
            "IsAboutToWarpClientSide?: {" + player.isAboutToWarpOnClient + "}", D.ADMIN_LOG_TYPE.Warp);
      }
   }

   [ServerOnly]
   public void startWarpForPlayer (NetEntity player) {
      // Lets give a bit more time for the client to perform any visual animations for warping
      StartCoroutine(CO_ExecWarpServer(player, 0.5f));
   }

   [ServerOnly]
   private IEnumerator CO_ExecWarpServer (NetEntity player, float delay) {
      if (player == null) {
         D.adminLog("Cannot Process warp! Reference to player has been removed!", D.ADMIN_LOG_TYPE.Warp);
         yield return null;
      }

      D.adminLog("Warping player: " + player.userId + " : To area: " + areaTarget + " : Spawn Target: " + spawnTarget + " Delay: " + delay, D.ADMIN_LOG_TYPE.Warp);
      yield return new WaitForSeconds(delay);

      if (GroupInstanceManager.isPOIArea(areaTarget)) {
         POISiteManager.self.warpUserToPOIArea(player, areaTarget, spawnTarget, newFacingDirection);
      }
      // Any warp inside a treasure site area is considered an exit towards the sea voyage area
      else if (GroupManager.isInGroup(player) && GroupInstanceManager.isTreasureSiteArea(player.areaKey)) {
         D.adminLog("Successfully warp process", D.ADMIN_LOG_TYPE.Warp);

         // Try to find the treasure site entrance (spawn) where the user is registered
         int groupInstanceId = player.getInstance().groupInstanceId;

         // If the group instance cannot be found, warp the player to the starting town
         if (!InstanceManager.self.tryGetGroupInstance(groupInstanceId, out Instance seaGroupInstance)) {
            D.error(string.Format("Could not find the sea group instace when leaving a treasure site. userId: {0}, treasure site areaKey: {1}", player.userId, player.areaKey));
            D.debug("This player {" + player.userId + " " + player.entityName + "} could not find sea group instance after treasure site, returning to Town");

            player.spawnInNewMap(Area.STARTING_TOWN);
            yield break;
         }

         string spawnTarget = "";
         foreach (TreasureSite treasureSiteRef in seaGroupInstance.treasureSites) {
            if (treasureSiteRef.playerListInSite.Contains(player.userId)) {
               spawnTarget = treasureSiteRef.spawnTarget;
               break;
            }
         }

         player.spawnInNewMap(groupInstanceId, seaGroupInstance.areaKey, spawnTarget, newFacingDirection);
      }
      // Check if the destination is a treasure site
      else if (GroupManager.isInGroup(player) && isWarpToTreasureSite(player.instanceId)) {
         TreasureSite treasureSite = getTreasureSite(player.instanceId);

         // Register this user as being inside the treasure site
         treasureSite.playerListInSite.Add(player.userId);

         int groupInstanceId = player.getInstance().groupInstanceId;
         player.spawnInNewMap(groupInstanceId, treasureSite.destinationArea, treasureSite.destinationSpawn, newFacingDirection);
      }
      // Any warp inside a league map is considered an exit towards the next voyage map
      else if (GroupManager.isInGroup(player) && GroupInstanceManager.isAnyLeagueArea(player.areaKey)) {
         Instance instance = InstanceManager.self.getInstance(player.instanceId);

         // If the instance is not a group instance, warp the player to the starting town
         if (instance == null || instance.groupInstanceId <= 0) {
            D.error(string.Format("An instance in a league area is not a group instance. userId: {0}, league areaKey: {1}", player.userId, player.areaKey));
            D.debug("This player {" + player.userId + " " + player.entityName + "} is accessing instance that is not a group instance, returning to Town");
            player.spawnInNewMap(Area.STARTING_TOWN);
            yield break;
         }

         if (GroupInstance.isLastVoyageMap(instance.leagueIndex)) {
            // At the end of a voyage, warp to the town in the next biome
            player.spawnInBiomeHomeTown(Biome.getNextBiome(instance.biome));
         } else {
            if (!player.tryGetGroupInstance(out GroupInstance groupInstance)) {
               D.error("Error when retrieving the group instance during warp to next league instance.");
               player.rpc.sendError("Error when warping between league maps. Could not find the group instance.");
               D.debug("This player {" + player.userId + " " + player.entityName + "} could not find group instance during warp to league, returning to Town");
               player.spawnInNewMap(Area.STARTING_TOWN);
               yield break;
            }

            // If the group is already assigned to another group instance, the next instance has already been created and we simply warp the player to it
            if (groupInstance.groupInstanceId != instance.groupInstanceId) {
               player.spawnInNewMap(groupInstance.groupInstanceId, groupInstance.areaKey, Direction.South);
            } else {
               // Create the next league map and warp the player to it
               GroupInstanceManager.self.createLeagueInstanceAndWarpPlayer(player, instance.leagueIndex + 1, instance.biome, instance.leagueRandomSeed, "", groupInstance.voyageExitAreaKey, groupInstance.voyageExitSpawnKey, groupInstance.voyageExitFacingDirection);
            }
         }
      } else {
         bool isWarpingToPrivateCustomArea = CustomMapManager.isPrivateCustomArea(areaTarget);
         bool doesUserOwnUserSpecificArea = false;
         bool isUserInUserSpecificArea = CustomMapManager.isUserSpecificAreaKey(player.areaKey);
         bool isWarpingToGuildMap = areaTarget.Contains(CustomGuildMapManager.GROUP_AREA_KEY);
         bool isWarpingToGuildHouse = areaTarget.Contains(CustomGuildHouseManager.GROUP_AREA_KEY);

         if (isUserInUserSpecificArea) {
            int userIdOfMapOwner = CustomMapManager.getUserId(player.areaKey);
            doesUserOwnUserSpecificArea = userIdOfMapOwner == player.userId;
         }

         string visitedArea = areaTarget + "_user";
         if (isWarpingToGuildMap) {

            // If the player isn't in a guild, send them to their home town
            if (player.guildId <= 0) {
               player.spawnInBiomeHomeTown();
            } else {
               if (areaTarget == CustomGuildMapManager.GROUP_AREA_KEY) {
                  string targetArea = CustomGuildMapManager.getGuildSpecificAreaKey(player.guildId);
                  player.spawnInNewMap(targetArea, spawnTarget, newFacingDirection);
               } else {
                  player.spawnInNewMap(areaTarget, spawnTarget, newFacingDirection);
               }
            }
         } else if (isWarpingToGuildHouse) {

            // If the player isn't in a guild, send them to their home town
            if (player.guildId <= 0) {
               player.spawnInBiomeHomeTown();
            } else {
               if (areaTarget == CustomGuildHouseManager.GROUP_AREA_KEY) {
                  string targetArea = CustomGuildHouseManager.getGuildSpecificAreaKey(player.guildId);
                  player.spawnInNewMap(targetArea, spawnTarget, newFacingDirection);
               } else {
                  player.spawnInNewMap(areaTarget, spawnTarget, newFacingDirection);
               }
            }

         } else if (isWarpingToPrivateCustomArea && isUserInUserSpecificArea) {
            int ownerIdOfNextMap = -1;
            if (doesUserOwnUserSpecificArea) {
               visitedArea += player.userId;
               ownerIdOfNextMap = player.userId;
            } else {
               visitedArea += CustomMapManager.getUserId(player.areaKey);
               ownerIdOfNextMap = CustomMapManager.getUserId(player.areaKey);
            }

            if (areaTarget == CustomHouseManager.GROUP_AREA_KEY) {
               player.visitPrivateInstanceHouseById(ownerIdOfNextMap, "");
            } else if (areaTarget == CustomFarmManager.GROUP_AREA_KEY) {
               player.visitPrivateInstanceFarmById(ownerIdOfNextMap, "");
            }

            D.adminLog("VISIT NULL {" + areaTarget + "} VISITOR IS {" + player.userId + "}", D.ADMIN_LOG_TYPE.Visit);
         } else if (isWarpingToPrivateCustomArea && !isUserInUserSpecificArea) {
            if (areaTarget == CustomHouseManager.GROUP_AREA_KEY) {
               player.visitPrivateInstanceHouseById(player.userId, "");
            } else if (areaTarget == CustomFarmManager.GROUP_AREA_KEY) {
               player.visitPrivateInstanceFarmById(player.userId, "");
            } else {
               D.adminLog("VISIT NULL {" + areaTarget + "}", D.ADMIN_LOG_TYPE.Visit);
            }
         } else {
            player.spawnInNewMap(areaTarget, spawnTarget, newFacingDirection);
         }
      }
   }

   public void receiveData (DataField[] dataFields) {
      DataField targetMapField = null;

      foreach (DataField field in dataFields) {
         switch (field.k.ToLower()) {
            case DataField.WARP_TARGET_MAP_KEY:
               targetMapField = field;
               break;
            case DataField.WARP_TARGET_SPAWN_KEY:
               spawnTarget = field.v.Trim(' ');
               break;
            case DataField.WARP_ARRIVE_FACING_KEY:
               if (field.tryGetDirectionValue(out Direction dir)) {
                  newFacingDirection = dir;
               }
               break;
            case DataField.TARGET_MAP_INFO_KEY:
               targetInfo = field.objectValue<Map>();
               break;
         }
      }

      if (targetMapField != null) {
         setAreaTarget(targetMapField);
      }

      updateArrow();
   }

   public void setAreaTarget (DataField targetField) {
      if (targetField.tryGetIntValue(out int id)) {
         if (targetInfo != null && targetInfo.id == id) {
            areaTarget = targetInfo.name;
         } else {
            areaTarget = AreaManager.self.getAreaName(id);
         }
      } else {
         areaTarget = targetField.v;
      }

      leadsToTown = false;
      leadsToLand = false;
      if (AreaManager.self.tryGetAreaInfo(areaTarget, out Map map)) {
         if (map.specialType == Area.SpecialType.Town) {
            leadsToTown = true;
         }
         if (map.editorType == MapCreationTool.EditorType.Area) {
            leadsToLand = true;
         }
      }
   }

   public void updateTargetTownVisual (bool isInSeaArea) {
      Map map = null;
      bool shouldShow = isInSeaArea && AreaManager.self.tryGetAreaInfo(areaTarget, out map);

      if (townVisual != null) {
         townVisual.gameObject.SetActive(shouldShow);
         if (shouldShow) {
            if (townVisualText.text != null) {
               townVisualText.text = map.displayName;
            }

            Vector3 pos = Util.getDirectionFromFacing(newFacingDirection) * 5f;
            pos.z = townVisual.transform.localPosition.z;
            townVisual.transform.localPosition = pos;
         }
      }
   }

   /// <summary>
   /// Updates the arrow color and direction, based on this area type and target area type
   /// </summary>
   public void updateArrow () {
      string dir = newFacingDirection.ToString().ToLower();
      string color = "unrecognized";

      string thisArea = transform.GetComponentInParent<Area>()?.areaKey;
      if (thisArea != null) {
         if (AreaManager.self.isSeaArea(thisArea)) {
            color = "blue";
         } else {
            color = "gold";
         }
      }

      string spriteName = $"warp_{color}_{dir}";

      Sprite arrowSprite = ImageManager.getSprite("Map/Warp Arrows/" + spriteName);
      if (arrowSprite != null) {
         SpriteRenderer ren = arrow.GetComponent<SpriteRenderer>();
         if (ren != null) {
            ren.sprite = arrowSprite;
         }
      } else {
         D.warning("Could not find sprite for warp arrow. Target sprite name: " + spriteName);
      }

      // Adjust the position to point the warp visual, if we are not a house warp
      if (transform.parent != null && transform.parent.GetComponent<House>() == null) {
         if (arrow != null) {
            arrow.transform.localPosition = -DirectionUtil.getVectorForDirection(newFacingDirection);
         }
      }
   }

   private void showOrHideArrow () {
      try {
         if (Global.player == null) {
            return;
         }

         // Check if the local player can use the warp
         if (canPlayerUseWarp(Global.player)) {
            if (!arrow.activeSelf) {
               arrow.SetActive(true);
            }
            if (blockedIcon.enabled) {
               blockedIcon.enabled = false;
            }
         } else {
            if (arrow.activeSelf) {
               arrow.SetActive(false);
            }
            if (!blockedIcon.enabled && !Global.player.isAboutToWarpOnClient) {
               blockedIcon.enabled = true;
            }

            // In leagues, warps to inactive treasure sites are completely invisible
            if (_treasureSites.TryGetValue(Global.player.instanceId, out TreasureSite site)) {
               if (site != null && !site.isActive()) {
                  if (circle.activeSelf) {
                     circle.SetActive(false);
                  }
                  if (blockedIcon.enabled) {
                     blockedIcon.enabled = false;
                  }
               }
            }
         }
      } catch {
         CancelInvoke();
      }
   }

   public void setTreasureSite (int instanceId, TreasureSite treasureSite) {
      _treasureSites.Add(instanceId, treasureSite);
   }

   public void removeTreasureSite (int instanceId) {
      _treasureSites.Remove(instanceId);
   }

   public bool isWarpToTreasureSite (int instanceId) {
      return _treasureSites.ContainsKey(instanceId);
   }

   public TreasureSite getTreasureSite (int instanceId) {
      if (_treasureSites.TryGetValue(instanceId, out TreasureSite site)) {
         return site;
      }

      return null;
   }

   public bool hasCollider () {
      return _collider;
   }

   public Bounds getColliderBounds () {
      return _collider ? _collider.bounds : new Bounds();
   }

   public bool canPlayerUseWarp (NetEntity player) {
      if (player.isClient && !player.isServer && player.isAboutToWarpOnClient) {
         return false;
      }

      if (player.isServer && player.isAboutToWarpOnServer) {
         return false;
      }

      // Inside a treasure site, all warps are considered an exit and are always active
      if (GroupManager.isInGroup(player) && GroupInstanceManager.isTreasureSiteArea(player.areaKey)) {
         return true;
      }

      // Check if a treasure site is controlling the warp in this instance
      if (_treasureSites.TryGetValue(player.instanceId, out TreasureSite site)) {
         if (site != null && site.isActive() && GroupManager.isInGroup(player) && site.isCaptured() && site.isOwnedByGroup(player.groupId)) {
            return true;
         }
      }

      // In league maps, warps are only active when all enemies are defeated
      Instance instance = player.getInstance();
      if (instance != null && instance.isLeague) {
         // Check that all sea enemies in the instance are defeated
         if (instance.aliveNPCEnemiesCount != 0) {
            return false;
         }

         // Check that all enemies inside maps accessed through treasure sites in this instance have been defeated
         if (!instance.areAllTreasureSitesClearedOfEnemies) {
            return false;
         }

         // Skip demo restriction and destination checks if we are in a league map
         return true;
      }

      // If the warp has a static destination, it must be defined
      if (string.IsNullOrEmpty(areaTarget) || string.IsNullOrEmpty(spawnTarget)) {
         return false;
      }

      // Don't let Demo users warp outside Forest or Desert
      if (player.isDemoUser && !player.isAdmin()) {
         if (!AreaManager.self.tryGetAreaInfo(areaTarget, out Map map)) {
            return false;
         }

         if (!AdminGameSettingsManager.self.isBiomeLegalForDemoUser(map.biome)) {
            return false;
         }
      }

      return true;
   }

   #region Private Variables

   // The the collider, which will trigger the warp to activate
   protected Collider2D _collider;

   // The associated treasure site for each instance id, if any
   protected Dictionary<int, TreasureSite> _treasureSites = new Dictionary<int, TreasureSite>();

   #endregion
}
