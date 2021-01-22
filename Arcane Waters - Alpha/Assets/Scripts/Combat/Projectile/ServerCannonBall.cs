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

         // Create a cannon smoke effect
         Instantiate(PrefabsManager.self.requestCannonSmokePrefab(Attack.ImpactMagnitude.Normal), transform.position, Quaternion.identity);
      }
   }

   public override void OnStartClient () {
      base.OnStartClient();

      _rigidbody.velocity = projectileVelocity;
      _startTime = NetworkTime.time;

   }

   [Server]
   public void init (uint creatorID, int instanceID, Attack.ImpactMagnitude impactType, int abilityId, Vector2 startPos, Vector2 velocity, Status.Type statusType = Status.Type.None, float statusDuration = 3.0f, float lifetime = -1, float damageMultiplier = 1, bool playFiringSound = true) {      
      _startTime = NetworkTime.time;
      _creatorNetId = creatorID;
      _instanceId = instanceID;
      _impactMagnitude = impactType;
      _abilityData = ShipAbilityManager.self.getAbility(abilityId);
      _lifetime = lifetime > 0 ? lifetime : DEFAULT_LIFETIME;
      _damageMultiplier = damageMultiplier;
      _lobHeight = 0.1f;
      _statusType = statusType;
      _statusDuration = statusDuration;
      _playFiringSound = playFiringSound;

      this.projectileVelocity = velocity;

      _rigidbody.velocity = velocity;
      transform.position = startPos;
   }

   [Server]
   public void initLob (uint creatorID, int instanceID, Attack.ImpactMagnitude impactType, int abilityId, Vector2 startPos, Vector2 velocity, float lobHeight, Status.Type statusType = Status.Type.None, float statusDuration = 3.0f, float lifetime = -1, float damageMultiplier = 1, bool playFiringSound = true) {
      _startTime = NetworkTime.time;
      _creatorNetId = creatorID;
      _instanceId = instanceID;
      _impactMagnitude = impactType;
      _abilityData = ShipAbilityManager.self.getAbility(abilityId);
      _lifetime = lifetime > 0 ? lifetime : DEFAULT_LIFETIME;
      _damageMultiplier = damageMultiplier;
      _lobHeight = lobHeight;
      _statusType = statusType;
      _statusDuration = statusDuration;
      _playFiringSound = playFiringSound;

      this.projectileVelocity = velocity;

      _rigidbody.velocity = velocity;
      transform.position = startPos;

      // Disable collider until ball is near the ground
      _ballCollider = GetComponentInChildren<CircleCollider2D>();
      _ballCollider.enabled = false;
      StartCoroutine(CO_SetColliderAfter(_lifetime * 0.9f, true));
   }

   private IEnumerator CO_SetColliderAfter(float seconds, bool value) {
      yield return new WaitForSeconds(seconds);
      _ballCollider.enabled = value;
   }

   private void Update () {
      updateHeight();

      if (isServer) {
         double timeAlive = NetworkTime.time - _startTime;

         if (timeAlive > _lifetime && !_hasCollided) {
            processDestruction();
         } else if (NetworkTime.time - _lastVelocitySyncTime > 0.2) {
            _lastVelocitySyncTime = NetworkTime.time;
            projectileVelocity = _rigidbody.velocity;
         }  
      }
   }

   private void updateHeight () {
      // Updates the height of the cannonball, to follow a parabolic curve

      // Calculate the 'a' constant for the quadratic graph equation
      float a = (4 * _lobHeight) / -(_lifetime * _lifetime);
      float timeAlive = (float)(NetworkTime.time - _startTime);
      Vector3 ballPos = _ballCollider.transform.localPosition;

      // Use a quadratic graph equation to determine the height (y = a * x * (x - k))
      ballPos.y = a * timeAlive * (timeAlive - _lifetime);
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

      _hasCollided = true;

      // The Server will handle applying damage
      if (isServer) {
         // Ship process if the owner of the projectile and the collided entity has the same voyage, this prevents friendly fire
         if ((sourceEntity.voyageGroupId > 0 || hitEntity.voyageGroupId > 0) && (sourceEntity.voyageGroupId == hitEntity.voyageGroupId)) {
            return;
         }

         ProjectileStatData projectileData = ProjectileStatManager.self.getProjectileData(_abilityData.projectileId);
         int projectileBaseDamage = (int) projectileData.projectileDamage;
         int shipDamageMultiplier = (int) ((sourceEntity.damage*.001f) * projectileBaseDamage);
         int abilityDamageMultiplier = (int) ((_abilityData.damageModifier * .001f) * projectileBaseDamage);
         int totalDamage = projectileBaseDamage + shipDamageMultiplier + abilityDamageMultiplier;
         hitEntity.currentHealth -= totalDamage;

         // TODO: Observe damage formula on live build
         D.editorLog("Total damage of network Cannonball is" + " : " + totalDamage + " Projectile: " + projectileBaseDamage + " Ship: " + shipDamageMultiplier + " Ability: " + abilityDamageMultiplier, Color.cyan);

         // Apply the status effect
         if (_statusType != Status.Type.None) {
            hitEntity.applyStatus(_statusType, _statusDuration);
         }

         // Have the server tell the clients where the explosion occurred
         hitEntity.Rpc_ShowExplosion(sourceEntity.netId, transform.position, totalDamage, Attack.Type.Cannon);

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

   private void OnDestroy () {
      // Don't need to handle any of these effects in Batch Mode
      if (Util.isBatch()) {
         return;
      }

      bool hitLand = Util.hasLandTile(transform.position);

      // Plays SFX and VFX for land collision
      if (hitLand) {
         Instantiate(PrefabsManager.self.requestCannonSmokePrefab(_impactMagnitude), transform.position, Quaternion.identity);
         SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Slash_Lightning, this.transform.position);
      } else {
         Instantiate(PrefabsManager.self.requestCannonSplashPrefab(_impactMagnitude), transform.position, Quaternion.identity);
         SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Splash_Cannon_1, this.transform.position);
      }
   }

   #region Private Variables

   // Our rigidbody
   [SerializeField]
   protected Rigidbody2D _rigidbody;

   // The last time we synched velocity
   protected double _lastVelocitySyncTime;

   // Our Start Time
   [SyncVar]
   protected double _startTime;

   // Blocks update func if the projectile collided
   protected bool _hasCollided;

   // The source of this attack
   protected uint _creatorNetId;

   // The instance id for this projectile
   protected int _instanceId;

   // The damage multiplier of this projectile considering the current travel force
   protected float _damageMultiplier = 1;

   // Determines the impact level of this projectile
   protected Attack.ImpactMagnitude _impactMagnitude = Attack.ImpactMagnitude.None;

   // The ability data cache
   protected ShipAbilityData _abilityData;

   // The time in seconds before the cannonball is destroyed
   [SyncVar]
   protected float _lifetime;

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

   #endregion

}
