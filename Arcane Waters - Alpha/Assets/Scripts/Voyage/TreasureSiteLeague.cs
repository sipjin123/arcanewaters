using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

public class TreasureSiteLeague : TreasureSite
{
   #region Public Variables

   // The index of the voyage instance in the league series, where this treasure site is located
   [SyncVar]
   public int leagueIndex = 0;

   #endregion

   public override void Update () {
      if (!isActive() && _spriteRenderer.enabled) {
         _spriteRenderer.enabled = false;
      }

      if (isServer && isActive()) {
         Instance instance = InstanceManager.self.getInstance(instanceId);

         // When all enemies in the instance are defeated, set the site as captured
         if (status != Status.Captured && instance.aliveNPCEnemiesCount == 0) {
            capturePoints = 1;
            status = Status.Captured;
            
            // Play the captured animation
            _animator.SetBool("captured", true);
         } else if (status != Status.Idle && instance.aliveNPCEnemiesCount != 0) {
            capturePoints = 0;
            status = Status.Idle;
         }

         // Check if the enemies inside the treasure site instance have been defeated
         Instance treasureSiteInstance = InstanceManager.self.getInstance(destinationInstanceId);
         if (treasureSiteInstance == null || treasureSiteInstance.aliveNPCEnemiesCount != 0) {
            isClearedOfEnemies = false;
         } else {
            isClearedOfEnemies = true;
         }
      }
   }

   public override void OnTriggerEnter2D (Collider2D other) {
   }

   public override void OnTriggerExit2D (Collider2D other) {
   }

   public override bool isOwnedByGroup (int voyageGroupId) {
      return true;
   }

   public override bool isActive () {
      return Voyage.isLastLeagueMap(leagueIndex);
   }

   #region Private Variables

   #endregion
}
