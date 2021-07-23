using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PvpCaptureTarget : SeaEntity {
   #region Public Variables

   // A reference to the entity currently holding the target
   public SeaEntity holdingEntity = null;

   // A reference to the target holder that spawned us
   public PvpCaptureTargetHolder targetHolder;

   #endregion

   private void OnTriggerEnter2D (Collider2D collision) {
      // Handle picking up / returning on the server
      if (!isServer) {
         return;
      }

      // If a player ship is holding the capture target, check for collisions with the PvpCaptureTargetHolder, for capturing
      if (holdingEntity != null && holdingEntity.isPlayerShip()) {
         PvpCaptureTargetHolder targetHolder = collision.GetComponent<PvpCaptureTargetHolder>();
         if (targetHolder) {
            targetHolder.tryCaptureTarget(this, holdingEntity.getPlayerShipEntity());
         }

      // If a PvpCaptureTargetHolder is holding the capture target, check for collisions with an enemy player ship, for picking it up
      } else if (holdingEntity != null && holdingEntity.isPvpCaptureTargetHolder()) {
         PlayerShipEntity playerShip = collision.GetComponent<PlayerShipEntity>();
         if (playerShip && playerShip.pvpTeam != pvpTeam && pvpTeam != PvpTeamType.None) {
            playerPickedUpFlag(playerShip);
         }

      // If no one is holding the capture target, check for collisions with player ships, for picking it up or returning it
      } else if (holdingEntity == null) {
         PlayerShipEntity playerShip = collision.GetComponent<PlayerShipEntity>();
         if (playerShip) {
            if (playerShip.pvpTeam == pvpTeam && pvpTeam != PvpTeamType.None) {
               playerReturnedFlag(playerShip);
            } else if (playerShip.pvpTeam != pvpTeam && pvpTeam != PvpTeamType.None) {
               playerPickedUpFlag(playerShip);
            }
         }
      }
   }

   private void playerPickedUpFlag (PlayerShipEntity player) {
      // Assign holdingEntity
      holdingEntity = player;

      // Attach to player
      transform.SetParent(player.transform);

      // Add visual effect to player?

   }

   private void playerReturnedFlag (PlayerShipEntity player) {
      D.log(player.nameText.text + " returned the " + PvpGame.getTeamName(player.pvpTeam) + " team's flag.");
      targetHolder.returnTarget();
   }

   private void playerDroppedFlag (PlayerShipEntity player) {
      // Called on player death / dash

      // Remove visual effect from player

      // Set holdingEntity to null
      holdingEntity = null;

      // Detach from player
      transform.SetParent(targetHolder.transform);
   }

   #region Private Variables

   #endregion
}
