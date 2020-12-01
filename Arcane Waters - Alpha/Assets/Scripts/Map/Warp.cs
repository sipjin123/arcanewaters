using UnityEngine;
using MapCreationTool.Serialization;
using System.Collections;
using System.Collections.Generic;

public class Warp : MonoBehaviour, IMapEditorDataReceiver
{
   #region Public Variables

   // The area for this warp
   public string areaTarget;

   // The spawn for this warp
   public string spawnTarget;

   // Information about targeted map, can be null if unset
   public Map targetInfo;

   // The facing direction we should have after spawning
   public Direction newFacingDirection = Direction.South;

   // The animated arrow
   public GameObject arrow;

   // Hard coded quest index
   public const int GET_DRESSED_QUEST_INDEX = 1;
   public const int HEAD_TO_DOCKS_QUEST_INDEX = 8;
   public const int ENTER_TREASURE_SITE_QUEST_INDEX = 14;

   #endregion

   protected virtual void Awake () {
      _collider = GetComponent<BoxCollider2D>();
   }

   void Start () {
      try {
         InvokeRepeating("showOrHideArrow", Random.Range(0f, 1f), 0.5f);
      } catch {
         CancelInvoke();
      }
   }

   void OnTriggerEnter2D (Collider2D other) {
      NetEntity player = other.transform.GetComponent<NetEntity>();

      if (player == null) {
         return;
      }

      if (player.isLocalPlayer && canPlayerUseWarp(player)) {
         // If a player is client, show loading screen and stop the player         
         // If it's a custom map, we have to own it, otherwise let server prompt us with map selection panel
         if (!string.IsNullOrEmpty(areaTarget) && AreaManager.self.tryGetCustomMapManager(areaTarget, out CustomMapManager customMapManager)) {
            if (!customMapManager.canUserWarpInto(player, areaTarget, out System.Action<NetEntity> warpHandler)) {
               warpHandler?.Invoke(player);
               return;
            } 
         }

         player.setupForWarpClient();
         Global.player.rpc.Cmd_RequestWarp(areaTarget, spawnTarget);

         if (PanelManager.self.loadingScreen != null) {
            PanelManager.self.loadingScreen.show(LoadingScreen.LoadingType.MapCreation, PostSpotFader.self, PostSpotFader.self);
         }
      }
   }

   [ServerOnly]
   public void startWarpForPlayer (NetEntity player) {
      // Lets give a bit more time for the client to perform any visual animations for warping
      StartCoroutine(CO_ExecWarpServer(player, 0.5f));
   }

   [ServerOnly]
   private IEnumerator CO_ExecWarpServer (NetEntity player, float delay) {
      D.debug("Warping player: " + player.userId + " : To area: " + areaTarget + " : Spawn Target: " + spawnTarget);
      yield return new WaitForSeconds(delay);

      // Any warp inside a treasure site area is considered an exit towards the sea voyage area
      if (VoyageGroupManager.isInGroup(player) && VoyageManager.isTreasureSiteArea(player.areaKey)) {
         // Try to find the treasure site entrance (spawn) where the user is registered
         int voyageId = player.getInstance().voyageId;
         Instance destinationInstance = InstanceManager.self.getVoyageInstance(voyageId);
         string spawnTarget = "";

         foreach (TreasureSite treasureSite in destinationInstance.treasureSites) {
            if (treasureSite.playerListInSite.Contains(player.userId)) {
               treasureSite.playerListInSite.Remove(player.userId);
               spawnTarget = treasureSite.spawnTarget;
               break;
            }
         }

         player.spawnInNewMap(voyageId, destinationInstance.areaKey, spawnTarget, newFacingDirection);
      }
      // Check if the destination is a treasure site
      else if (VoyageGroupManager.isInGroup(player) && isWarpToTreasureSite(player.instanceId)) {
         TreasureSite treasureSite = getTreasureSite(player.instanceId);

         // Register this user as being inside the treasure site
         treasureSite.playerListInSite.Add(player.userId);

         int voyageId = player.getInstance().voyageId;
         player.spawnInNewMap(voyageId, treasureSite.destinationArea, treasureSite.destinationSpawn, newFacingDirection);
      } else {
         player.spawnInNewMap(areaTarget, spawnTarget, newFacingDirection);
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
      if (transform.parent.GetComponent<House>() == null) {
         arrow.transform.localPosition = -DirectionUtil.getVectorForDirection(newFacingDirection);
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
         } else {
            if (arrow.activeSelf) {
               arrow.SetActive(false);
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

   public bool canPlayerUseWarp (NetEntity player) {
      if (player.isClient && !player.isServer && player.isAboutToWarpOnClient) {
         return false;
      }

      if (player.isServer && player.isAboutToWarpOnServer) {
         return false;
      }

      // Inside a treasure site, all warps are considered an exit and are always active
      if (VoyageGroupManager.isInGroup(player) && VoyageManager.isTreasureSiteArea(player.areaKey)) {
         return true;
      }

      // Check if a treasure site is controlling the warp in this instance
      if (_treasureSites.TryGetValue(player.instanceId, out TreasureSite site)) {
         if (site != null && VoyageGroupManager.isInGroup(player) && site.isCaptured() && site.voyageGroupId == player.voyageGroupId) {
            return true;
         }
      }

      // If the warp has a static destination, it must be defined
      if (string.IsNullOrEmpty(areaTarget) || string.IsNullOrEmpty(spawnTarget)) {
         return false;
      }

      return true;
   }

   #region Private Variables

   // The the collider, which will trigger the warp to activate
   protected BoxCollider2D _collider;

   // The associated treasure site for each instance id, if any
   protected Dictionary<int, TreasureSite> _treasureSites = new Dictionary<int, TreasureSite>();

   #endregion
}
