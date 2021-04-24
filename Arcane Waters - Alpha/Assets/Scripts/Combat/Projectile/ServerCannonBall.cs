﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using DG.Tweening;

public class ServerCannonBall : NetworkBehaviour {
   #region Public Variables

   // The force of the cannonball when fired
   [SyncVar]
   public Vector2 projectileVelocity;

   // The projectile id
   [SyncVar]
   public int projectileId;

   #endregion

   private void Awake () {
      _ballCollider = GetComponentInChildren<CircleCollider2D>();
   }

   private void Start () {
      if (!Util.isBatch()) {
         // Play an appropriate sound
         if (_playFiringSound) {
            SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Ship_Cannon_1, this.transform.position);
         }

         Instantiate(PrefabsManager.self.poofPrefab, transform.position, Quaternion.identity);
      }
   }

   public override void OnStartClient () {
      base.OnStartClient();

      _rigidbody.velocity = projectileVelocity;
      _startTime = NetworkTime.time;
   }

   public void init (uint creatorID, int instanceID, Attack.ImpactMagnitude impactType, int abilityId, Vector2 velocity, float lobHeight, bool highShot,
      Status.Type statusType = Status.Type.None, float statusDuration = 3.0f, float lifetime = -1.0f, bool playFiringSound = true, bool isCrit = false) {

      _startTime = NetworkTime.time;
      _creatorNetId = creatorID;
      _instanceId = instanceID;
      _impactMagnitude = impactType;
      _abilityData = ShipAbilityManager.self.getAbility(abilityId);
      _lifetime = lifetime > 0 ? lifetime : DEFAULT_LIFETIME;
      _lobHeight = lobHeight;
      _statusType = statusType;
      _statusDuration = statusDuration;
      _playFiringSound = playFiringSound;
      projectileId = _abilityData.projectileId;
      _distance = velocity.magnitude * _lifetime;
      _isCrit = isCrit;

      this.projectileVelocity = velocity;

      _rigidbody.velocity = velocity;

      // High shots have their colliders disabled until the cannonball falls back near the water
      if (highShot) {
         _ballCollider = GetComponentInChildren<CircleCollider2D>();
         _ballCollider.enabled = false;
         StartCoroutine(CO_SetColliderAfter(_lifetime * 0.9f, true));
      }
   }

   public void addEffectors (List<CannonballEffector> effectors) {
      _effectors.AddRange(effectors);
   }

   private IEnumerator CO_SetColliderAfter(float seconds, bool value) {
      yield return new WaitForSeconds(seconds);
      _ballCollider.enabled = value;
   }

   private void Update () {
      if (_hasCollided) {
         return;
      }

      updateHeight();

      if (isServer) {
         double timeAlive = NetworkTime.time - _startTime;

         if (timeAlive > _lifetime) {
            processDestruction();
         } else if (NetworkTime.time - _lastVelocitySyncTime > 0.2) {
            _lastVelocitySyncTime = NetworkTime.time;
            projectileVelocity = _rigidbody.velocity;
         }  
      }
   }

   private void updateHeight () {
      // Updates the height of the cannonball, to follow a parabolic curve
      float timeAlive = Mathf.Clamp((float) (NetworkTime.time - _startTime), 0.0f, _lifetime);
      float t = (_distance / _lifetime) * timeAlive;
      float cannonBallHeight = Util.getPointOnParabola(_lobHeight, _distance, t);
      Vector3 ballPos = _ballCollider.transform.localPosition;

      ballPos.y = cannonBallHeight;
      _ballCollider.transform.localPosition = ballPos;
   }

   private void FixedUpdate () {
      if (isServer) {
         _rigidbody.velocity = projectileVelocity;
      }
   }

   private void OnTriggerEnter2D (Collider2D other) {
      // Check if the other object is a Sea Entity
      SeaEntity hitEntity = other.transform.GetComponent<SeaEntity>();

      if (hitEntity == null) {
         hitEntity = other.transform.GetComponentInParent<SeaEntity>();
      }

      // We only care about hitting other sea entities in our instance
      if (hitEntity == null || hitEntity.instanceId != this._instanceId || hitEntity.netId == this._creatorNetId) {
         return;
      }

      SeaEntity sourceEntity = SeaManager.self.getEntity(this._creatorNetId);

      // Prevent players from being damaged by other players if they have not entered PvP yet
      if (sourceEntity != null && sourceEntity.isPlayerShip() && !hitEntity.canBeAttackedByPlayers()) {
         return;
      }

      // Ship process if the owner of the projectile and the collided entity has the same voyage, this prevents friendly fire
      if ((sourceEntity.voyageGroupId > 0 || hitEntity.voyageGroupId > 0) && (sourceEntity.voyageGroupId == hitEntity.voyageGroupId)) {
         return;
      }

      _hasCollided = true;

      // The Server will handle applying damage
      if (isServer) {
         _hitEnemy = true;
         Rpc_NotifyHitEnemy();

         ProjectileStatData projectileData = ProjectileStatManager.self.getProjectileData(_abilityData.projectileId);
         int projectileBaseDamage = (int) projectileData.projectileDamage;
         int shipDamage = (int) (sourceEntity.damage * projectileBaseDamage);
         int abilityDamage = (int) (_abilityData.damageModifier * projectileBaseDamage);
         int critDamage = (int) (_isCrit ? projectileBaseDamage * 0.5f : 0.0f);
         int perkDamage = (int) (projectileBaseDamage * PerkManager.self.getPerkMultiplierAdditive(sourceEntity.userId, Perk.Category.ShipDamage));
         int totalDamage = projectileBaseDamage + shipDamage + abilityDamage + critDamage + perkDamage;
         if (hitEntity.currentHealth > 0) {
            hitEntity.currentHealth -= totalDamage;
            sourceEntity.totalDamageDealt += totalDamage;
         }

         if (hitEntity is BotShipEntity) {
            if (hitEntity.currentHealth <= 0) {
               ((BotShipEntity) hitEntity).spawnChest(sourceEntity.userId);
            }
         }

         // TODO: Observe damage formula on live build
         D.adminLog("Total damage of network Cannonball is" + " : " + totalDamage +
            " Projectile: " + projectileBaseDamage +
            " Ship: " + shipDamage + " Dmg: {" + (sourceEntity.damage * 100) + "%}" +
            " Ability: " + abilityDamage + "Dmg: {" + (_abilityData.damageModifier * 100) + "%}" +
            " Perk: " + perkDamage + " Dmg: {" + (PerkManager.self.getPerkMultiplier(sourceEntity.userId, Perk.Category.ShipDamage) * 10) + "%}"
            , D.ADMIN_LOG_TYPE.Sea);

         // Apply the status effect
         if (_statusType != Status.Type.None) {
            hitEntity.applyStatus(_statusType, 1.0f, _statusDuration);
         }

         // Have the server tell the clients where the explosion occurred
         hitEntity.Rpc_ShowExplosion(sourceEntity.netId, transform.position, totalDamage, Attack.Type.Cannon, _isCrit);

         // Registers Damage throughout the clients
         hitEntity.Rpc_NetworkProjectileDamage(_creatorNetId, Attack.Type.Cannon, transform.position);

         // Apply on-hit effectors to the target
         applyEffectorsOnHit(hitEntity);

         // Destroy the projectile
         processDestruction();
      }
   }

   private void processDestruction () {
      if (_cancelDestruction) {
         _cancelDestruction = false;
         return;
      }

      // Detach the Trail Renderer so that it continues to show up a little while longer
      TrailRenderer trail = this.gameObject.GetComponentInChildren<TrailRenderer>();
      if (trail != null) {
         trail.autodestruct = true;
         trail.transform.SetParent(null);

         // For some reason, autodestruct doesn't always work resulting in infinite TrailRenderers being left in the scene, so we force it.
         Destroy(trail.gameObject, 3.0f);
      }

      NetworkServer.Destroy(gameObject);
   }

   [ClientRpc]
   private void Rpc_NotifyHitEnemy () {
      _hitEnemy = true;
   }

   private void OnDestroy () {
      // Don't need to handle any of these effects in Batch Mode
      if (Util.isBatch() || ClientManager.isApplicationQuitting) {
         return;
      }

      bool hitLand = Util.hasLandTile(transform.position);

      playHitSound(hitLand || _hitEnemy);

      // Plays SFX and VFX for land collision
      if (hitLand || _hitEnemy) {
         Instantiate(PrefabsManager.self.requestCannonSmokePrefab(_impactMagnitude), transform.position, Quaternion.identity);
      } else {
         Instantiate(PrefabsManager.self.requestCannonSplashPrefab(_impactMagnitude), transform.position, Quaternion.identity);
      }
   }

   private void playHitSound (bool hitSolid) {
      bool playDefaultSFX = false;
      ProjectileStatData projectileData = ProjectileStatManager.self.getProjectileData(projectileId);
      if (projectileData == null) {
         playDefaultSFX = true;
      } else {
         if (hitSolid) {
            if (projectileData.landHitSFX.Length < 1) {
               playDefaultSFX = true;
            } else {
               playDefaultSFX = false;
            }
         } else {
            if (projectileData.waterHitSFX.Length < 1) {
               playDefaultSFX = true;
            } else {
               playDefaultSFX = false;
            }
         }
      }

      if (hitSolid) {
         if (playDefaultSFX) {
            SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Slash_Lightning, this.transform.position, true);
         } else {
            SoundManager.create3dSoundWithPath(projectileData.landHitSFX, transform.position, projectileData.landHitVol);
         }
      } else {
         if (playDefaultSFX) {
            SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Splash_Cannon_1, this.transform.position, true);
         } else {
            SoundManager.create3dSoundWithPath(projectileData.waterHitSFX, transform.position, projectileData.waterHitVol);
         }
      }
   }

   public int getInstanceId () {
      return _instanceId;
   }

   private void applyEffectorsOnHit (SeaEntity hitEntity) {
      SeaEntity sourceEntity = SeaManager.self.getEntity(this._creatorNetId);

      // Execute the effects of any effectors attached to this cannonball
      foreach (CannonballEffector effector in _effectors) {
         switch (effector.effectorType) {
            case CannonballEffector.Type.Fire:
               hitEntity.applyStatus(Status.Type.Burning, effector.effectStrength, effector.effectDuration);
               break;
            case CannonballEffector.Type.Electric:
               if (PowerupManager.self.powerupActivationRoll(sourceEntity.userId, Powerup.Type.ElectricShots)) {
                  sourceEntity.cannonballChainLightning(_creatorNetId, transform.position, hitEntity.netId, effector.effectRange, effector.effectStrength);
               }
               break;
            case CannonballEffector.Type.Ice:
               hitEntity.applyStatus(Status.Type.Slowed, effector.effectStrength, effector.effectDuration);
               break;
            case CannonballEffector.Type.Explosion:
               createOnHitExplosion(effector, hitEntity);
               break;
            case CannonballEffector.Type.Bouncing:
               handleBounceEffect(effector, hitEntity);
               break;
         }
      }
   }

   private void createOnHitExplosion (CannonballEffector effector, SeaEntity hitEntity) {
      float explosionRadius = effector.effectRange;
      int explosionDamage = (int) effector.effectStrength;
      SeaEntity sourceEntity = SeaManager.self.getEntity(this._creatorNetId);

      PlayerShipEntity player = sourceEntity as PlayerShipEntity;
      if (player) {
         player.rpc.Rpc_ShowExplosiveShotEffect(transform.position, explosionRadius);
      }

      List<SeaEntity> nearbyEnemies = Util.getEnemiesInCircle(sourceEntity, transform.position, explosionRadius);
      foreach(SeaEntity enemy in nearbyEnemies) {
         // The enemy hit by the cannonball won't take splash damage
         if (enemy.netId != hitEntity.netId && enemy.instanceId == _instanceId) {
            // Apply damage
            if (enemy.currentHealth > 0) {
               enemy.currentHealth -= explosionDamage;
               sourceEntity.totalDamageDealt += explosionDamage;
            }

            // Spawn a chest if enemy is killed
            if (enemy is BotShipEntity) {
               if (enemy.currentHealth <= 0) {
                  ((BotShipEntity) enemy).spawnChest(sourceEntity.userId);
               }
            }

            // Registers Damage throughout the clients
            enemy.Rpc_NetworkProjectileDamage(_creatorNetId, Attack.Type.Cannon, enemy.transform.position);
            enemy.Rpc_ShowDamage(Attack.Type.Cannon, enemy.transform.position, explosionDamage);
         }
      }
   }

   private void handleBounceEffect (CannonballEffector effector, SeaEntity hitEntity) {
      int maxBounceCount = (int) effector.effectStrength;
      SeaEntity sourceEntity = SeaManager.self.getEntity(this._creatorNetId);

      // Check if max bounce number has been reached
      if (effector.triggerCount >= maxBounceCount) {
         return;
      }

      float bounceActivationChance = 0.8f - effector.triggerCount * 0.1f;

      // Roll for chance to bounce
      if (UnityEngine.Random.Range(0.0f, 1.0f) <= bounceActivationChance) {
         
         // Find an enemy to bounce to, that isn't the one we hit
         List<SeaEntity> nearbyEnemies = Util.getEnemiesInCircle(sourceEntity, transform.position,  effector.effectRange);
         foreach (SeaEntity enemy in nearbyEnemies) {
            if (enemy.netId != hitEntity.netId && enemy.instanceId == _instanceId) {
               // Setup cannonball variables for new target
               Vector2 toNewEnemy = enemy.transform.position - transform.position;
               _startTime = NetworkTime.time;
               _distance = toNewEnemy.magnitude;
               effector.triggerCount++;
               _hasCollided = false;

               float currentSpeed = _rigidbody.velocity.magnitude;
               projectileVelocity = toNewEnemy.normalized * currentSpeed;
               _rigidbody.velocity = projectileVelocity;
               Rpc_NotifyBounce(projectileVelocity, _distance);

               // Set flag to stop destruction of cannonball
               _cancelDestruction = true;
               break;
            }
         }
      }
   }

   [ClientRpc]
   private void Rpc_NotifyBounce (Vector2 newVelocity, float distance) {
      _rigidbody.velocity = newVelocity;
      _startTime = NetworkTime.time;
      _distance = distance;
      _hasCollided = false;
   }

   #region Private Variables

   // Our rigidbody
   [SerializeField]
   protected Rigidbody2D _rigidbody;

   // The last time we synched velocity
   protected double _lastVelocitySyncTime;

   // Our Start Time
   protected double _startTime;

   // Blocks update func if the projectile collided
   protected bool _hasCollided;

   // The source of this attack
   protected uint _creatorNetId;

   // The instance id for this projectile
   protected int _instanceId;

   // Determines the impact level of this projectile
   protected Attack.ImpactMagnitude _impactMagnitude = Attack.ImpactMagnitude.None;

   // The ability data cache
   protected ShipAbilityData _abilityData;

   // The time in seconds before the cannonball is destroyed
   [SyncVar]
   protected float _lifetime;

   // The distance that this cannonball will travel
   [SyncVar]
   protected float _distance;

   // The default lifetime if none is provided
   protected const float DEFAULT_LIFETIME = 1.25f;

   // A reference to the child object that contains the cannonball
   private CircleCollider2D _ballCollider;

   // How high this cannonball was lobbed
   [SyncVar]
   private float _lobHeight = 0.0f;

   // The status effect that this cannonball will apply to a sea entity it hits
   private Status.Type _statusType = Status.Type.None;

   // How long the status effect applied by this cannonball will last for
   private float _statusDuration = 0.0f;

   // Whether this will play a firing sound effect
   private bool _playFiringSound = true;

   // True if this cannonball hit an enemy
   private bool _hitEnemy = false;

   // Whether this cannonball will deal critical damage
   private bool _isCrit = false;

   // All effectors that will apply effects to this cannonball
   private List<CannonballEffector> _effectors = new List<CannonballEffector>();

   // When set to true, this will prevent processDestruction from destroying the cannonball once
   private bool _cancelDestruction = false;

   #endregion

}
