using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class EffectManager : MonoBehaviour {
   #region Public Variables

   // The Generic Effect prefab
   public Effect effectPrefab;

   // Self
   public static EffectManager self;

   #endregion

   public void Awake () {
      self = this;
   }

   public Effect create (Effect.Type effectType, Vector2 pos) {
      Effect effect = Instantiate(effectPrefab, pos, Quaternion.identity);
      effect.effectType = effectType;

      return effect;
   }

   public float getTimePerFrame (Effect.Type effectsType) {
      switch (effectsType) {
         case Effect.Type.Crop_Shine:
         case Effect.Type.Crop_Harvest:
            return .25f;
         case Effect.Type.Poof:
         case Effect.Type.Slam_Physical:
         case Effect.Type.Slash_Fire:
         case Effect.Type.Slash_Ice:
         case Effect.Type.Slash_Lightning:
         case Effect.Type.Slash_Physical:
         case Effect.Type.Blunt_Physical:
            return .08f;
         default:
            return .10f;
      }
   }

   public static List<GameObject> show (List<Effect.Type> effectTypes, Vector2 pos) {
      List<GameObject> list = new List<GameObject>();

      foreach (Effect.Type effectType in effectTypes) {
         GameObject effect = show(effectType, pos);
         list.Add(effect);
      }

      return list;
   }

   public static GameObject show (Effect.Type effectType, Vector2 pos) {
      // Instantiate a generic effect from the prefab
      GameObject genericEffect = Instantiate(PrefabsManager.self.genericEffectPrefab, pos, Quaternion.identity);

      // Swap in the desired Effect texture
      SimpleAnimation anim = genericEffect.GetComponent<SimpleAnimation>();
      anim.setNewTexture(ImageManager.getTexture("Effects/" + effectType));

      // Check if we need to set a custom speed
      float frameLength = getFrameLength(effectType);
      if (frameLength >= 0f) {
         anim.frameLengthOverride = frameLength;
      }

      return genericEffect;
   }

   public static List<Effect.Type> getEffects (Attack.Type attackType) {
      switch (attackType) {
         case Attack.Type.Air:
            return new List<Effect.Type>() { Effect.Type.Ranged_Air, Effect.Type.Cannon_Smoke };
         case Attack.Type.Ice:
            return new List<Effect.Type>() { Effect.Type.Freeze };
         default:
            return new List<Effect.Type>() { Effect.Type.Ranged_Fire, Effect.Type.Cannon_Smoke };
      }
   }

   protected static float getFrameLength (Effect.Type effectType) {
      switch (effectType) {
         case Effect.Type.Freeze:
            return .10f;
         default:
            return -1f;
      }
   }

   public static void playBlockEffect (Battler attacker, Battler target) {
      SoundManager.playClipAtPoint(SoundManager.Type.Character_Block, target.transform.position);

      // Find a point at which to display the effect
      Vector3 impactPoint = target.transform.position;

      if (target.isAttacker()) {
         impactPoint.x += .08f;
         impactPoint.y += .2f;
      } else {
         impactPoint.x -= .08f;
         impactPoint.y += .2f;
      }

      // Instantiate and play a block effect at that position
      Effect effectInstance = self.create(Effect.Type.Block, impactPoint);
      effectInstance.transform.SetParent(self.transform, false);
      Util.setZ(effectInstance.transform, target.transform.position.z - 5f);
   }

   public static void playHitEffect (Battler attacker, Battler target) {
      // Instantiate and play a hit effect at that position
      Effect effectInstance = self.create(Effect.Type.Hit, Vector3.zero);
      effectInstance.transform.SetParent(self.transform, false);
      effectInstance.transform.localPosition = new Vector3(
          target.transform.localPosition.x,
          target.transform.localPosition.y + .12f,
          0f
      );
   }

   public static void playPoofEffect (Battler deadBattler) {
      SoundManager.playClipAtPoint(SoundManager.Type.Death_Poof, deadBattler.transform.position);

      // Play a "Poof" effect on our head
      Effect poof = self.create(Effect.Type.Poof, deadBattler.transform.position);
      poof.transform.SetParent(self.transform, false);
      float flip = deadBattler.isAttacker() ? -1f : 1f;
      poof.transform.position = new Vector3(
          deadBattler.transform.position.x - (.08f * flip), deadBattler.transform.position.y + .45f, deadBattler.transform.position.z
      );
   }

   // TODO ZERONEV: This method can be simplifieD
   public static void playAttackEffect (Battler attacker, Battler target, AttackAction action, Vector2 targetPos) {
      Weapon.Type attackerWeapon = attacker.weaponManager.weaponType;

      // TODO ZERONEV-IMPORTANT: This was commented out to be able to compile, restore it later. (using deprecated method in the meantime)
      //Ability ability = AbilityManager.getAbility(action.abilityType);
      Ability ability = AbilityManager.getAbility(Ability.Type.Basic_Attack);
      
      // Look up the appropriate attack effect
      Effect.Type effectType = ability.getEffectType(attackerWeapon);

      // Instantiate and play the effect at that position
      Effect effectInstance = self.create(effectType, targetPos);
      effectInstance.transform.position = new Vector3(
          targetPos.x,
          targetPos.y,
          target.transform.position.z - .001f
      );
      effectInstance.transform.SetParent(self.transform, false);

      // Flip the X scale for the defenders
      if (!target.isAttacker()) {
         effectInstance.transform.localScale = new Vector3(
             effectInstance.transform.localScale.x * -1f,
             effectInstance.transform.localScale.y,
             effectInstance.transform.localScale.z
         );
      }
   }

   #region Private Variables

   #endregion
}
