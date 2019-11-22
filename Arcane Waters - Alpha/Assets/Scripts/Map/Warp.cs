using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Warp : MonoBehaviour {
   #region Public Variables

   // The spawn for this warp
   public Spawn.Type spawnTarget;

   // The facing direction we should have after spawning
   public Direction newFacingDirection = Direction.South;

   #endregion

   void OnTriggerEnter2D (Collider2D other) {
      NetEntity player = other.transform.GetComponent<NetEntity>();

      // Make sure the player meets the requirements to use this warp
      if (player == null || !meetsRequirements(player)) {
         return;
      }

      // If a player entered this warp on the server, move them
      if (player.isServer && player.connectionToClient != null) {
         Spawn spawn = SpawnManager.self.getSpawn(this.spawnTarget);
         Debug.Log("Starting warp to target area: " + spawn.AreaKey);
         player.spawnInNewMap(spawn.AreaKey, spawn, newFacingDirection);
      }
   }

   protected bool meetsRequirements (NetEntity player) {
      Spawn spawn = SpawnManager.self.getSpawn(this.spawnTarget);
      int currentStep = TutorialManager.getHighestCompletedStep(player.userId) + 1;

      // We can't warp to the sea until we've gotten far enough into the tutorial
      if (Area.isSea(spawn.AreaKey) && currentStep < (int) Step.HeadToDocks) {
         return false;
      }
      if (spawnTarget == Spawn.Type.HouseExit && currentStep == (int) Step.GetDressed) {
         ServerMessageManager.sendError(ErrorMessage.Type.Misc, player, "You need to get dressed before leaving the house!");
         return false;
      }

      // We can't warp into the treasure site until we clear the gate
      if (Area.isTreasureSite(spawn.AreaKey) && currentStep < (int) Step.EnterTreasureSite) {
         return false;
      }

      return true;
   }

   #region Private Variables

   #endregion
}
