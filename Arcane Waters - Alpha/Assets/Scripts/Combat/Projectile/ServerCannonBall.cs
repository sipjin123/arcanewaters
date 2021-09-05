using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using DG.Tweening;

public class ServerCannonBall : SeaProjectile
{
   #region Public Variables

   // A reference to the particle system that represents the trail for this cannonball
   public ParticleSystem trailParticles;

   #endregion

   protected override void Awake () {
      base.Awake();

      _showDamageNumber = false;
   }

   protected override void Start () {
      base.Start();

      if (!Util.isBatch()) {
         // Play an appropriate sound
         if (_playFiringSound) {
            _shipCannonEvent = SoundEffectManager.self.getEventInstance(SoundEffectManager.SHIP_CANNON);
            _shipCannonEvent.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(transform));
            _shipCannonEvent.start();

            //SoundEffectManager.self.playFmodSoundEffect(SoundEffectManager.SHIP_CANNON, this.transform);
            //SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Ship_Cannon_1, this.transform.position);
         }

         Instantiate(PrefabsManager.self.poofPrefab, transform.position, Quaternion.identity);

         initTrail();
      }
   }

   public void addEffectors (List<CannonballEffector> effectors) {
      _effectors.AddRange(effectors);
   }

   private void initTrail () {
      Material trailMaterial = Resources.Load<Material>("Materials/CannonballTrail" + _statusType.ToString());
      if (trailMaterial != null) {
         ParticleSystemRenderer trailRenderer = trailParticles.GetComponent<ParticleSystemRenderer>();
         trailRenderer.sharedMaterial = trailMaterial;
      }
   }

   protected override void Update () {
      base.Update();

      if (_hasCollided) {
         return;
      }

      // Attaching the SFX to our position
      FMODUnity.RuntimeManager.AttachInstanceToGameObject(_shipCannonEvent, transform, _rigidbody);
   }

   protected override void onHitEnemy (SeaEntity hitEntity, SeaEntity sourceEntity, int finalDamage) {
      base.onHitEnemy(hitEntity, sourceEntity, finalDamage);

      // Have the server tell the clients where the explosion occurred
      hitEntity.Rpc_ShowExplosion(sourceEntity.netId, transform.position, finalDamage, Attack.Type.Cannon, _isCrit);

      // Apply on-hit effectors to the target
      applyEffectorsOnHit(hitEntity, _isCrit);
   }

   protected override void OnDestroy () {
      base.OnDestroy();

      // Don't need to handle any of these effects in Batch Mode
      if (Util.isBatch() || ClientManager.isApplicationQuitting) {
         return;
      }

      bool hitLand = Util.hasLandTile(transform.position) || hitSeaStructureIsland();

      playHitSound(hitLand, _hitEnemy);

      // Plays SFX and VFX for land collision
      if (hitLand || _hitEnemy) {
         Instantiate(PrefabsManager.self.requestCannonSmokePrefab(_impactMagnitude), transform.position, Quaternion.identity);
      } else {
         Instantiate(PrefabsManager.self.requestCannonSplashPrefab(_impactMagnitude), transform.position, Quaternion.identity);
      }

      _shipCannonEvent.release();
   }

   private bool hitSeaStructureIsland () {
      int layerMask = LayerMask.GetMask(LayerUtil.SEA_STRUCTURES);

      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.05f, layerMask);

      foreach (Collider2D hit in hits) {
         if (hit.GetComponent<SeaStructure>()) {
            return true;
         }
      }

      return false;
   }

   private void playHitSound (bool hitLand, bool hitEnemy) {
      bool playDefaultSFX = false;

      bool hitSolid = hitLand || hitEnemy;

      ProjectileStatData projectileData = ProjectileStatManager.self.getProjectileData(projectileTypeId);
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

      if (hitLand) {
         if (playDefaultSFX) {
            SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Slash_Lightning, this.transform.position, true);
         } else {
            SoundManager.create3dSoundWithPath(projectileData.landHitSFX, transform.position, projectileData.landHitVol);
         }
      } else if (!hitEnemy) {
         // FMOD sfx for water
         SoundEffectManager.self.playCannonballImpact(SoundEffectManager.CannonballImpactType.Water, this.transform.position);

         //if (playDefaultSFX) {
         //   SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Splash_Cannon_1, this.transform.position, true);
         //} else {
         //   SoundManager.create3dSoundWithPath(projectileData.waterHitSFX, transform.position, projectileData.waterHitVol);
         //}
      }
   }

   private void applyEffectorsOnHit (SeaEntity hitEntity, bool isCrit) {
      SeaEntity sourceEntity = SeaManager.self.getEntity(this._creatorNetId);

      // Execute the effects of any effectors attached to this cannonball
      foreach (CannonballEffector effector in _effectors) {
         switch (effector.effectorType) {
            case CannonballEffector.Type.Fire:
               hitEntity.applyStatus(Status.Type.Burning, effector.effectStrength, effector.effectDuration, sourceEntity.netId);
               break;
            case CannonballEffector.Type.Electric:
               if (sourceEntity.userId > 0 && PowerupManager.self.powerupActivationRoll(sourceEntity.userId, Powerup.Type.ElectricShots)) {
                  sourceEntity.cannonballChainLightning(_creatorNetId, transform.position, hitEntity.netId, effector.effectRange, effector.effectStrength);
               } else {
                  // Bot ships have a flat 75% chance to trigger chain lightning for now
                  if (UnityEngine.Random.Range(0.0f, 1.0f) <= 0.75f) {
                     sourceEntity.cannonballChainLightning(_creatorNetId, transform.position, hitEntity.netId, effector.effectRange, effector.effectStrength);
                  }
               }
               break;
            case CannonballEffector.Type.Ice:
               hitEntity.applyStatus(Status.Type.Slowed, effector.effectStrength, effector.effectDuration, sourceEntity.netId);
               break;
            case CannonballEffector.Type.Explosion:
               createOnHitExplosion(effector, hitEntity);
               break;
            case CannonballEffector.Type.Bouncing:
               handleBounceEffect(effector, hitEntity);
               break;
         }

         // Play SFX for effector
         hitEntity.Rpc_PlayHitSfx(hitEntity is ShipEntity, isCrit, effector.effectorType, transform.position);
      }

      // If the cannonball doesn't have effectors applied
      if (_effectors.Count == 0) {
         hitEntity.Rpc_PlayHitSfx(hitEntity is ShipEntity, isCrit, CannonballEffector.Type.None, transform.position);
      }
   }

   private void createOnHitExplosion (CannonballEffector effector, SeaEntity hitEntity) {
      float explosionRadius = effector.effectRange;
      SeaEntity sourceEntity = SeaManager.self.getEntity(this._creatorNetId);

      if (sourceEntity) {
         sourceEntity.Rpc_ShowExplosiveShotEffect(transform.position, explosionRadius);
      }

      // Gives us a value from ~0.1-0.6, more if powerups are stacked
      float effectorRangeMultiplier = ((effector.effectRange / 0.6f) - 1.0f);
      int numProjectiles = 3 + Mathf.RoundToInt(effectorRangeMultiplier * 10.0f);
      numProjectiles = Mathf.Clamp(numProjectiles, 3, 16);
      float projectileVelocity = 1.5f * (1.0f + effectorRangeMultiplier * 0.5f);

      const float ANIM_DURATION = 1.05f;
      float anglePerProjectile = 360.0f / numProjectiles;
      float spawnAngle = 0.0f;

      for (int i = 0; i < numProjectiles; i++) {
         Quaternion spawnAngleQuaternion = Quaternion.Euler(0.0f, 0.0f, spawnAngle);
         SeaProjectile projectile = Instantiate(PrefabsManager.self.explosiveShotProjectilePrefab, transform.position, spawnAngleQuaternion, null);
         if (projectile) {
            projectile.linearDrag = 1.0f;

            Vector2 velocity = spawnAngleQuaternion * Vector3.up * projectileVelocity;

            projectile.initProjectile(_creatorNetId, _instanceId, Attack.ImpactMagnitude.Normal, EXPLOSIVE_SHOT_PROJECTILE_ID, velocity, 0.0f, lifetime: ANIM_DURATION, disableColliderAfter: 0.75f);
            projectile.addIgnoredEnemy(hitEntity.netId);

            NetworkServer.Spawn(projectile.gameObject);

            spawnAngle += anglePerProjectile;
         } else {
            D.debug("Missing Prefab for SeaProjectile!");
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
         float currentSpeed = _rigidbody.velocity.magnitude;
         float maxRange = _lifetime * currentSpeed;

         // Find an enemy to bounce to, that isn't the one we hit
         List <SeaEntity> nearbyEnemies = Util.getEnemiesInCircle(sourceEntity, transform.position, maxRange);
         foreach (SeaEntity enemy in nearbyEnemies) {
            if (enemy.netId != hitEntity.netId) {
               // Setup cannonball variables for new target
               Vector2 toNewEnemy = enemy.transform.position - transform.position;
               _startTime = NetworkTime.time;
               _distance = toNewEnemy.magnitude;
               effector.triggerCount++;
               _hasCollided = false;
               
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

   public void setPlayFiringSound (bool value) {
      _playFiringSound = value;
   }

   #region Private Variables

   // Whether this will play a firing sound effect
   private bool _playFiringSound = true;

   // All effectors that will apply effects to this cannonball
   private List<CannonballEffector> _effectors = new List<CannonballEffector>();

   // FMOD Event instance
   FMOD.Studio.EventInstance _shipCannonEvent;

   // The projectile id for the explosive shot projectile
   private const int EXPLOSIVE_SHOT_PROJECTILE_ID = 33;

   #endregion

}
