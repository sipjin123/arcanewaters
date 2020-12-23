using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool;

public class ShipEntity : SeaEntity
{
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

   // The primary ability id used by this ship
   [SyncVar]
   public int primaryAbilityId = -1;

   // The Rarity of the ship
   public Rarity.Type rarity;

   // List of ship sprites depending on size
   public List<ShipSizeSpritePair> shipSizeSpriteList;

   // Cached ship size sprite
   public ShipSizeSpritePair shipSizeSpriteCache;

   // Size of the ship
   [SyncVar]
   public ShipSize shipSize;

   #endregion

   protected virtual void initialize (ShipData data) {
      shipType = data.shipType;
      skinType = data.skinType;
      currentHealth = data.baseHealth;
      maxHealth = data.baseHealth;
      attackRangeModifier = data.baseRange;

      speed = data.baseSpeed;
      sailors = data.baseSailors;
      rarity = Rarity.Type.None;
      damage = data.baseDamage;

      shipSize = data.shipSize;
      shipSizeSpriteCache = shipSizeSpriteList.Find(_ => _.shipSize == shipSize);
   }

   protected virtual void initializeAsSeaEnemy (SeaMonsterEntityData enemyData, ShipData shipData) {
      shipType = shipData.shipType;
      skinType = shipData.skinType;
      currentHealth = enemyData.maxHealth;
      maxHealth = enemyData.maxHealth;
      attackRangeModifier = (int) enemyData.detectRadius;

      speed = shipData.baseSpeed;
      sailors = shipData.baseSailors;
      rarity = Rarity.Type.None;
      damage = shipData.baseDamage;

      shipSize = shipData.shipSize;
      shipSizeSpriteCache = shipSizeSpriteList.Find(_ => _.shipSize == shipSize);
   }

   protected virtual void initialize (ShipInfo info) {
      shipType = info.shipType;
      skinType = info.skinType;
      currentHealth = info.health;
      maxHealth = info.maxHealth;
      attackRangeModifier = info.attackRange;

      speed = info.speed;
      sailors = info.sailors;
      rarity = info.rarity;
      damage = info.damage;

      ShipData newShipData = ShipDataManager.self.getShipData(info.shipType);
      shipSize = newShipData.shipSize;
      shipSizeSpriteCache = shipSizeSpriteList.Find(_ => _.shipSize == shipSize);
   }

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

   public bool isInRange (Vector2 position) {
      return Vector2.SqrMagnitude(position - (Vector2) transform.position) <= getAttackRange() * getAttackRange();
   }

   public float getAttackRange () {
      return 1.4f * (attackRangeModifier / 100f);
   }

   public override float getMoveSpeed () {
      // Start with the base speed for all sea entities
      float baseSpeed = base.getMoveSpeed();
      if (isSpeedingUp) {
         baseSpeed *= SPEEDUP_MULTIPLIER_SHIP;
      }

      // Increase or decrease our speed based on the settings for this ship
      float calculatedSpeed = baseSpeed * (this.speed / 100f);
      if (calculatedSpeed > MAX_SHIP_SPEED) {
         calculatedSpeed = MAX_SHIP_SPEED;
      }
      return calculatedSpeed;
   }

   public override float getTurnDelay () {
      switch (this.shipType) {
         case Ship.Type.Type_1:
            return .25f;
         case Ship.Type.Type_2:
            return .30f;
         case Ship.Type.Type_3:
            return .35f;
         case Ship.Type.Type_4:
            return .40f;
         case Ship.Type.Type_5:
            return .45f;
         case Ship.Type.Type_6:
            return .50f;
         case Ship.Type.Type_7:
            return .60f;
         case Ship.Type.Type_8:
            return .75f;
         default:
            return .25f;
      }
   }

   public Vector2 clampToRange (Vector2 targetPoint) {
      // Compute the relative position
      Vector2 relativePosition = (targetPoint - (Vector2) transform.position);

      // Compute the magnitude
      float magnitude = relativePosition.magnitude;

      // Clamp the magnitude to the upper and lower limits
      magnitude = Mathf.Clamp(magnitude, MIN_RANGE, getAttackRange());

      // Calculate the new relative position using the clamped magnitude
      Vector2 clampedRelativePosition = relativePosition.normalized * magnitude;

      // Return the world space position
      return (Vector2) transform.position + clampedRelativePosition;
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
      _lastAttackTime = NetworkTime.time;

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

      // Casting a skill is considered a PvP action
      hasEnteredPvP = true;

      Rpc_CastSkill(attackType, shipData, transform.position);
   }

   [ClientRpc]
   public void Rpc_CastSkill (Attack.Type attackType, ShipAbilityData shipData, Vector2 pos) {
      // Play The effect of the buff
      EffectManager.createDynamicEffect(shipData.castSpritePath, pos, shipData.abilitySpriteFXPerFrame);

      // Play an appropriate sound
      AudioClip clip = AudioClipManager.self.getAudioClipData(shipData.castSFXPath).audioClip;
      if (clip != null) {
         SoundManager.playClipAtPoint(clip, Camera.main.transform.position);
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
      _lastAttackTime = NetworkTime.time;

      // The target point is clamped to the attack range
      spot = clampToRange(spot);

      // Calculate the distance to target, normalized to the max range
      float normalizedDistance = getNormalizedTargetDistance(spot);

      // Calculate shot parameters
      ShipAbilityData shipData = ShipAbilityManager.self.getAbility(attackType);
      float distanceModifier = getDamageModifierForDistance(normalizedDistance);
      float projectileFlightDuration = normalizedDistance / shipData.projectileSpeed;
      currentImpactMagnitude = ShipAbilityData.getImpactType(normalizedDistance);

      // Fire the cannon ball and display an attack circle in all the clients
      Rpc_CreateCannonBall(spawnPosition, spot, NetworkTime.time, NetworkTime.time + projectileFlightDuration,
         attackType, AttackManager.self.getColorForDistance(normalizedDistance), shipData, normalizedDistance, NetworkTime.time + getInputDelay());

      // Have the server check for collisions after the AOE projectile reaches the target
      StartCoroutine(CO_CheckCircleForCollisions(this, projectileFlightDuration, spot, attackType, false, distanceModifier, currentImpactMagnitude, primaryAbilityId));

      // Firing the cannon is considered a PvP action
      hasEnteredPvP = true;

      // Make note on the clients that the ship just attacked
      Rpc_NoteAttack();
   }

   [ClientRpc]
   public void Rpc_CreateCannonBall (Vector2 startPos, Vector2 endPos, double startTime, double endTime, Attack.Type attackType, Color color, ShipAbilityData shipAbilityData, float normalizedDistance, double timeStamp) {
      StartCoroutine(CO_FireDelayedCannonBall(startPos, endPos, startTime, endTime, attackType, color, shipAbilityData, normalizedDistance, timeStamp));
   }

   protected IEnumerator CO_FireDelayedCannonBall (Vector2 startPos, Vector2 endPos, double startTime, double endTime, Attack.Type attackType, Color color, ShipAbilityData shipAbilityData, float normalizedDistance, double timeStamp) {
      while (NetworkTime.time < timeStamp) {
         yield return null;
      }

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
      GenericSeaProjectile ball = Instantiate(PrefabsManager.self.seaEntityProjectile, startPos, Quaternion.identity);
      ball.init(startTime, endTime, startPos, endPos, this, shipAbilityData.abilityId);

      // Play an appropriate sound
      AudioClip clip = AudioClipManager.self.getAudioClipData(shipAbilityData.castSFXPath).audioClip;
      if (clip != null) {
         SoundManager.playClipAtPoint(clip, Camera.main.transform.position);
      } else {
         playAttackSound();
      }

      // If it was our ship, shake the camera
      if (isLocalPlayer) {
         CameraManager.shakeCamera();
      }
   }

   protected override void updateSprites () {
      base.updateSprites();

      // Store the ripple sprites for later so we can quickly swap them once the entity starts/stops moving
      _ripplesStillSprites = ImageManager.getTexture(Ship.getRipplesPath(shipType));
      _ripplesMovingSprites = ImageManager.getTexture(Ship.getRipplesMovingPath(shipType));
      ripplesContainer.GetComponent<SpriteSwap>().newTexture = _ripplesStillSprites;
      
      // Cache ship boost sprite
      if (shipSizeSpriteCache.shipSize != ShipSize.None) {
         _shipBoostSpritesFront = shipSizeSpriteCache.speedBoostSpriteFront.texture;
         _shipBoostSpritesBack = shipSizeSpriteCache.speedBoostSpriteBack.texture;
      } else {
         shipSizeSpriteCache = shipSizeSpriteList.Find(_ => _.shipSize == shipSize);
         _shipBoostSpritesFront = shipSizeSpriteCache.speedBoostSpriteFront.texture;
         _shipBoostSpritesBack = shipSizeSpriteCache.speedBoostSpriteBack.texture;
      }

      // Set the initial idle sprites
      string skinPath = Ship.getSkinPath(shipType, skinType, isBotShip());
      _shipSprites = ImageManager.getTexture(skinPath);
      spritesContainer.GetComponent<SpriteSwap>().newTexture = _shipSprites;
   }

   protected override void onStartMoving () {
      base.onStartMoving();

      if (!isSpeedingUp) {
         ripplesContainer.GetComponent<SpriteSwap>().newTexture = _ripplesMovingSprites;
      }
   }

   protected override void onEndMoving () {
      base.onEndMoving();

      if (!isSpeedingUp) {
         ripplesContainer.GetComponent<SpriteSwap>().newTexture = _ripplesStillSprites;
      }
   }

   #region Private Variables

   // Ship Ripple SpriteSheets
   protected Texture2D _ripplesStillSprites;
   protected Texture2D _ripplesMovingSprites;
   protected Texture2D _shipBoostSpritesFront, _shipBoostSpritesBack;
   protected Texture2D _shipSprites;

   #endregion
}
