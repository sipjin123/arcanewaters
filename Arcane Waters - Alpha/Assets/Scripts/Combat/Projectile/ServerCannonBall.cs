using UnityEngine;
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
         int totalDamage = projectileBaseDamage + shipDamage + abilityDamage + critDamage;
         if (hitEntity.currentHealth > 0) {
            hitEntity.currentHealth -= totalDamage;
         }

         if (hitEntity is BotShipEntity) {
            if (hitEntity.currentHealth <= 0) {
               ((BotShipEntity) hitEntity).spawnChest();
            }
            sourceEntity.rpc.notifyVoyageMembers(VoyageGroupMemberCell.NotificationType.Engage, sourceEntity);
         }

         // TODO: Observe damage formula on live build
         D.adminLog("Total damage of network Cannonball is" + " : " + totalDamage +
            " Projectile: " + projectileBaseDamage +
            " Ship: " + shipDamage + " Dmg: {" + (sourceEntity.damage * 100) + "%}" +
            " Ability: " + abilityDamage + "Dmg: {" + (_abilityData.damageModifier * 100) + "%}", D.ADMIN_LOG_TYPE.Sea);

         // Apply the status effect
         if (_statusType != Status.Type.None) {
            hitEntity.applyStatus(_statusType, _statusDuration);
         }

         // Have the server tell the clients where the explosion occurred
         hitEntity.Rpc_ShowExplosion(sourceEntity.netId, transform.position, totalDamage, Attack.Type.Cannon, _isCrit);

         // Registers Damage throughout the clients
         hitEntity.Rpc_NetworkProjectileDamage(_creatorNetId, Attack.Type.Cannon, transform.position);

         // Destroy the projectile
         processDestruction();
      }
   }

   private void processDestruction () {
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

      bool playDefaultSFX = false;
      ProjectileStatData projectileData = ProjectileStatManager.self.getProjectileData(projectileId);
      if (projectileData == null) {
         playDefaultSFX = true;
      } else {
         if (hitLand || _hitEnemy) {
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

      // Plays SFX and VFX for land collision
      if (hitLand || _hitEnemy) {
         Instantiate(PrefabsManager.self.requestCannonSmokePrefab(_impactMagnitude), transform.position, Quaternion.identity);
         if (playDefaultSFX) {
            SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Slash_Lightning, this.transform.position, true);
         } else {
            SoundManager.create3dSoundWithPath(projectileData.landHitSFX, transform.position, projectileData.landHitVol);
         }
      } else {
         Instantiate(PrefabsManager.self.requestCannonSplashPrefab(_impactMagnitude), transform.position, Quaternion.identity);
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

   #endregion

}
