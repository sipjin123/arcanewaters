using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class Ability {
    #region Public Variables

    // The Types of abilities
    public enum Type {
        Basic_Attack = 100,
            Monster_Attack = 101, Slash_Fire = 102, Slash_Lightning = 103, Slash_Ice = 104, Lava_Glob = 105,
            Roots = 106, Haste = 107, Boulder = 108, Arrow_Coral = 109, Heal = 110, Arrow_Ent = 111,
            Rockspire = 112, BossShout = 113, SlimeSpit = 114, SlimeRain = 115,
        Cancel_Ability = 200,
        Stance_Ability = 300,
    }

    // The type of ability this is
    public Type type;

    // The displayed name of the ability
    public string name;

   #endregion

   public virtual string getDescription () {
      D.warning("No description defined for ability: " + type);
      return "";
   }

   public virtual float getModifier () {
      // No damage increase or decrease by default
      return 1.0f;
   }

   public virtual Element getElement () {
      // Default to Physical damage
      return Element.Physical;
   }

   public virtual float getCooldown () {
      // By default, attacks take 3 seconds to cooldown
      return 3.0f;
   }

   public virtual int getApChange () {
      // By default, abilities will give a small amount of AP
      return 3;
   }

   public virtual List<Battler.Stance> getAllowedStances () {
      // By default, allow the ability to be used in all stances
      return new List<Battler.Stance>() { Battler.Stance.Attack, Battler.Stance.Balanced, Battler.Stance.Defense };
   }

   public virtual Weapon.Class getRequiredWeaponClass () {
      // By default, allow the ability to work with any weapon class
      return Weapon.Class.Any;
   }

   public virtual Effect.Type getEffectType (Weapon.Type weaponType) {
      // By default, show the blunt attack effect
      return Effect.Type.Blunt_Physical;
   }

   public virtual Anim.Type getAnimation () {
      // By default, we'll use the Attack animation when the ability is activated
      return Anim.Type.Attack_East;
   }

   public virtual bool hasEffectAnimation () {
      // Most abilities have an effect animation that is displayed
      return true;
   }

   public virtual bool hasKnockup () {
      // By default, abilities will not knock players up
      return false;
   }

   public virtual bool hasShake () {
      // By default, abilities will not shake players
      return false;
   }

   public virtual bool canBeBlocked () {
      // By default, all abilities are capable of being blocked
      return true;
   }

   public virtual bool isIncludedFor (Battler battler) {
      // Make sure we're in the right stance
      if (!getAllowedStances().Contains(battler.stance)) {
         return false;
      }

      // Make sure we have the right class of weapon
      Weapon.Class requiredWeaponClass = getRequiredWeaponClass();
      if (requiredWeaponClass != Weapon.Class.Any && Weapon.getClass(battler.weaponManager.weaponType) != requiredWeaponClass) {
         return false;
      }

      // Everything checks out
      return true;
   }

   public virtual bool isReadyForUseBy (Battler battler) {
      // First, check if the ability even shows up for this battler
      if (!isIncludedFor(battler)) {
         return false;
      }

      // Then, make sure they have sufficent AP
      if (battler.AP < getApCost()) {
         return false;
      }

      // And that we're off cooldown
      if (Util.netTime() < battler.cooldownEndTime) {
         return false;
      }

      return true;
   }

   public virtual float getTotalAnimLength (Battler attacker, Battler target) {
      // The total length of the entire animation chain for this ability, determined by child classes
      return 0f;
   }

   // Zeronev-Comment: Moved this method completely to the new AbilityData as an interface.
   /*public virtual IEnumerator display (float timeToWait, BattleAction battleAction, bool isFirstAction) {
      // Display code is implemented in the child class
      yield return new WaitForSeconds(timeToWait);
   }*/

   public virtual void playCastSound (Battler attacker, Battler defender) {
      // Nothing by default
   }

   public virtual void playMagicSound (Battler attacker, Battler defender) {
      // Nothing by default
   }

   public virtual void playImpactSound (Battler attacker, Battler defender) {
      // Nothing by default
   }

   public bool isHeal () {
      return getModifier() < 0;
   }

   public int getApCost () {
      // Just a handy wrapper function to the AP change
      return -getApChange();
   }

   #region Private Variables

   #endregion
}
