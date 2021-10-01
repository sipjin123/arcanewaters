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

   #endregion

   protected virtual void Awake () {
      _rigidbody = GetComponent<Rigidbody2D>();
      _circleCollider = GetComponentInChildren<CircleCollider2D>();
   }

   protected virtual void Start () {
      if (!Util.isBatch()) {
         updateVisuals();
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
         }
      }
      _rigidbody.drag = linearDrag;
      _circleCollider = GetComponentInChildren<CircleCollider2D>();

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
         float normalisedTimeAlive = Mathf.Clamp01((float)timeAlive / _lifetime);
         setAlphaForRenderers(alphaOverLifetime.Evaluate(normalisedTimeAlive));
      }
   }

   private void updateHeight () {
      // Updates the height of the projectile, to follow a parabolic curve
      float timeAlive = Mathf.Clamp((float) (NetworkTime.time - _startTime), 0.0f, _lifetime);
      float t = (_distance / _lifetime) * timeAlive;
      float projectileHeight = Util.getPointOnParabola(_lobHeight, _distance, t);
      Vector3 projectilePos = _circleCollider.transform.localPosition;

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
      if (!sourceEntity.isEnemyOf(hitEntity)) {
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
         int shipDamage = (int) (sourceEntity.damage * projectileBaseDamage);
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

         int totalFinalDamage = hitEntity.applyDamage(totalInitialDamage, sourceEntity.netId);
         sourceEntity.totalDamageDealt += totalFinalDamage;

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
            hitEntity.applyStatus(_statusType, 1.0f, _statusDuration, sourceEntity.netId);
         }

         _lastHitEntityNetId = hitEntity.netId;

         onHitEnemy(hitEntity, sourceEntity, totalFinalDamage);

         processDestruction();
      }
   }

   protected virtual void processDestruction () {
      if (_cancelDestruction) {
         _cancelDestruction = false;
         return;
      }

      // If this is a tentacle / poison circle attack, spawn venom
      SeaEntity sourceEntity = SeaManager.self.getEntity(_creatorNetId);
      Area area = AreaManager.self.getArea(sourceEntity.areaKey);
      if (area != null && (_attackType == Attack.Type.Tentacle || _attackType == Attack.Type.Poison_Circle)) {
         VenomResidue venomResidue = Instantiate(PrefabsManager.self.bossVenomResiduePrefab, transform.position, Quaternion.identity);
         venomResidue.creatorNetId = sourceEntity.netId;
         venomResidue.instanceId = _instanceId;
         sourceEntity.Rpc_SpawnBossVenomResidue(sourceEntity.netId, _instanceId, transform.position);
      }

      // If we have a trail renderer, detach it, so it can continue to show while this object is destroyed
      TrailRenderer trail = GetComponentInChildren<TrailRenderer>();
      if (trail != null) {
         trail.autodestruct = true;
         trail.transform.SetParent(null);

         // For some reason, autodestruct doesn't always work resulting in infinite TrailRenderers being left in the scene, so we force it.
         Destroy(trail.gameObject, 3.0f);
      }

      // Check if there is a custom trail and detach it as well
      if (tryGetCustomTrailEffect(out GameObject customTrail)) {
         customTrail.transform.SetParent(null);
         Destroy(customTrail, 3.0f);
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

   protected virtual void OnDestroy () {}

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

   protected virtual bool tryGetCustomTrailEffect (out GameObject trail) {
      trail = null;
      return false;
   }

   #region Private Variables

   // The rigidbody for this projectile
   protected Rigidbody2D _rigidbody;

   // The time at which we were created
   protected double _startTime;

   // Blocks the update function when set to true, which will be after we have collided
   protected bool _hasCollided;

   // The netId of the entity that created this projectile
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
   private CircleCollider2D _circleCollider;

   // How high this projectile was lobbed
   [SyncVar]
   private float _lobHeight = 0.0f;

   // The status effect that this projectile will apply to a sea entity it hits
   protected Status.Type _statusType = Status.Type.None;

   // How long the status effect applied by this projectile will last for
   private float _statusDuration = 0.0f;

   // Set to true once this projectile hits an enemy
   protected bool _hitEnemy = false;

   // Whether this projectile will deal critical damage
   protected bool _isCrit = false;

   // What type of attack this is
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

   #endregion
}