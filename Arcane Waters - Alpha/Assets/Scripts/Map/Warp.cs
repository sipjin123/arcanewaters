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

   // The warp event that can be observed by external classes
   public WarpEvent warpEvent = new WarpEvent();

   #endregion

   void Awake () {
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

      if (canPlayerUseWarp(player)) {
         // If a player entered this warp on the server, move them
         if (player.isServer && player.connectionToClient != null) {
            // Lets give a bit more time for the client to perform any visual animations for warping
            StartCoroutine(CO_ExecWarpServer(player, 0.5f));
         }

         // If a player is client, show loading screen and stop the player
         if (player.isLocalPlayer) {
            // If it's a custom map, we have to own it, otherwise let server prompt us with map selection panel
            if (AreaManager.self.tryGetCustomMapManager(areaTarget, out CustomMapManager customMapManager)) {
               if (!customMapManager.canUserWarpInto(player, areaTarget, out System.Action<NetEntity> _)) {
                  return;
               }
            }

            player.setupForWarpClient();

            if (PanelManager.self.loadingScreen != null) {
               PanelManager.self.loadingScreen.show(LoadingScreen.LoadingType.MapCreation, SpotFader.self, SpotFader.self);
            }
         }
      }
   }

   [ServerOnly]
   private IEnumerator CO_ExecWarpServer (NetEntity player, float delay) {
      // If player is a ship, stop him from moving, if he is a human, client will handle it
      if (player is PlayerShipEntity) {
         player.getRigidbody().drag = 1000;
      }

      yield return new WaitForSeconds(delay);

      player.spawnInNewMap(areaTarget, spawnTarget, newFacingDirection);
      warpEvent.Invoke(player);
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         switch (field.k.ToLower()) {
            case DataField.WARP_TARGET_MAP_KEY:
               if (field.tryGetIntValue(out int id)) {
                  areaTarget = AreaManager.self.getAreaName(id);
               } else {
                  areaTarget = field.v;
               }
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

      updateArrow();
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
         SpriteRenderer ren = GetComponentInChildren<SpriteRenderer>();
         if (ren != null) {
            ren.sprite = arrowSprite;
         }
      } else {
         D.warning("Could not find sprite for warp arrow. Target sprite name: " + spriteName);
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

   private bool canPlayerUseWarp (NetEntity player) {
      if (string.IsNullOrEmpty(areaTarget) || string.IsNullOrEmpty(spawnTarget)) {
         return false;
      }

      // Check if a treasure site is controlling the warp in this instance
      TreasureSite site;
      if (_treasureSites.TryGetValue(player.instanceId, out site)) {
         // Verify that the player is allowed to use the warp
         if (site != null && !(VoyageManager.isInGroup(player) && site.isCaptured() && site.voyageGroupId == player.voyageGroupId)) {
            return false;
         }
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
