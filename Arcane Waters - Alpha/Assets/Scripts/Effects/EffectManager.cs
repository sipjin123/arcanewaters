using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class EffectManager : MonoBehaviour {
   #region Public Variables

   // The Generic Effect prefab
   public Effect effectPrefab;

   // The Generic Projectile prefab
   public BattlerProjectile projectilePrefab;

   // Generic effect prefab for use in combat actions
   public CombatEffect combatVFXPrefab;

   // Generic effect prefab for use in world combat effects
   public CombatEffect worldCombatVFXPrefab;

   // Generic effect prefab used when ships receive a buff
   public SeaBuffEffect seaBuffEffectPrefab;

   // The effects when interacting a crate
   public GameObject interactCrateEffects;

   // Self
   public static EffectManager self;

   // The list of projectile info for associating sprites
   public List<ProjectileInfo> projectileInfoList;

   #endregion

   public void Awake () {
      self = this;
   }

   public Effect create (Effect.Type effectType, Vector2 pos) {
      Effect effect = Instantiate(effectPrefab, pos, Quaternion.identity);
      effect.effectType = effectType;

      return effect;
   }

   public static void createDynamicEffect (string spritePath, Vector2 pos, float timePerFrame, Transform parent, bool setLocalPosition = false) {
      Sprite[] sprites = ImageManager.getSprites(spritePath);

      //If we do not have any effect sprites we cancel
      if (sprites.Length <= 0) {
         return;
      }

      CombatEffect effect = Instantiate(self.worldCombatVFXPrefab, Vector3.zero, Quaternion.identity, parent).GetComponent<CombatEffect>();

      if (setLocalPosition) {
         effect.transform.localPosition = pos;
      } else {
         effect.transform.position = pos;
      }

      effect.initEffect(sprites, timePerFrame);
   }

   public static void createBuffEffect (string spritePath, Vector2 pos, Transform parent, bool setLocalPosition = false) {
      Sprite sprite = ImageManager.getSprite(spritePath);

      //If we did not find a sprite, we cancel
      if (sprite == null) {
         return;
      }

      SeaBuffEffect effect = Instantiate(self.seaBuffEffectPrefab, Vector3.zero, Quaternion.identity, parent);
      effect.buffSpriteRenderer.sprite = sprite;

      if (setLocalPosition) {
         effect.transform.localPosition = pos;
      } else {
         effect.transform.position = pos;
      }
   }

   public CombatEffect createCombatEffect (Sprite[] effectSprites, Vector2 pos, float timePerFrame) {

      //If we do not have any effect sprites we cancel
      if (effectSprites.Length <= 0) {
         return null;
      }

      CombatEffect effect = Instantiate(combatVFXPrefab, pos, Quaternion.identity);
      effect.initEffect(effectSprites, timePerFrame);

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
         case Effect.Type.Leaves_Exploding:
            return 0.05f;
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

   public static GameObject show (Effect.Type effectType, Vector2 pos, float overWriteSpeed = 0) {
      // Instantiate a generic effect from the prefab
      GameObject genericEffect = Instantiate(PrefabsManager.self.genericEffectPrefab, pos, Quaternion.identity);

      // Swap in the desired Effect texture
      SimpleAnimation anim = genericEffect.GetComponent<SimpleAnimation>();
      anim.setNewTexture(ImageManager.getSprite("Effects/" + effectType, true).texture);

      // Check if we need to set a custom speed
      float frameLength = getFrameLength(effectType);
      if (overWriteSpeed != 0) {
         frameLength = overWriteSpeed;
      }
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
      // Play block SFX
      SoundEffectManager.self.playFmodSfx(SoundEffectManager.BLOCK_ATTACK, target.transform.position);
      //SoundManager.playClipAtPoint(SoundManager.Type.Character_Block, target.transform.position);

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

   public static void playPoofEffect (Battler deadBattler) {
      //SoundManager.playClipAtPoint(SoundManager.Type.Death_Poof, deadBattler.transform.position);
      if (!deadBattler.isBossType) {
         SoundEffectManager.self.playFmodSfx(SoundEffectManager.DEATH_POOF, deadBattler.transform.position);
      }

      // Play a "Poof" effect on our head
      Effect poof = self.create(Effect.Type.Poof, deadBattler.transform.position);
      poof.transform.SetParent(self.transform, false);
      float flip = deadBattler.isAttacker() ? -1f : 1f;
      poof.transform.position = new Vector3(
          deadBattler.transform.position.x - (.08f * flip), deadBattler.transform.position.y + .45f, deadBattler.transform.position.z
      );
   }

   // Used for executing a VFX in the target battler
   public static void playCombatAbilityVFX (Battler attacker, Battler target, BattleAction action, Vector2 targetPos, BattleActionType actionType) {
      BasicAbilityData ability = new BasicAbilityData();
      if (actionType == BattleActionType.Attack) {
         ability = attacker.getAttackAbilities()[action.abilityInventoryIndex];
      } else if (actionType == BattleActionType.BuffDebuff) {
         ability = attacker.getBuffAbilities()[action.abilityInventoryIndex];
      }

      List<Sprite> hitSprites = new List<Sprite>();
      foreach (string path in ability.hitSpritesPath) {
         if (!string.IsNullOrEmpty(path)) {
            Sprite[] spritePath = ImageManager.getSprites(path);
            foreach (Sprite sprite in spritePath) {
               hitSprites.Add(sprite);
            }
         }
      }

      if (hitSprites.Count > 0) { 
         // Process ability effects, skip if the ability effect was intentionally not assigned
         CombatEffect effectInstance = self.createCombatEffect(hitSprites.ToArray(), targetPos, ability.hitFXTimePerFrame);
         if (effectInstance != null) {
            // Instantiate and play the effect at that position
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
      }
   }

   // Used for creating a VFX for casting a magic spell
   public static void playCastAbilityVFX (Battler source, BattleAction action, Vector2 targetPos, BattleActionType actionType) {
      BasicAbilityData ability = new BasicAbilityData();

      if (actionType == BattleActionType.Attack) {
         ability = source.getAttackAbilities()[action.abilityInventoryIndex];
      } else if (actionType == BattleActionType.BuffDebuff) {
         ability = source.getBuffAbilities()[action.abilityInventoryIndex];
      }

      List<Sprite> castSprites = new List<Sprite>();
      foreach (string path in ability.castSpritesPath) {
         if (!string.IsNullOrEmpty(path)) {
            foreach (Sprite sprite in ImageManager.getSprites(path)) {
               castSprites.Add(sprite);
            }
         }
      }

      bool castAboveTarget = ability.abilityCastPosition == BasicAbilityData.AbilityCastPosition.AboveTarget || ability.abilityCastPosition == BasicAbilityData.AbilityCastPosition.AboveSelf;
      if (castSprites.Count > 0) {
         // Process ability effects, skip if the ability effect was intentionally not assigned
         CombatEffect effectInstance = self.createCombatEffect(castSprites.ToArray(), targetPos, ability.FXTimePerFrame);
         if (effectInstance) {
            effectInstance.transform.position = new Vector3(
               targetPos.x,
               targetPos.y + (castAboveTarget ? .5f : 0),
               source.transform.position.z - .001F
               );
            effectInstance.transform.SetParent(self.transform, false);

            // Flip the X scale for the defenders
            if (!source.isAttacker()) {
               effectInstance.transform.localScale = new Vector3(
                   effectInstance.transform.localScale.x * -1f,
                   effectInstance.transform.localScale.y,
                   effectInstance.transform.localScale.z
               );
            }
         }
      }
   }
   public static void spawnProjectile (Battler source, AttackAction action, Vector2 sourcePos, Vector2 targetPos, float projectileSpeed, string projectileSpritePath, float scale, float fxSpeed) {
      GameObject genericEffect = Instantiate(self.projectilePrefab.gameObject, sourcePos, Quaternion.identity);
      genericEffect.transform.position = sourcePos;
      BattlerProjectile battlerProjectile = genericEffect.GetComponent<BattlerProjectile>();
      battlerProjectile.setTrajectory(sourcePos, targetPos, projectileSpeed, fxSpeed);
      battlerProjectile.projectileRenderer.sprite = ImageManager.getSprite(projectileSpritePath);
      if (projectileSpritePath.Contains("empty")) {
         battlerProjectile.shadowObj.SetActive(false);
      }
      genericEffect.transform.localScale = new Vector3(scale, scale, scale);

      // It target is at the left side of the map, flip the sprite
      battlerProjectile.projectileRenderer.flipX = targetPos.x < sourcePos.x;
   }

   #region Private Variables

   #endregion
}