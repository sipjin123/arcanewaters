using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public abstract class BuffAbility : MagicAbility {
   #region Public Variables

   #endregion

   public virtual float getDuration () {
      // The duration of the buff
      return 12f;
   }

   public override bool isIncludedFor (Battler battler) {
      // Players can't use this ability
      if (!battler.isMonster()) {
         return false;
      }

      return base.isIncludedFor(battler);
   }

   public override float getModifier () {
      return 1.2f;
   }

   public override Element getElement () {
      return Element.Earth;
   }

   public override float getCooldown () {
      return 6.0f;
   }

   public override List<Battler.Stance> getAllowedStances () {
      return new List<Battler.Stance>() { Battler.Stance.Attack, Battler.Stance.Balanced, Battler.Stance.Defense };
   }

   public override Weapon.Class getRequiredWeaponClass () {
      return Weapon.Class.Any;
   }

   public override Effect.Type getEffectType (Weapon.Type weaponType) {
      return Effect.Type.Blunt_Physical;
   }

   public override void applyAction (Battler sourceBattler, Battler targetBattler, BattleAction action) {
      BuffAction buff = (BuffAction) action;
      targetBattler.addBuff(buff.getBuffTimer());

      // Create a BuffIcon that will hover over the target sprite
      /*GameObject prefab = (GameObject) Resources.Load("Buffs/BuffIcon");
      GameObject instance = (GameObject) GameObject.Instantiate(prefab);
      BuffIcon icon = instance.GetComponent<BuffIcon>();
      icon.initialize(buff, targetSprite.getBattler());

      // Move it into position and set the parent
      icon.name = this.type + " Buff Icon";
      icon.transform.SetParent(targetSprite.largeStatusBar.buffIconsContainer.transform, false);*/
   }

   #region Private Variables

   #endregion
}
