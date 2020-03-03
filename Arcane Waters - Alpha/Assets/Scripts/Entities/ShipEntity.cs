using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShipEntity : SeaEntity {
   #region Public Variables

   // The lower limit of the range zone
   public static float MIN_RANGE = 0.2f;

   // The Type of ship this is
   [SyncVar]
   public Ship.Type shipType;

   // The Skin Type of the ship
   [SyncVar]
   public Ship.SkinType skinType;

   // How fast this ship goes
   [SyncVar]
   public int speed = 100;

   // The range of attack - in percentage of the base range
   [SyncVar]
   public int attackRangeModifier = 100;

   // The number of sailors it takes to run this ship
   [SyncVar]
   public int sailors;

   // The Rarity of the ship
   public Rarity.Type rarity;

   #endregion

   public override void playAttackSound () {
      // Play a sound effect
      SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Ship_Cannon_1, this.transform.position);
   }

   public void updateSkin (Ship.SkinType newSkinType) {
      this.skinType = newSkinType;

      StartCoroutine(CO_UpdateAllSprites());
   }

   public bool canUseSkin (Ship.SkinType newSkinType) {
      string skinClass = newSkinType.ToString().Split('_')[0];

      return this.shipType.ToString().ToLower().Contains(skinClass.ToLower());
   }

   public bool isInRange(Vector2 spot) {
      if (Vector2.SqrMagnitude(spot - (Vector2)transform.position) < getAttackRange() * getAttackRange()) {
         return true;
      } else {
         return false;
      }
   }

   public float getAttackRange () {
      return 1.4f * (attackRangeModifier / 100f);
   }

   public override float getMoveSpeed () {
      // Start with the base speed for all sea entities
      float baseSpeed = base.getMoveSpeed();

      // Increase or decrease our speed based on the settings for this ship
      return baseSpeed * (this.speed / 100f);
   }

   public override float getTurnDelay () {
      switch (this.shipType) {
         case Ship.Type.Caravel:
            return .25f;
         case Ship.Type.Brigantine:
            return .30f;
         case Ship.Type.Nao:
            return .35f;
         case Ship.Type.Carrack:
            return .40f;
         case Ship.Type.Cutter:
            return .45f;
         case Ship.Type.Buss:
            return .50f;
         case Ship.Type.Galleon:
            return .60f;
         case Ship.Type.Barge:
            return .75f;
         default:
            return .25f;
      }
   }

   public Vector2 clampToRange (Vector2 targetPoint) {
      // Compute the relative position
      Vector2 relativePosition = (targetPoint - (Vector2)transform.position);

      // Compute the magnitude
      float magnitude = relativePosition.magnitude;

      // Clamp the magnitude to the upper and lower limits
      magnitude = Mathf.Clamp(magnitude, MIN_RANGE, getAttackRange());

      // Calculate the new relative position using the clamped magnitude
      Vector2 clampedRelativePosition = relativePosition.normalized * magnitude;

      // Return the world space position
      return (Vector2)transform.position + clampedRelativePosition;
   }

   public float getNormalizedTargetDistance (Vector2 target) {
      // Calculate the distance to the target
      float distance = Vector2.Distance(transform.position, target);

      // Clamps the distance to the limits
      distance = Mathf.Clamp(distance, MIN_RANGE, getAttackRange());

      // Calculate the ratio to the full range
      return distance / getAttackRange();
   }

   public static float getDamageModifierForDistance (float normalizedDistanceToTarget) {
      return 1f / normalizedDistanceToTarget;
   }

   [Command]
   public void Cmd_CastAbility (Attack.Type attackType) {
      if (isDead() || !hasReloaded()) {
         return;
      }

      // Note the time at which we last successfully attacked
      _lastAttackTime = Time.time;

      ShipAbilityData shipData = ShipAbilityManager.self.getAbility(attackType);
      
      // Attributes set up by server only
      switch (attackType) {
         case Attack.Type.Heal:
            currentHealth += shipData.damage;
            break;
         case Attack.Type.SpeedBoost:
            speed += shipData.damage;
            break;
      }

      Rpc_CastSkill(attackType, shipData, transform.position);
   }

   [ClientRpc]
   public void Rpc_CastSkill (Attack.Type attackType, ShipAbilityData shipData, Vector2 pos) {
      // Play The effect of the buff
      EffectManager.createDynamicEffect(shipData.castSpritePath, pos, shipData.abilitySpriteFXPerFrame);

      // Play an appropriate sound
      AudioClip clip = AudioClipManager.self.getAudioClipData(shipData.castSFXPath).audioClip;
      if (clip != null) {
         SoundManager.playClipOneShotAtPoint(clip, Camera.main.transform.position);
      } else {
         playAttackSound();
      }

      // Show the damage text
      ShipDamageText damageText = Instantiate(PrefabsManager.self.getTextPrefab(attackType), pos, Quaternion.identity);
      damageText.setIcon(shipData.skillIconPath);
      damageText.negativeEffect = false;
      damageText.setDamage(shipData.damage);
      damageText.notificationText.text = shipData.abilityName;
   }

   [Command]
   public void Cmd_FireMainCannonAtSpot (Vector2 spot, Attack.Type attackType, Vector2 spawnPosition) {
      if (isDead() || !hasReloaded()) {
         return;
      }

      // Note the time at which we last successfully attacked
      _lastAttackTime = Time.time;

      // The target point is clamped to the attack range
      spot = clampToRange(spot);

      // Calculate the distance to target, normalized to the max range
      float normalizedDistance = getNormalizedTargetDistance(spot);

      // Calculate shot parameters
      float distanceModifier = getDamageModifierForDistance(normalizedDistance);
      float projectileFlightDuration = (normalizedDistance * normalizedDistance * 1f) * ShipAbilityManager.self.getAbility(attackType).projectileSpeed;
    
      ShipAbilityData shipData = ShipAbilityManager.self.getAbility(attackType);
      this.currentImpactMagnitude = ShipAbilityData.getImpactType(normalizedDistance);

      // Fire the cannon ball and display an attack circle in all the clients
      Rpc_CreateCannonBall(spawnPosition, spot, Util.netTime(), Util.netTime() + projectileFlightDuration,
         attackType, AttackManager.self.getColorForDistance(normalizedDistance), shipData, normalizedDistance);

      // Have the server check for collisions after the AOE projectile reaches the target
      StartCoroutine(CO_CheckCircleForCollisions(this, projectileFlightDuration, spot, attackType, false, distanceModifier));

      // Make note on the clients that the ship just attacked
      Rpc_NoteAttack();
   }

   [ClientRpc]
   public void Rpc_CreateCannonBall (Vector2 startPos, Vector2 endPos, float startTime, float endTime, Attack.Type attackType, Color color, ShipAbilityData shipAbilityData, float normalizedDistance) {
      this.currentImpactMagnitude = ShipAbilityData.getImpactType(normalizedDistance);      

      // Create a new Attack Circle object from the prefab
      AttackCircle attackCircle;

      // Use a different prefab for local shots
      if (isLocalPlayer) {
         attackCircle = Instantiate(localAttackCirclePrefab, endPos, Quaternion.identity);
         attackCircle.color = color;
      } else {
         attackCircle = Instantiate(defaultAttackCirclePrefab, endPos, Quaternion.identity);
      }
      attackCircle.creator = this;
      attackCircle.startPos = startPos;
      attackCircle.endPos = endPos;
      attackCircle.startTime = startTime;
      attackCircle.endTime = startTime + 1f;

      // Create a cannon smoke effect
      Vector2 direction = endPos - startPos;
      Vector2 offset = direction.normalized * .1f;

      EffectManager.createDynamicEffect(shipAbilityData.castSpritePath, startPos, shipAbilityData.abilitySpriteFXPerFrame);

      // Create a cannon ball
      CannonBall ball = Instantiate(PrefabsManager.self.getCannonBallPrefab(attackType), startPos, Quaternion.identity);
      ball.init(startTime, endTime, startPos, endPos, this, currentImpactMagnitude);

      string projectilePath = ShipAbilityManager.self.getAbility(attackType).projectileSpritePath;
      if (projectilePath != "") {
         Sprite[] spriteArray = ImageManager.getSprites(projectilePath);
         if (spriteArray.Length == 1) {
            ball.staticSprite.gameObject.SetActive(true);
            ball.staticSprite.sprite = spriteArray[0];
         } else {
            ball.animatedSprite.gameObject.SetActive(true);
            ball.animatedSprite.sprite = spriteArray[0];
         }
      }

      // Play an appropriate sound
      AudioClip clip = AudioClipManager.self.getAudioClipData(shipAbilityData.castSFXPath).audioClip;
      if (clip != null) {
         SoundManager.playClipOneShotAtPoint(clip, Camera.main.transform.position);
      } else {
         playAttackSound();
      }

      // If it was our ship, shake the camera
      if (isLocalPlayer) {
         CameraManager.shakeCamera();
      }
   }

   #region Private Variables

   #endregion
}
