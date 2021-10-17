using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;

public class PvpCaptureTarget : SeaEntity {
   #region Public Variables

   // A reference to the target holder that spawned us
   public PvpCaptureTargetHolder targetHolder;

   // The renderer displaying the capture target
   public SpriteRenderer mainRenderer;

   // The renderer displaying the spinning arrow above the target when dropped
   public SpriteRenderer spinningArrowRenderer;

   // The renderer displaying the 'held' icon for this capture target
   public SpriteRenderer heldIconRenderer;

   #endregion

   protected override void Start () {
      base.Start();

      findHolder();
      getHoldingEntity();
      updateParent();

      Minimap.self.addTreasureChestIcon(this.gameObject);
   }

   private void findHolder () {
      foreach (PvpCaptureTargetHolder holder in FindObjectsOfType<PvpCaptureTargetHolder>()) {
         if (holder.pvpTeam == this.pvpTeam && holder.instanceId == this.instanceId) {
            targetHolder = holder;
            break;
         }
      }

      if (targetHolder == null) {
         D.warning("PvpCaptureTarget couldn't find its holder.");
      }
   }

   protected override void Update () {
      base.Update();
      updateParent();
   }

   private void OnTriggerEnter2D (Collider2D collision) {
      checkCollisions(collision);
   }

   private void OnTriggerStay2D (Collider2D collision) {
      checkCollisions(collision);
   }

   private void checkCollisions (Collider2D collision) {
      // Handle picking up / returning on the server
      if (!isServer) {
         return;
      }

      // Only collide with other entities in our instance
      NetEntity otherNetEntity = collision.GetComponent<NetEntity>();
      if (otherNetEntity && otherNetEntity.instanceId != this.instanceId) {
         return;
      }

      // If a player ship is holding the capture target, check for collisions with the PvpCaptureTargetHolder, for capturing
      if (_holdingEntity != null && _holdingEntity.isPlayerShip()) {
         PvpCaptureTargetHolder targetHolder = collision.GetComponent<PvpCaptureTargetHolder>();
         if (targetHolder && targetHolder.pvpTeam != this.pvpTeam) {
            if (targetHolder.tryCaptureTarget(this, _holdingEntity.getPlayerShipEntity())) {
               _lastCaptureTime = (float) NetworkTime.time;
            }
         }

         // If a PvpCaptureTargetHolder is holding the capture target, check for collisions with an enemy player ship, for picking it up
      } else if (_holdingEntity != null && _holdingEntity.isPvpCaptureTargetHolder()) {
         PlayerShipEntity playerShip = collision.GetComponent<PlayerShipEntity>();
         if (playerShip && playerShip.pvpTeam != pvpTeam && pvpTeam != PvpTeamType.None && playerShip.pvpTeam != PvpTeamType.None && !playerShip.isDead()) {

            // Make sure enough time has passed since the last capture, and since it was last dropped
            if (getTimeSinceCaptured() > CAPTURE_COOLDOWN && getTimeSinceDropped() > DROPPED_PICKUP_COOLDOWN) {
               playerPickedUpTarget(playerShip);
            }
         }

         // If no one is holding the capture target, check for collisions with player ships, for picking it up or returning it
      } else if (_holdingEntity == null) {
         PlayerShipEntity playerShip = collision.GetComponent<PlayerShipEntity>();
         if (playerShip) {
            if (playerShip.pvpTeam == pvpTeam && pvpTeam != PvpTeamType.None && playerShip.pvpTeam != PvpTeamType.None && !playerShip.isDead()) {
               playerReturnedTarget(playerShip);
            } else if (playerShip.pvpTeam != pvpTeam && pvpTeam != PvpTeamType.None && playerShip.pvpTeam != PvpTeamType.None && !playerShip.isDead()) {
               
               // Check that enough time has passed since the target was last dropped
               if (getTimeSinceDropped() > DROPPED_PICKUP_COOLDOWN) {
                  playerPickedUpTarget(playerShip);
               }
            }
         }
      }
   }

   public void playerPickedUpTarget (PlayerShipEntity player) {
      // Attach to player
      attachToTransform(player.transform);

      // Add visual effect to player
      player.holdingPvpCaptureTarget = true;

      PvpGame activeGame = PvpManager.self.getGameWithInstance(instanceId);
      activeGame?.sendGameMessage(player.entityName + " picked up the " + faction.ToString() + "' treasure.", PvpAnnouncement.Priority.ObjectiveUpdate);

      // Assign holdingEntity
      assignHoldingEntity(player);

      player.heldPvpCaptureTarget = this;
      
      setMainVisibility(false);
      setHeldIconVisibility(true);
   }

   private void playerReturnedTarget (PlayerShipEntity player) {
      PvpGame activeGame = PvpManager.self.getGameWithInstance(instanceId);
      activeGame?.sendGameMessage(player.entityName + " returned the " + player.faction.ToString() + "' treasure.", PvpAnnouncement.Priority.ObjectiveUpdate);

      returnFlag();

      setMainVisibility(false);
      setHeldIconVisibility(false);
   }

   public void returnFlag () {
      targetHolder.returnTarget(this);
   }

   private void playerDroppedTarget (PlayerShipEntity player) {
      PvpGame activeGame = PvpManager.self.getGameWithInstance(instanceId);
      activeGame?.sendGameMessage(player.entityName + " dropped the " + faction.ToString() + "' treasure.", PvpAnnouncement.Priority.ObjectiveUpdate);

      // Remove visual effect from player
      player.holdingPvpCaptureTarget = false;

      // Set holdingEntity to null
      assignHoldingEntity(null);

      // Detach from player
      transform.SetParent(targetHolder.transform);

      // Track when we were last dropped
      _lastDroppedTime = (float) NetworkTime.time;

      player.heldPvpCaptureTarget = null;

      setMainVisibility(true);
      setHeldIconVisibility(false);
   }

   private float getTimeSinceCaptured () {
      return (float)NetworkTime.time - _lastCaptureTime;
   }

   private float getTimeSinceDropped () {
      return (float) NetworkTime.time - _lastDroppedTime;
   }

   public void onPlayerBoosted (PlayerShipEntity player) {
      if (getHoldingEntity() == player) {
         playerDroppedTarget(player);
      }
   }

   public void onPlayerDied (PlayerShipEntity player) {
      if (getHoldingEntity() == player) {
         playerDroppedTarget(player);
      }
   }

   private void updateParent () {
      _holdingEntity = getHoldingEntity();

      // If we should be attached to a player, attach to a player
      if (_holdingEntity is PlayerShipEntity && transform.parent != _holdingEntity.transform) {
         attachToTransform(_holdingEntity.transform);
         setMainVisibility(false);
         setHeldIconVisibility(true);

      // If we should be attached to the target holder, attach to it
      } else if (_holdingEntity == targetHolder && transform.parent != targetHolder.targetHolderTransform) {
         attachToTransform(targetHolder.targetHolderTransform);
         setMainVisibility(false);
         setHeldIconVisibility(false);

         // If we should be dropped, make sure we are dropped
      } else if (_holdingEntity == null && transform.parent != targetHolder.targetHolderDroppedTransform) {
         transform.SetParent(targetHolder.targetHolderDroppedTransform);
         setMainVisibility(true);
         setHeldIconVisibility(false);
      }

      // If we are back at base, or held by a player, ensure our local position is zeroed
      if (transform.parent != targetHolder.targetHolderDroppedTransform) {
         Util.setLocalXY(transform, Vector3.zero);
      }
   }

   private void setMainVisibility (bool value) {
      DOTween.Kill(mainRenderer);
      DOTween.Kill(spinningArrowRenderer);
      if (value) {
         transform.localScale = Vector3.one * 0.01f;
         transform.DOScale(1.0f, 0.5f).SetEase(Ease.OutElastic);

         Util.setAlpha(mainRenderer, 0.0f);
         mainRenderer.DOFade(1.0f, 0.2f);

         Util.setAlpha(spinningArrowRenderer, 0.0f);
         spinningArrowRenderer.DOFade(1.0f, 0.4f);
      } else {
         Util.setAlpha(mainRenderer, 0.0f);
         Util.setAlpha(spinningArrowRenderer, 0.0f);
      }
   }

   private void setHeldIconVisibility (bool value) {
      DOTween.Kill(heldIconRenderer);
      if (value) {
         Util.setAlpha(heldIconRenderer, 0.0f);
         heldIconRenderer.DOFade(1.0f, 0.25f);
      } else {
         Util.setAlpha(heldIconRenderer, 1.0f);
         heldIconRenderer.DOFade(0.0f, 0.25f);
      }
   }

   public void attachToTransform (Transform newParent) {
      transform.SetParent(newParent);
      Util.setLocalXY(transform, Vector3.zero);
   }

   public void assignHoldingEntity (SeaEntity newHoldingEntity) {
      _holdingEntity = newHoldingEntity;

      if (newHoldingEntity == null) {
         _holdingEntityNetId = 0;
      } else {
         _holdingEntityNetId = newHoldingEntity.netId;
      }
   }

   public SeaEntity getHoldingEntity () {
      if (isServer) {
         return _holdingEntity;
      } else {
         _holdingEntity = MyNetworkManager.fetchEntityFromNetId<SeaEntity>(_holdingEntityNetId);
         return _holdingEntity;
      }
   }

   #region Private Variables

   // A reference to the entity currently holding the target
   private SeaEntity _holdingEntity = null;

   // The net id of the holding entity
   [SyncVar]
   private uint _holdingEntityNetId = 0;

   // The last time this flag was captured
   private float _lastCaptureTime = 0.0f;

   // The last time this flag was dropped
   private float _lastDroppedTime = 0.0f;

   // After the flag is captured, how long players have to wait to pick it up again
   private const float CAPTURE_COOLDOWN = 0.1f;

   // After the flag is dropped, how long players have to wait to pick it up again
   private const float DROPPED_PICKUP_COOLDOWN = 0.5f;

   #endregion
}
