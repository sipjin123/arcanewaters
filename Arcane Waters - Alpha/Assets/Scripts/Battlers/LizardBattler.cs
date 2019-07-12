using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class LizardBattler : MonsterBattler
{
   #region Public Variables

   #endregion

   public override int getXPValue () {
      return Random.Range(12, 25);
   }

   public override int getGoldValue () {
      return Random.Range(12, 25);
   }

   public override float getPreContactLength () {
      return .35f;
   }

   public override float getDefense (Ability.Element element) {
      float defense = base.getDefense(element);

      switch (element) {
         case Ability.Element.Fire:
            return defense * 1.25f;
         case Ability.Element.Earth:
            return defense * 1.50f;
         case Ability.Element.Water:
            return defense * .75f;
         default:
            return defense;
      }
   }

   public override float getDamage (Ability.Element element) {
      float damage = base.getDamage(element);

      switch (element) {
         case Ability.Element.Fire:
            return damage * 1.25f;
         case Ability.Element.Earth:
            return damage * 1.50f;
         case Ability.Element.Water:
            return damage * .75f;
         default:
            return damage;
      }
   }

   public override void playJumpSound () {
      // Nothing for now
   }

   public override void playDeathSound () {
      SoundManager.playClipAtPoint(SoundManager.Type.Golem_Death, this.transform.position);
   }

   #region Private Variables

   #endregion
}
