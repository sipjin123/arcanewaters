﻿using UnityEngine;
using MapCreationTool;
using MapCreationTool.Serialization;
using System.Collections.Generic;
using UnityEngine.Events;

public class Warp : MonoBehaviour, IMapEditorDataReceiver
{
   #region Public Variables

   // The area for this warp
   public string areaTarget;

   // The spawn for this warp
   public string spawnTarget;

   // The facing direction we should have after spawning
   public Direction newFacingDirection = Direction.South;

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

   void OnTriggerEnter2D (Collider2D other) {
      NetEntity player = other.transform.GetComponent<NetEntity>();

      if (player == null) {
         return;
      }

      // If a player entered this warp on the server, move them
      if (player.isServer && player.connectionToClient != null) {
         // Check if a treasure site is controlling the warp in this instance
         TreasureSite site;
         if (_treasureSites.TryGetValue(player.instanceId, out site)) {
            // Verify that the player is allowed to use the warp
            if (site != null && !(VoyageManager.isInVoyage(player) && site.isCaptured() && site.voyageGroupId == player.voyageGroupId)) {
               return;
            }
         }

         SpawnID spawnID = new SpawnID(areaTarget, spawnTarget);
         Vector2 localPos = SpawnManager.self.getSpawnLocalPosition(spawnID);
         player.spawnInNewMap(areaTarget, localPos, newFacingDirection);

         warpEvent.Invoke(player);
      }
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
            default:
               Debug.LogWarning($"Unrecognized data field key: {field.k}");
               break;
         }
      }

      updateArrow();
   }

   /// <summary>
   /// Updates the arrow color and direction, based on this area type and target area type
   /// </summary>
   public void updateArrow () {
      string thisArea = transform.GetComponentInParent<Area>()?.areaKey;
      EditorType? thisType = thisArea == null ? null : AreaManager.self.getAreaEditorType(thisArea);

      string dir = newFacingDirection.ToString().ToLower();
      string color = "unrecognized";

      if (thisType != null) {
         if (thisType == EditorType.Sea) {
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

   public void setTreasureSite (int instanceId, TreasureSite treasureSite) {
      _treasureSites.Add(instanceId, treasureSite);
   }

   public void removeTreasureSite (int instanceId) {
      _treasureSites.Remove(instanceId);
   }

   #region Private Variables

   // The the collider, which will trigger the warp to activate
   protected BoxCollider2D _collider;

   // The associated treasure site for each instance id, if any
   protected Dictionary<int, TreasureSite> _treasureSites = new Dictionary<int, TreasureSite>();

   #endregion
}
