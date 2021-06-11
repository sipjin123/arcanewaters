using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeaStructure : SeaEntity {
   #region Public Variables

   // An action that can be subscribed to, to be notified of a structure's death
   public System.Action<SeaStructure> onDeathAction;

   // The sea structure that will be able to be damaged after this dies
   public SeaStructure unlockAfterDeath;

   #endregion

   public override bool isSeaStructure () {
      return true;
   }

   public override void onDeath () {
      base.onDeath();
      onDeathAction?.Invoke(this);
      _deathTime = NetworkTime.time;
      unlockAfterDeath?.setIsInvulnerable(false);
   }

   protected override void Update () {
      base.Update();

      if (isDead()) {
         double timeSinceDeath = NetworkTime.time - _deathTime;
         if (timeSinceDeath >= DYING_TIME_BEFORE_DISABLE) {
            gameObject.SetActive(false);
         }
      }
   }

   #region Private Variables

   // The timestamp for when this structure died
   private double _deathTime = 0.0f;

   // How long this structure will wait after dying, before being disabled
   private const double DYING_TIME_BEFORE_DISABLE = 2.0;

   #endregion
}
