using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PvpBase : SeaStructure {
   #region Public Variables

   #endregion

   protected override void onActivated () {
      base.onActivated();

      if (isServer) {
         InvokeRepeating(nameof(healAllies), 1.0f, 1.0f);
      }
   }

   protected override void onDeactivated () {
      base.onDeactivated();

      if (isServer) {
         CancelInvoke(nameof(healAllies));
      }
   }

   private void healAllies () {
      List<SeaEntity> allies = Util.getAlliesInCircle(this, transform.position, 2.0f);

      foreach (SeaEntity ally in allies) {
         // Only heal friendly player ships
         if (ally.isPlayerShip()) {
            int healValue = (int) (ally.maxHealth * 0.1f);
            ally.currentHealth = Mathf.Clamp(ally.currentHealth + healValue, 0, ally.maxHealth);
         }
      }
   }

   public override void onDeath () {
      if (_hasRunOnDeath) {
         return;
      }
      // Don't run base onDeath function, as we don't to start the coroutine in SeaStructure.onDeath
      onDeathAction?.Invoke(this);
      Rpc_OnDeath();
      _hasRunOnDeath = true;
   }

   #region Private Variables

   #endregion
}
