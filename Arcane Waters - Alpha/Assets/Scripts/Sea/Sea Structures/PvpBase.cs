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
         // Disable healing of units on base proximity
         //InvokeRepeating(nameof(healAllies), 1.0f, 1.0f);
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
         rewardXPToAllAttackers();
         Rpc_OnDeath();
      }

      _hasRunOnDeath = true;
   }

   protected override Sprite getSprite () {
      switch (_structureIntegrity) {
         case StructureIntegrity.Healthy:
            return ImageManager.getSprite("Sprites/SeaStructures/pvp_base");
         case StructureIntegrity.Damaged:
            return ImageManager.getSprite("Sprites/SeaStructures/pvp_base_damaged");
         case StructureIntegrity.Destroyed:
            return ImageManager.getSprite("Sprites/SeaStructures/pvp_base_destroyed");
         default:
            return ImageManager.getSprite("Sprites/SeaStructures/pvp_base");
      }
   }

   public override void setupSprites () {
      Sprite newSprite = getSprite();
      if (newSprite != null) {
         if (isDead()) {
            mainRendererAnimation.enabled = false;
            mainRenderer.sprite = newSprite;
         } else {
            mainRendererAnimation.setNewTexture(newSprite.texture);
            mainRendererAnimation.minIndex = getAnimationStartIndex();
            mainRendererAnimation.maxIndex = getAnimationEndIndex();
         }

         string paletteDef = PvpManager.getStructurePaletteForTeam(pvpTeam);
         mainRenderer.GetComponent<RecoloredSprite>().recolor(paletteDef);
      }
   }

   private int getAnimationStartIndex () {
      int factionIndex = (int) faction;
      return factionIndex * 4;
   }

   private int getAnimationEndIndex () {
      int factionIndex = (int) faction;
      return factionIndex * 4 + 3;
   }

   #region Private Variables

   #endregion
}
