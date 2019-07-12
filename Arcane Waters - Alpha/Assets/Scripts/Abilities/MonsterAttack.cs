using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class MonsterAttack : MeleeAbility {
   #region Public Variables

   #endregion

   public MonsterAttack () {
      this.type = Type.Monster_Attack;
   }

   public override bool isIncludedFor (Battler battler) {
      // Players can't use Monster Abilities
      if (!battler.isMonster()) {
         return false;
      }

      return base.isIncludedFor(battler);
   }

   public override float getCooldown () {
      return 6.0f;
   }

   public override int getApChange () {
      // Monsters will get 5 AP for each attack
      return 5;
   }

   public override void playImpactSound (Battler attacker, Battler defender) {
      Vector3 pos = defender.transform.position;

      // Customize the impact sound based on the attacker type
      if (attacker is GolemBattler || attacker is LizardBattler) {
         SoundManager.playClipAtPoint(SoundManager.Type.Slam_Physical, pos);
      } /*else if (attacker is SlimeBattler) {
         SoundManager.playClipAtPoint(SoundManager.Type.Slime_Attack, pos);
      } else if (attacker is PlantBattler) {
         SoundManager.playClipAtPoint(SoundManager.Type.Plant_Chomp, pos);
      }*/
   }

   #region Private Variables

   #endregion
}
