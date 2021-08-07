using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PvpBase : SeaStructure {
   #region Public Variables

   // A reference to the SimpleAnimation component on the sprite renderering the PvpBase structure
   public SimpleAnimation mainRendererAnimation;

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
         // Only heal live and damaged friendly player ships
         if (!ally.isPlayerShip() || ally.isDead() || (ally.currentHealth >= ally.maxHealth)) {
            continue;
         }

         // Healing the ship comes with a silver penalty
         if (GameStatsManager.self.isUserRegistered(ally.userId)) {
            int silverPenalty = SilverManager.computeHealSilverPenalty(ally);
            int currentSilver = GameStatsManager.self.getSilverAmount(ally.userId);

            if (currentSilver < silverPenalty) {
               // The user doesn't have enough silver. Skip the healing process
               continue;
            }

            // Apply the penalty
            GameStatsManager.self.addSilverAmount(ally.userId, -silverPenalty);
            ally.Target_ReceiveSilverCurrency(ally.connectionToClient, -silverPenalty, SilverManager.SilverRewardReason.Heal);

            // Heal the ship
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
      if (isServer) {
         Rpc_OnDeath();
      }

      _hasRunOnDeath = true;
   }

   protected override void setupSprites () {
      Sprite newSprite = getSprite();
      if (newSprite != null) {
         if (isDead()) {
            mainRendererAnimation.enabled = false;
            mainRenderer.sprite = newSprite;
         } else {
            mainRendererAnimation.setNewTexture(newSprite.texture);
         }

         string paletteDef = PvpManager.getStructurePaletteForTeam(pvpTeam);
         mainRenderer.GetComponent<RecoloredSprite>().recolor(paletteDef);
      }
   }

   #region Private Variables

   #endregion
}
