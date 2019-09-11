using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class GolemBattler : MonsterBattler {
   #region Public Variables

   #endregion

   public override void adjustCanvas (Canvas canvas) {
      // We're a bit short, so move our status bar canvas
      canvas.transform.position = new Vector3(
          canvas.transform.position.x,
          canvas.transform.position.y - .12f,
          canvas.transform.position.z
      );
   }

   public override int getXPValue () {
      return Random.Range(12, 25);
   }

   public override int getGoldValue () {
      return Random.Range(12, 25);
   }

   public override float getPreContactLength () {
      return .6f;
   }

   public override float getDefense (Element element) {
      float defense = base.getDefense(element);

      switch (element) {
         case Element.Fire:
            return defense * 1.25f;
         case Element.Earth:
            return defense * 1.50f;
         case Element.Water:
            return defense * .75f;
         default:
            return defense;
      }
   }

   public override float getDamage (Element element) {
      float damage = base.getDamage(element);

      switch (element) {
         case Element.Fire:
            return damage * 1.25f;
         case Element.Earth:
            return damage * 1.50f;
         case Element.Water:
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
