using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;

public class SeaProjectile : NetworkBehaviour
{
   #region Public Variables

   // The velocity of the projectile when fired
   [SyncVar]
   public Vector2 projectileVelocity;

   // The id defining the type of this projectile
   [SyncVar]
   public int projectileTypeId;

   // The id of the sea ability that spawns this projectile
   [SyncVar]
   public int shipAbilityId;

   // The amount of linear drag that will be applied to the rigidbody of this projectile
   [SyncVar]
   public float linearDrag = 0.0f;

   // The sprite renderer reference
   public SpriteRenderer[] spriteRenderers;

   // Whether the sprite should fade out over its lifetime
   public bool fadeOverLifetime = false;

   // A curve defining how the alpha of this projectile will change over its lifetime, if fadeOverLifetime is set to true
   public AnimationCurve alphaOverLifetime;

   // A reference to the shadow renderer for this projectile
   public SpriteRenderer shadowRenderer;

   // Reference to the gameobject that holds the visuals for the animated projectile
   public GenericSpriteEffect animatedGO;

   // Reference to the gameobject that holds the visuals for the static projectile
   public GameObject staticGO;

   // A reference to the particle system that represents the trail for this sea projectile
   public ParticleSystem trailParticles;

   #endregion

   protected virtual void Awake () {
      _rigidbody = GetComponent<Rigidbody2D>();
      _circleCollider = GetComponentInChildren<CircleCollider2D>();
   }

   protected virtual void Start () {
      if (!Util.isBatch()) {
         updateVisuals();
         updateAnimatedVisuals();

         // Play FMOD sfx
         if (_playFiringSound) {
            ShipAbilityData shipAbilityData = null;
            ProjectileStatData projectileStatData = null;

            if (shipAbilityId > 0) {
               shipAbilityData = ShipAbilityManager.self.getAbility(shipAbilityId);
            }
            if (projectileTypeId > 0) {
               projectileStatData = ProjectileStatManager.self.getProjectileData(projectileTypeId);
            }

            SoundEffectManager.SeaAbilityType seaAbilityType = shipAbilityData != null ? shipAbilityData.sfxType : SoundEffectManager.SeaAbilityType.None;
            SoundEffectManager.ProjectileType projectileType = projectileStatData != null ? projectileStatData.sfxType : SoundEffectManager.ProjectileType.None;
            SoundEffectManager.self.playSeaProjectileSfx(seaAbilityType, projectileType, this.transform, this._rigidbody);
         }
      }
   }

   public override void OnStartClient () {
      base.OnStartClient();

      _rigidbody.velocity = projectileVelocity;
      _startTime = NetworkTime.time;
      _rigidbody.drag = linearDrag;

      if (shadowRenderer && _minDropShadowScale != 1.0f && _lobHeight > 0.0f) {
         StartCoroutine(CO_SetShadowHeight(_minDropShadowScale));
      }
   }

   public void initAbilityProjectile (uint creatorID, int instanceID, Attack.ImpactMagnitude impactType, int abilityId, Vector2 velocity, float lobHeight,
      Status.Type statusType = Status.Type.None, float statusDuration = 3.0f, float lifetime = -1.0f, bool isCrit = false, float disableColliderFor = 0.0f,
      float disableColliderAfter = 0.0f, Attack.Type attackType = Attack.Type.None, float minDropShadowScale = 1.0f) {

      init(creatorID, instanceID, impactType, abilityId, velocity, lobHeight, statusType, statusDuration, lifetime, isCrit, disableColliderFor, disableColliderAfter, attackType, -1, minDropShadowScale);
   }

   public void initProjectile (uint creatorID, int instanceID, Attack.ImpactMagnitude impactType, int projectileId, Vector2 velocity, float lobHeight,
      Status.Type statusType = Status.Type.None, float statusDuration = 3.0f, float lifetime = -1.0f, bool isCrit = false, float disableColliderFor = 0.0f,
      float disableColliderAfter = 0.0f, Attack.Type attackType = Attack.Type.None, float minDropShadowScale = 1.0f) {

      init(creatorID, instanceID, impactType, -1, velocity, lobHeight, statusType, statusDuration, lifetime, isCrit, disableColliderFor, disableColliderAfter, attackType, projectileId, minDropShadowScale);
   }

   protected void init (uint creatorID, int instanceID, Attack.ImpactMagnitude impactType, int abilityId, Vector2 velocity, float lobHeight,
      Status.Type statusType = Status.Type.None, float statusDuration = 3.0f, float lifetime = -1.0f, bool isCrit = false, float disableColliderFor = 0.0f,
      float disableColliderAfter = 0.0f, Attack.Type attackType = Attack.Type.None, int projectileId = -1, float minDropShadowScale = 1.0f) {

      _startTime = NetworkTime.time;
      _creatorNetId = creatorID;
      _instanceId = instanceID;
      _impactMagnitude = impactType;
      _lifetime = lifetime > 0 ? lifetime : DEFAULT_LIFETIME;
      _lobHeight = lobHeight;
      _statusType = statusType;
      _statusDuration = statusDuration;
      _distance = velocity.magnitude * _lifetime;
      _isCrit = isCrit;
      _attackType = attackType;
      _minDropShadowScale = minDropShadowScale;

      shipAbilityId = abilityId;

      // If an ability id was provided, get projectile data from that, otherwise use the provided projectile id
      if (abilityId > 0) {
         _abilityData = ShipAbilityManager.self.getAbility(abilityId);
         projectileTypeId = _abilityData.projectileId;
      } else if (projectileId != -1) {
         projectileTypeId = projectileId;
      } else {
         D.error("A valid ability id or projectile id must be provided for a sea projectile.");
      }

      D.adminLog("Projectile id is: " + projectileTypeId + " Ability id is: " + abilityId, D.ADMIN_LOG_TYPE.SeaAbility);

      updateVisuals();

      this.projectileVelocity = velocity;
      _rigidbody.velocity = velocity;
      if (_abilityData != null) {
         ProjectileStatData projectileData = ProjectileStatManager.self.getProjectileData(_abilityData.projectileId);

         if (projectileData != null) {
            //_rigidbody.velocity *= (_abilityData == null ? 1 : projectileData.animationSpeed);
            //_rigidbody.mass = projectileData.projectileMass;
            transform.localScale = new Vector3(projectileData.projectileScale, projectileData.projectileScale, projectileData.projectileScale);
            _circleCollider.radius = projectileData.colliderRadius;
         }
      }
      _rigidbody.drag = linearDrag;
      _circleCollider = GetComponentInChildren<CircleCollider2D>();

      updateAnimatedVisuals();

      // If set to greater than 0, the collider will be disabled for a percentage of its lifetime
      if (disableColliderFor > 0.0f) {
         _circleCollider.enabled = false;
         StartCoroutine(CO_SetColliderAfter(_lifetime * disableColliderFor, true));
      }

      // If set to greater than 0, the collider will be disabled after a percentage of its lifetime
      if (disableColliderAfter > 0.0f) {
         StartCoroutine(CO_SetColliderAfter(_lifetime * disableColliderAfter, false));
      }
   }

   private IEnumerator CO_SetColliderAfter (float seconds, bool value) {
      yield return new WaitForSeconds(seconds);
      _circleCollider.enabled = value;
   }

   private IEnumerator CO_SetShadowHeight (float minShadowScale) {
      float shadowInitialScale = shadowRenderer.transform.localScale.x;

      while (!_hasCollided) {
         float currentHeight = _circleCollider.transform.localPosition.y;
         float heightQuotient = currentHeight / _lobHeight;
         float newShadowScale = Mathf.Lerp(minShadowScale, 1.0f, (1.0f - heightQuotient)) * shadowInitialScale;
         shadowRenderer.transform.localScale = Vector3.one * newShadowScale;
         yield return null;
      }
   }

   protected void updateVisuals () {
      ProjectileStatData projectileData = ProjectileStatManager.self.getProjectileData(projectileTypeId);
      if (projectileData != null) {
         foreach (SpriteRenderer spriteRenderer in spriteRenderers) {
            spriteRenderer.sprite = ImageManager.getSprite(projectileData.projectileSpritePath, true);
         }

         transform.localScale = new Vector3(projectileData.projectileScale, projectileData.projectileScale, projectileData.projectileScale);
      }
      D.adminLog("Projectile id is: " + projectileTypeId, D.ADMIN_LOG_TYPE.SeaAbility);
   }

   protected void updateAnimatedVisuals () {
      ProjectileStatData projectileData = ProjectileStatManager.self.getProjectileData(projectileTypeId);

      if (projectileData == null) {
         return;
      }

      if (animatedGO != null && animatedGO.gameObject != null) {
         animatedGO.gameObject.SetActive(projectileData.isAnimating);
      } else {
         return;
      }

      if (!projectileData.isAnimating) {
         return;
      }

      GenericSpriteEffect spriteEffect = GetComponentInChildren<GenericSpriteEffect>();

      if (spriteEffect == null) {
         D.error("Couldn't find the Generic sprite effect component in the Cannon ball.");
         return;
      }

      spriteEffect.texturePath = projectileData.projectileSpritePath;
      float secondsPerFrame = Mathf.Max(projectileData.animationSpeed, 0.1f) * spriteEffect.secondsPerFrame;
      spriteEffect.secondsPerFrame = Mathf.Max(secondsPerFrame, 0f);
      spriteEffect.isLooping = true;
      spriteEffect.play();

      // Hide the static component of the projectile
      if (staticGO != null) {
         staticGO.SetActive(false);
      }
   }

   protected virtual void Update () {
      if (_hasCollided) {
         return;
      }

      updateHeight();

      double timeAlive = NetworkTime.time - _startTime;

      if (isServer) {
         // If our lifetime has run out, process our destruction
         if (timeAlive > _lifetime) {
            processDestruction();
         }
      }

      if (!Util.isBatch() && fadeOverLifetime) {
         float normalisedTimeAlive = Mathf.Clamp01((float) timeAlive / _lifetime);
         setAlphaForRenderers(alphaOverLifetime.Evaluate(normalisedTimeAlive));
      }
   }

   private void updateHeight () {
      // Updates the height of the projectile, to follow a parabolic curve
      float timeAlive = Mathf.Clamp((float) (NetworkTime.time - _startTime), 0.0f, _lifetime);
      float t = (_distance / _lifetime) * timeAlive;
      float projectileHeight = Util.getPointOnParabola(_lobHeight, _distance, t);
      Vector3 projectilePos = _circleCollider.transform.localPosition;

      // Update dropshadow scale
      if (shadowRenderer != null) {
         float shadowScale = (_lobHeight == 0.0f) ? 1.0f : (1.0f - (projectileHeight / _lobHeight));
         shadowScale = shadowScale * 0.25f + 0.75f;
         shadowRenderer.transform.localScale = Vector3.one * shadowScale;
      }

      // Align sprite direction with velocity
      if (_alignDirectionWithVelocity) {
         float deltaHeight = projectileHeight - _circleCollider.transform.localPosition.y;
         float verticalSpeed = deltaHeight * 1.0f / Time.deltaTime;
         Vector2 verticalVelocity = Mathf.Abs(Vector2.Dot(_rigidbody.velocity.normalized, Vector2.right)) * Vector2.up * verticalSpeed;
         Vector2 perceivedVelocity = _rigidbody.velocity + verticalVelocity;
         SpriteRenderer mainRenderer = spriteRenderers[0];

         if (mainRenderer != null) {
            mainRenderer.transform.rotation = Quaternion.Euler(0.0f, 0.0f, Mathf.Atan2(perceivedVelocity.y, perceivedVelocity.x) * Mathf.Rad2Deg);
         }
      }

      projectilePos.y = projectileHeight;
      _circleCollider.transform.localPosition = projectilePos;
   }

   private void OnTriggerEnter2D (Collider2D collision) {
      // Check if the other object is a sea entity
      SeaEntity hitEntity = collision.transform.GetComponent<SeaEntity>();

      if (hitEntity == null) {
         hitEntity = collision.transform.GetComponentInParent<SeaEntity>();
      }

      // Only hit sea entities in our instance, and don't hit our creator
      if (hitEntity == null || hitEntity.instanceId != this._instanceId || hitEntity.netId == this._creatorNetId) {
         return;
      }

      SeaEntity sourceEntity = SeaManager.self.getEntity(this._creatorNetId);

      // Prevent players from being damaged by other players if they have not entered Pvp yet
      if (sourceEntity != null && sourceEntity.isPlayerShip() && !hitEntity.canBeAttackedByPlayers()) {
         return;
      }

      // Projectiles won't hit dead entities
      if (hitEntity.isDead()) {
         return;
      }

      // Check if the hit entity is an ally
      if (!sourceEntity.isEnemyOf(hitEntity) || sourceEntity.isAllyOf(hitEntity)) {
         return;
      }

      // Don't hit pvp capture target holder
      if (hitEntity.isPvpCaptureTargetHolder()) {
         return;
      }

      // If the hit entity is on the list of enemies to ignore, don't collide
      if (_enemiesToIgnore.Contains(hitEntity.netId)) {
         return;
      }

      // Disallow hitting the entity we hit last (used for bouncing cannonballs)
      if (hitEntity.netId == _lastHitEntityNetId) {
         return;
      }

      _hasCollided = true;

      // The server will handle applying damage
      if (isServer) {
         _hitEnemy = true;
         Rpc_NotifyHitEnemy();

         ProjectileStatData projectileData = ProjectileStatManager.self.getProjectileData(projectileTypeId);
         int projectileBaseDamage = (int) projectileData.projectileDamage;
         int shipDamage = (int) (sourceEntity.damage * projectileBaseDamage * ((sourceEntity.getBuffValue(SeaBuff.Category.Buff, SeaBuff.Type.DamageAmplify) * 100) / 100.0f));

         // Override damage if god mode
         if (sourceEntity is PlayerShipEntity && sourceEntity.isGodMode && sourceEntity.isAdmin()) {
            shipDamage = 99999;
         }
         int abilityDamage = 0;
         if (_abilityData != null) {
            abilityDamage = (int) (_abilityData.damageModifier * projectileBaseDamage);
         }

         int critDamage = (int) (_isCrit ? projectileBaseDamage * 0.5f : 0.0f);
         int perkDamage = 0;
         if (sourceEntity.userId != 0) {
            perkDamage = (int) (projectileBaseDamage * PerkManager.self.getPerkMultiplierAdditive(sourceEntity.userId, Perk.Category.ShipDamage));
         }
         int totalInitialDamage = projectileBaseDamage + shipDamage + abilityDamage + critDamage + perkDamage;

         // Add damage for trinket based buffs
         if (sourceEntity is PlayerShipEntity) {
            PlayerShipEntity shipEntity = (PlayerShipEntity) sourceEntity;
            if (shipEntity.trinketType > 0) {
               TrinketStatData trinketData = EquipmentXMLManager.self.getTrinketData(shipEntity.trinketType);
               if (trinketData != null) {
                  if (trinketData.gearBuffType == EquipmentStatData.GearBuffType.PlayerDamageBoost) {
                     if (trinketData.isBuffPercentage) {
                        totalInitialDamage += (int) (totalInitialDamage * (trinketData.itemBuffValue * 0.01));
                     } else {
                        totalInitialDamage += (int) trinketData.itemBuffValue;
                     }
                  }
               }
            }
         }
         int totalFinalDamage = hitEntity.applyDamage(totalInitialDamage, sourceEntity.netId, _attackType);

         string abilityDataDamageModifier = (_abilityData != null) ? (_abilityData.damageModifier * 100).ToString() : "0";
         // TODO: Observe damage formula on live build
         D.adminLog("Total damage of network Cannonball is" + " : " + totalFinalDamage +
            " Projectile: " + projectileBaseDamage +
            " Ship: " + shipDamage + " Dmg: {" + (sourceEntity.damage * 100) + "%}" +
            " Ability: " + abilityDamage + "Dmg: {" + abilityDataDamageModifier + "%}" +
            " Perk: " + perkDamage
            , D.ADMIN_LOG_TYPE.Sea);

         // Apply the status effect
         if (_statusType != Status.Type.None) {
            hitEntity.applyStatus(_statusType, projectileData.projectileDamage * .1f, _statusDuration, sourceEntity.netId);
         }

         _lastHitEntityNetId = hitEntity.netId;

         onHitEnemy(hitEntity, sourceEntity, totalFinalDamage);

         processDestruction();
      }
   }

   [Server]
   protected virtual void processDestruction () {
      if (_cancelDestruction) {
         _cancelDestruction = false;
         return;
      }

      SeaEntity sourceEntity = SeaManager.self.getEntity(_creatorNetId);
      bool hitSeaTile = !Util.hasLandTile(transform.position, _circleCollider.radius, sourceEntity.areaKey);

      // If this is a tentacle / poison circle attack, spawn venom
      if ((_attackType == Attack.Type.Tentacle || _attackType == Attack.Type.Poison_Circle)) {
         // Only spawn residue if we hit a sea tile
         if (hitSeaTile) {
            VenomResidue venomResidue = Instantiate(PrefabsManager.self.bossVenomResiduePrefab, transform.position, Quaternion.identity);
            venomResidue.creatorNetId = sourceEntity.netId;
            venomResidue.instanceId = _instanceId;
         }

         sourceEntity.Rpc_SpawnBossVenomResidue(sourceEntity.netId, _instanceId, transform.position, hitSeaTile);

         // If this is a mine attack, spawn a mine
      } else if (_attackType == Attack.Type.Mine) {
         if (hitSeaTile) {
            SeaMine seaMine = Instantiate(PrefabsManager.self.seaMinePrefab, transform.position, Quaternion.identity);
            seaMine.init(_instanceId, _creatorNetId, 0.6f, 20.0f);

            PlayerShipEntity playerShip = sourceEntity.getPlayerShipEntity();
            if (playerShip != null) {
               playerShip.trackSeaMine(seaMine);
            }

            NetworkServer.Spawn(seaMine.gameObject);
         }
      }

      // If this ability has any knockback, apply it now
      ProjectileStatData projectileData = ProjectileStatManager.self.getProjectileData(projectileTypeId);
      if (projectileData != null && projectileData.knockbackForce != 0.0f && projectileData.knockbackRadius > 0.0f) {

         bool isKnockback = projectileData.knockbackForce >= 0.0f;

         // Create a point effector to apply forces to entities
         GameObject pointEffector = Instantiate(PrefabsManager.self.pointEffectorPrefab, transform.position, Quaternion.identity, null);
         pointEffector.GetComponent<CircleCollider2D>().radius = projectileData.knockbackRadius;
         pointEffector.GetComponent<PointEffector2D>().forceMagnitude = projectileData.knockbackForce;
         float effectorDuration = (isKnockback) ? 0.5f : 2.5f;
         Destroy(pointEffector, effectorDuration);

         if (isKnockback) {
            sourceEntity.rpc.Rpc_ShowKnockbackEffect(transform.position, projectileData.knockbackRadius);
         } else {
            sourceEntity.rpc.Rpc_ShowWhirlpoolEffect(transform.position, projectileData.knockbackRadius);
         }
      }

      NetworkServer.Destroy(gameObject);
   }

   protected virtual void onHitEnemy (SeaEntity hitEntity, SeaEntity sourceEntity, int finalDamage) {
      hitEntity.Rpc_NetworkProjectileDamage(sourceEntity.netId, _attackType, transform.position);
      if (_showDamageNumber) {
         hitEntity.Rpc_ShowDamageTaken(finalDamage, false);
      }

      // If this is a shock ball attack, spawn chain lightning
      if (_attackType == Attack.Type.Shock_Ball) {
         sourceEntity.chainLightning(sourceEntity.netId, hitEntity.transform.position, hitEntity.netId);
      }
   }

   [ClientRpc]
   private void Rpc_NotifyHitEnemy () {
      _hitEnemy = true;
   }

   public override void OnStopClient () {
      base.OnStopClient();

      // Don't need to handle any of these effects in Batch Mode
      if (Util.isBatch() || ClientManager.isApplicationQuitting) {
         return;
      }

      // If we have a trail renderer, detach it, so it can continue to show while this object is destroyed
      TrailRenderer trail = GetComponentInChildren<TrailRenderer>();
      if (trail != null) {
         trail.autodestruct = true;
         trail.transform.SetParent(null);

         // For some reason, autodestruct doesn't always work resulting in infinite TrailRenderers being left in the scene, so we force it.
         Destroy(trail.gameObject, 3.0f);
      }
   }

   public int getInstanceId () {
      return _instanceId;
   }

   public void addIgnoredEnemy (uint enemyNetId) {
      if (!_enemiesToIgnore.Contains(enemyNetId)) {
         _enemiesToIgnore.Add(enemyNetId);
      }
   }

   private void setAlphaForRenderers (float newAlpha) {
      foreach (SpriteRenderer renderer in spriteRenderers) {
         Util.setAlpha(renderer, newAlpha);
      }
   }

   public void setPlayFiringSound (bool value) {
      _playFiringSound = value;
   }

   protected virtual void OnDestroy () { }

   protected bool hitSeaStructureIsland () {
      int layerMask = LayerMask.GetMask(LayerUtil.SEA_STRUCTURES);

      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.05f, layerMask);

      foreach (Collider2D hit in hits) {
         if (hit.GetComponent<SeaStructure>()) {
            return true;
         }
      }

      return false;
   }

   protected void playHitSound (bool hitLand, bool hitEnemy) {
      ProjectileStatData projectileData = ProjectileStatManager.self.getProjectileData(projectileTypeId);
      SoundEffectManager.ProjectileType sfxType = SoundEffectManager.ProjectileType.None;
      if (projectileData != null) {
         sfxType = projectileData.sfxType;
      }
      SoundEffectManager.self.playProjectileTerrainHitSound(hitLand, hitEnemy, sfxType, this.transform, this._rigidbody);
   }

   #region Private Variables

   // The rigidbody for this projectile
   protected Rigidbody2D _rigidbody;

   // The time at which we were created
   protected double _startTime;

   // Blocks the update function when set to true, which will be after we have collided
   protected bool _hasCollided;

   // The netId of the entity that created this projectile
   [SyncVar]
   protected uint _creatorNetId;

   // The id of the instance that this projectile is in
   protected int _instanceId;

   // Determines the magnitude of the impact of this projectile, for playing effects
   protected Attack.ImpactMagnitude _impactMagnitude = Attack.ImpactMagnitude.None;

   // The cached data for the ability of this projectile
   protected ShipAbilityData _abilityData;

   // The time before this projectile is destroyed
   [SyncVar]
   protected float _lifetime;

   // The distance that this projectile will travel
   [SyncVar]
   protected float _distance;

   // The default lifetime for the projectile, if none is provided
   protected const float DEFAULT_LIFETIME = 1.25f;

   // A reference to the circle collider for this projectile
   protected CircleCollider2D _circleCollider;

   // How high this projectile was lobbed
   [SyncVar]
   private float _lobHeight = 0.0f;

   // The status effect that this projectile will apply to a sea entity it hits
   [SyncVar]
   protected Status.Type _statusType = Status.Type.None;

   // How long the status effect applied by this projectile will last for
   private float _statusDuration = 0.0f;

   // Set to true once this projectile hits an enemy
   protected bool _hitEnemy = false;

   // Whether this projectile will deal critical damage
   protected bool _isCrit = false;

   // What type of attack this is
   [SyncVar]
   protected Attack.Type _attackType = Attack.Type.None;

   // A list of netIds of enemies that will be ignored by this projectile
   protected SyncList<uint> _enemiesToIgnore = new SyncList<uint>();

   // Whether this projectile will automatically show damage numbers for damage it inflicts, set to false if you want to override this functionality
   protected bool _showDamageNumber = true;

   // When set to true, this will prevent processDestruction from destroying the projectile once
   protected bool _cancelDestruction = false;

   // The net ID of the last entity this projectile hit
   private uint _lastHitEntityNetId = 0;

   // The minimum size of the drop shadow for this projectile - when it's at the height of its arc
   private float _minDropShadowScale = 1.0f;

   // Controls how the explosive force of a projectile drops off as you reach the edge of its range
   private const float EXPLOSIVE_FORCE_DROPOFF = 0.25f;

   // Whether this will play a firing sound effect
   protected bool _playFiringSound = true;

   // Whether the sprite for this projectile will be rotated to align with its velocity
   protected bool _alignDirectionWithVelocity = false;

   #endregion
}