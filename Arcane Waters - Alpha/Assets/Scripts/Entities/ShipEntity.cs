using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool;
using System;

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

   // A reference to the ship's Rigibody2D component
   public Rigidbody2D rb2d;

   // A manual override for the ship's sprites
   public Texture2D spritesOverride = null;

   // A reference to the transform that will hold visual effects created by casting abilities
   public Transform abilityEffectHolder;

   // A list of directional colliders, indexed by ship size
   public List<GameObject> directionalColliders;

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

      // Activate the appropriate directional collider for our ship size
      int shipSizeIndex = (int) shipSize - 1;
      if (shipSizeIndex < 0) {
         shipSizeIndex = 0;
      }

      for (int i = 0; i < directionalColliders.Count; i++) {
         GameObject directionalCollider = directionalColliders[i];
         directionalCollider.SetActive(i == shipSizeIndex);
      }
   }

   protected virtual void initializeAsSeaEnemy (SeaMonsterEntityData enemyData, ShipData shipData, int instanceDifficulty) {
      shipType = shipData.shipType;
      skinType = shipData.skinType;
      maxHealth = Mathf.RoundToInt(enemyData.maxHealth * (instanceDifficulty > 0 ? instanceDifficulty : 1) * AdminGameSettingsManager.self.settings.seaMaxHealth);
      currentHealth = maxHealth;
      attackRangeModifier = (int) enemyData.maxProjectileDistanceGap;

      float reloadModifier = 1 + (((float) instanceDifficulty - 1) / (Voyage.getMaxDifficulty() - 1));
      reloadDelay = enemyData.reloadDelay / (instanceDifficulty > 0 ? reloadModifier : 1);
      reloadDelay *= AdminGameSettingsManager.self.settings.seaAttackCooldown;

      speed = shipData.baseSpeed;
      sailors = shipData.baseSailors;
      rarity = Rarity.Type.None;

      // TODO: Confirm if damage should be based on the projectile instead of per ship type
      // Damage multiplier does not apply for bot ships, bot ships damage is based on their abilities
      damage = 1;

      shipSize = shipData.shipSize;
      shipSizeSpriteCache = shipSizeSpriteList.Find(_ => _.shipSize == shipSize);

      // Activate the appropriate directional collider for our ship size
      int shipSizeIndex = (int) shipSize - 1;
      if (shipSizeIndex < 0) {
         shipSizeIndex = 0;
      }

      for (int i = 0; i < directionalColliders.Count; i++) {
         GameObject directionalCollider = directionalColliders[i];
         directionalCollider.SetActive(i == shipSizeIndex);
      }
   }

   protected virtual void initialize (ShipInfo info) {
      shipType = info.shipType;
      skinType = info.skinType;
      currentHealth = info.health;
      maxHealth = info.maxHealth;
      _baseHealth = info.maxHealth;
      attackRangeModifier = info.attackRange;

      speed = info.speed;
      sailors = info.sailors;
      rarity = info.rarity;
      damage = info.damage;

      ShipData newShipData = new ShipData();
      if (info.shipXmlId > 0) {
         newShipData = ShipDataManager.self.getShipData(info.shipXmlId);
      } else {
         newShipData = ShipDataManager.self.getShipData(shipType);
      }
      shipSize = newShipData.shipSize;
      shipSizeSpriteCache = shipSizeSpriteList.Find(_ => _.shipSize == shipSize);

      // Activate the appropriate directional collider for our ship size
      int shipSizeIndex = (int) shipSize - 1;
      if (shipSizeIndex < 0) {
         shipSizeIndex = 0;
      }

      for (int i = 0; i < directionalColliders.Count; i++) {
         GameObject directionalCollider = directionalColliders[i];
         directionalCollider.SetActive(i == shipSizeIndex);
      }
   }

   public override void playAttackSound () {
      // Play a sound effect
      //SoundEffectManager.self.playFmodSfx(SoundEffectManager.SHIP_CANNON, this.transform.position);
   }

   public void updateSkin (Ship.SkinType newSkinType) {
      this.skinType = newSkinType;

      StartCoroutine(CO_UpdateAllSprites());
   }

   public bool canUseSkin (Ship.SkinType newSkinType) {
      string skinClass = newSkinType.ToString().Split('_')[0];

      return this.shipType.ToString().ToLower().Contains(skinClass.ToLower());
   }

   protected override bool isInRange (Vector2 position) {
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

      bool hasPvpCaptureTarget = false;
      if (this.isPlayerShip()) {
         PlayerShipEntity playerShip = this as PlayerShipEntity;
         hasPvpCaptureTarget = playerShip.holdingPvpCaptureTarget;
      }

      // Don't apply any speed buff if this entity is holding the capture target
      float speedBuff = (hasPvpCaptureTarget) ? 0.0f : ((getBuffValue(SeaBuff.Category.Buff, SeaBuff.Type.SpeedBoost) * 100));

      // Increase or decrease our speed based on the settings for this ship
      float calculatedSpeed = baseSpeed * ((this.speed + speedBuff) / 100.0f);
      if (calculatedSpeed > MAX_SHIP_SPEED && !isGhost) {
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
   public void Cmd_CastAbility (int shipAbilityId) {
      if (isDead() || !hasReloaded()) {
         return;
      }

      if (isPlayerShip()) {
         PlayerShipEntity playerShip = this as PlayerShipEntity;

         // Return if player ship ability is on cooldown
         if (playerShip.isAbilityOnCooldown(playerShip.selectedShipAbilityIndex)) {
            return;
         }
      }

      // Note the time at which we last successfully attacked
      _lastAttackTime = NetworkTime.time;

      ShipAbilityData shipAbilityData = ShipAbilityManager.self.getAbility(shipAbilityId);
      bool hasUsedBuff = false;

      if (shipAbilityData == null) {
         D.debug("ERROR here! Missing Ship Ability {" + shipAbilityId + "}");
      } else {
         // Self cast buff abilities
         switch (shipAbilityData.selectedAttackType) {
            case Attack.Type.Heal:
               hasUsedBuff = true;
               int healValue = (int) (shipAbilityData.damageModifier * 100);
               if (shipAbilityData.buffRadius > 0) {
                  addBuff(this.netId, SeaBuff.Category.Buff, SeaBuff.Type.Heal, shipAbilityData);
                  Rpc_CastSkill(shipAbilityId, shipAbilityData, transform.position, healValue, true, false, true, this.netId);
               } else {
                  currentHealth += healValue;
                  Rpc_CastSkill(shipAbilityId, shipAbilityData, transform.position, healValue, true, true, true, this.netId);
               }
               break;
            case Attack.Type.SpeedBoost:
               hasUsedBuff = true;
               if (VoyageGroupManager.self.tryGetGroupById(voyageGroupId, out VoyageGroupInfo targetVoyageGroup)) {
                  targetVoyageGroup.addBuffStatsForUser(userId, 1);
                  totalBuffs = targetVoyageGroup.getTotalBuffs(userId);
               }
               addBuff(this.netId, SeaBuff.Category.Buff, SeaBuff.Type.SpeedBoost, shipAbilityData);
               Rpc_CastSkill(shipAbilityId, shipAbilityData, transform.position, 0, true, false, true, this.netId);
               break;
         }

         // Cast abilities to allies if buff radius declared in web tool is greater than 0
         if (hasUsedBuff && shipAbilityData.buffRadius > 0) {
            List<NetEntity> allyEntities = EntityManager.self.getEntitiesWithVoyageId(voyageGroupId);

            if (shipAbilityData.isBuffRadiusDependent) {
               switch (shipAbilityData.selectedAttackType) {
                  case Attack.Type.Heal:
                     // TODO: Do heal stuff logic here
                     StartCoroutine(CO_TriggerActiveAOEBuff(shipAbilityData, shipAbilityData.statusDuration));
                     break;
                  case Attack.Type.SpeedBoost:
                     if (VoyageGroupManager.self.tryGetGroupById(voyageGroupId, out VoyageGroupInfo targetVoyageGroup)) {
                        targetVoyageGroup.addBuffStatsForUser(userId, 1);
                        totalBuffs = targetVoyageGroup.getTotalBuffs(userId);
                     }
                     StartCoroutine(CO_TriggerActiveAOEBuff(shipAbilityData, shipAbilityData.statusDuration));
                     break;
               }
            } else {
               if (allyEntities.Count > 0) {
                  foreach (NetEntity allyEntity in allyEntities) {
                     float distanceBetweenAlly = Vector2.Distance(transform.position, allyEntity.transform.position);
                     if (allyEntity is PlayerShipEntity && userId != allyEntity.userId) {
                        if (distanceBetweenAlly < shipAbilityData.buffRadius) {
                           PlayerShipEntity allyShip = (PlayerShipEntity) allyEntity;
                           switch (shipAbilityData.selectedAttackType) {
                              case Attack.Type.Heal:
                                 StartCoroutine(CO_TriggerOneShotBuff(allyShip, shipAbilityData, Attack.Type.Heal, allyShip.netId, false));
                                 break;
                              case Attack.Type.SpeedBoost:
                                 if (VoyageGroupManager.self.tryGetGroupById(voyageGroupId, out VoyageGroupInfo targetVoyageGroup)) {
                                    targetVoyageGroup.addBuffStatsForUser(userId, 1);
                                    totalBuffs = targetVoyageGroup.getTotalBuffs(userId);
                                 }
                                 allyShip.addBuff(this.netId, SeaBuff.Category.Buff, SeaBuff.Type.SpeedBoost, shipAbilityData);
                                 break;
                           }
                        } else {
                           // TODO: If ally is out of bounds, add logic here if needed
                        }
                     }
                  }
               }
            }
         }
      }

      // Casting a skill is considered a PvP action
      hasEnteredPvP = true;

      if (this.isPlayerShip()) {
         ((PlayerShipEntity) this).hasPerformedFirstActionAfterSpawn = true;
      }
   }

   private IEnumerator CO_TriggerOneShotBuff (PlayerShipEntity targetEntity, ShipAbilityData shipAbilityData, Attack.Type attackType, uint targetNetId, bool snapToTargetInstantly) {
      Rpc_ShowBuffAlly(targetNetId, attackType);

      yield return new WaitForSeconds(1 / BuffOrb.SNAP_SPEED_MULTIPLIER);

      targetEntity.addBuff(netId, SeaBuff.Category.Buff, SeaBuff.Type.Heal, shipAbilityData);
      targetEntity.Rpc_CastSkill(shipAbilityData.abilityId, shipAbilityData, transform.position, 0, true, false, true, targetNetId);
      // Old one shot aoe heal
      /*
      int healValue = (int) shipAbilityData.damageModifier;
      targetEntity.currentHealth += healValue;
      targetEntity.Rpc_CastSkill(shipAbilityData.abilityId, shipAbilityData, targetEntity.transform.position, healValue, true, true);
      */
   }

   private IEnumerator CO_TriggerActiveAOEBuff (ShipAbilityData shipAbilityData, float statusDuration) {
      double endTimeVal = NetworkTime.time + statusDuration;
      List<NetEntity> allyEntities = EntityManager.self.getEntitiesWithVoyageId(voyageGroupId);
      float value = shipAbilityData.damageModifier;
      float refreshDuration = 0.5f;
      while (NetworkTime.time < endTimeVal) {
         yield return new WaitForSeconds(refreshDuration);
         foreach (NetEntity allyEntity in allyEntities) {
            // Skip self
            if (allyEntity.userId == userId) {
               continue;
            }

            PlayerShipEntity allyShip = (PlayerShipEntity) allyEntity;
            float distanceToTarget = Vector2.Distance(transform.position, allyShip.transform.position);
            if (distanceToTarget < shipAbilityData.buffRadius) {
               switch (shipAbilityData.selectedAttackType) {
                  case Attack.Type.Heal:
                     if (allyShip.getBuffData(SeaBuff.Category.Buff, SeaBuff.Type.Heal) == null) {
                        allyShip.addBuff(netId, SeaBuff.Category.Buff, SeaBuff.Type.Heal, shipAbilityData, endTimeVal);
                     }
                     break;
                  case Attack.Type.SpeedBoost:
                     break;
               }
            } else {
               switch (shipAbilityData.selectedAttackType) {
                  case Attack.Type.Heal:
                     if (allyShip.getBuffData(SeaBuff.Category.Buff, SeaBuff.Type.Heal) != null) {
                        SeaBuffData healBuff = allyShip.getBuffData(SeaBuff.Category.Buff, SeaBuff.Type.Heal);
                        allyShip._buffs.Remove(healBuff);
                     }
                     break;
                  case Attack.Type.SpeedBoost:
                     break;
               }
            }
         }
      }
   }

   [ClientRpc]
   public void Rpc_CastSkill (int abilityId, ShipAbilityData shipAbilityData, Vector2 pos, int displayValue, bool showCastVfx, bool showValue, bool showIcon, uint netId) {
      if (shipAbilityData == null) {
         shipAbilityData = ShipAbilityManager.self.getAbility(abilityId);
      }

      if (shipAbilityData == null) {
         D.debug("Missing Ability! {" + abilityId + "}");
         return;
      }

      // Play The effect of the buff
      if (showCastVfx) {
         EffectManager.createDynamicEffect(shipAbilityData.castSpritePath, Vector2.zero, shipAbilityData.abilitySpriteFXPerFrame, abilityEffectHolder, true);
      }

      // We get the source entity to attach the sound effect to it
      SoundEffectManager.self.playSeaAbilitySfx(shipAbilityData.sfxType, netId);

      if (showValue) {
         // Show the damage text
         ShipDamageText damageText = Instantiate(PrefabsManager.self.getTextPrefab(shipAbilityData.selectedAttackType, displayValue < 1), pos, Quaternion.identity);
         if (showIcon) {
            damageText.setIcon(shipAbilityData.skillIconPath);
         } else {
            damageText.icon.gameObject.SetActive(false);
            damageText.text.text = "";
         }
         damageText.negativeEffect = displayValue < 1;
         damageText.setDamage(displayValue);
         if (damageText.notificationText != null) {
            damageText.notificationText.text = shipAbilityData.abilityName;
         }
      }
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

   [Command]
   public void Cmd_PlaySeaAbilitySfx (int abilityId, Vector3 position) {
      Rpc_PlaySeaAbilitySfx(abilityId, position);
   }

   [ClientRpc]
   public void Rpc_PlaySeaAbilitySfx (int abilityId, Vector3 position) {
      if (!Util.isBatch() && isClient) {
         ShipAbilityData shipAbilityData = ShipAbilityManager.self.getAbility(abilityId);
         if (shipAbilityData != null) {
            SoundEffectManager.self.playSeaAbilitySfx(shipAbilityData.sfxType, targetPosition: position);
         }
      }
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

      EffectManager.createDynamicEffect(shipAbilityData.castSpritePath, startPos, shipAbilityData.abilitySpriteFXPerFrame, null);

      // Create a cannon ball
      GenericSeaProjectile ball = Instantiate(PrefabsManager.self.seaEntityProjectile, startPos, Quaternion.identity);
      ball.init(startTime, endTime, startPos, endPos, this, shipAbilityData.abilityId);

      // Play an appropriate sound
      AudioClip clip = AudioClipManager.self.getAudioClipData(shipAbilityData.castSFXPath).audioClip;
      if (clip != null) {
         //SoundManager.playClipAtPoint(clip, Camera.main.transform.position);
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
      overrideSprite(shipType, shipSize, skinType);
   }

   public void overrideSprite (Ship.Type shipType, ShipSize shipSize, Ship.SkinType skinType) {
      // Store the ripple sprites for later so we can quickly swap them once the entity starts/stops moving
      _ripplesStillSprites = ImageManager.getTexture(Ship.getRipplesPath(shipType));
      _ripplesMovingSprites = ImageManager.getTexture(Ship.getRipplesMovingPath(shipType));
      ripplesContainer.GetComponent<SpriteSwap>().newTexture = _ripplesStillSprites;

      // Cache ship boost sprite
      shipSizeSpriteCache = shipSizeSpriteList.Find(_ => _.shipSize == shipSize);
      if (!(this is BotShipEntity)) {
         if (shipSizeSpriteCache != null && shipSizeSpriteCache.shipSize != ShipSize.None) {
            _shipBoostSpritesFront = shipSizeSpriteCache.speedBoostSpriteFront.texture;
            _shipBoostSpritesBack = shipSizeSpriteCache.speedBoostSpriteBack.texture;
            _boostCircleOutline = shipSizeSpriteCache.boostCircleOutline.texture;
            _boostCircleFill = shipSizeSpriteCache.boostCircleFill.texture;
         } else {
            D.debug("cant find ship with size: " + shipSize);
         }
      }

      // Set the initial idle sprites
      string skinPath = Ship.getSkinPath(shipType, skinType, isBotShip());
      _shipSprites = ImageManager.getTexture(skinPath);

      // TODO: Remove player ship entity after batch test
      if (this is PlayerShipEntity) {
         D.adminLog("Ship entity sprite override: {" + skinPath + "} {" + _shipSprites + "} {" + (_shipSprites != null ? _shipSprites.name : "") + "}", D.ADMIN_LOG_TYPE.Simulation_Sea);
      }
      if (spritesOverride) {
         spritesContainer.GetComponent<SpriteSwap>().newTexture = spritesOverride;
      } else {
         spritesContainer.GetComponent<SpriteSwap>().newTexture = _shipSprites;
      }
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

   public void applyBonusHealth (float healthBonusAdditive, bool applyToCurrentHealth = true) {
      int bonusHealth = (int) healthBonusAdditive;
      maxHealth += bonusHealth;
      if (applyToCurrentHealth) {
         currentHealth += bonusHealth;
      }
   }

   #region Private Variables

   // Ship Ripple SpriteSheets
   protected Texture2D _ripplesStillSprites;
   protected Texture2D _ripplesMovingSprites;
   protected Texture2D _shipBoostSpritesFront, _shipBoostSpritesBack;
   protected Texture2D _shipSprites;

   // Boost circle sprites
   protected Texture2D _boostCircleOutline, _boostCircleFill;

   // The base health amount for this ship, stored from its ShipInfo
   protected int _baseHealth;

   #endregion
}
