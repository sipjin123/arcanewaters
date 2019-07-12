using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class BasicAttack : MeleeAbility {
   #region Public Variables

   #endregion

   public BasicAttack () {
      this.type = Type.Basic_Attack;
      this.name = "Slash";
   }

   public override float getCooldown () {
      return 3.0f;
   }

   public override int getApChange () {
      // Basic attacks will grant 5 AP by default
      return 5;
   }

   public override void playCastSound (Battler attacker, Battler defender) {
      // Decide the sound to use based on the weapon type
      switch (attacker.weaponManager.weaponType) {
         case Weapon.Type.Pitchfork:
         case Weapon.Type.Sword_Steel:
         case Weapon.Type.Lance_Steel:
         case Weapon.Type.Sword_Rune:
         case Weapon.Type.Sword_1:
         case Weapon.Type.Sword_2:
         case Weapon.Type.Sword_3:
         case Weapon.Type.Sword_4:
         case Weapon.Type.Sword_5:
         case Weapon.Type.Sword_6:
         case Weapon.Type.Sword_7:
         case Weapon.Type.Sword_8:
            SoundManager.playClipAtPoint(SoundManager.Type.Sword_Swing, defender.transform.position);
            break;
         case Weapon.Type.Staff_Mage:
         case Weapon.Type.Mace_Steel:
         case Weapon.Type.Mace_Star:
         case Weapon.Type.None:
            SoundManager.playClipAtPoint(SoundManager.Type.Sword_Swing, defender.transform.position);
            break;
         default:
            D.warning("No attack sound defined for weapon: " + attacker.weaponManager.weaponType);
            break;
      }
   }

   public override void playImpactSound (Battler attacker, Battler defender) {
      Vector3 pos = defender.transform.position;

      // Decide the sound to use based on the weapon type
      switch (attacker.weaponManager.weaponType) {
         case Weapon.Type.Pitchfork:
         case Weapon.Type.Sword_Steel:
         case Weapon.Type.Lance_Steel:
         case Weapon.Type.Sword_Rune:
         case Weapon.Type.Sword_1:
         case Weapon.Type.Sword_2:
         case Weapon.Type.Sword_3:
         case Weapon.Type.Sword_4:
         case Weapon.Type.Sword_5:
         case Weapon.Type.Sword_6:
         case Weapon.Type.Sword_7:
         case Weapon.Type.Sword_8:
            SoundManager.playClipAtPoint(SoundManager.Type.Slash_Physical, pos);
            break;
         case Weapon.Type.Staff_Mage:
         case Weapon.Type.Mace_Steel:
         case Weapon.Type.Mace_Star:
         case Weapon.Type.None:
            SoundManager.playClipAtPoint(SoundManager.Type.Slam_Physical, pos);
            break;
         default:
            D.warning("No attack sound defined for weapon: " + attacker.weaponManager.weaponType);
            SoundManager.playClipAtPoint(SoundManager.Type.Slam_Physical, pos);
            break;
      }
   }

   #region Private Variables

   #endregion
}
