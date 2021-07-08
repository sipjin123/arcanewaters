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

   protected override void Awake () {
      base.Awake();
   }

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
