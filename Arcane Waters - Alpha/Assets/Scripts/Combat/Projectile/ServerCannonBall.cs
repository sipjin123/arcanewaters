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

   #endregion

   private void Start () {
      if (!Util.isBatch()) {
         // Play an appropriate sound
         SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Ship_Cannon_1, this.transform.position);

         // Create a cannon smoke effect
         Instantiate(PrefabsManager.self.requestCannonSmokePrefab(Attack.ImpactMagnitude.Normal), transform.position, Quaternion.identity);
      }
   }

   public override void OnStartClient () {
      base.OnStartClient();

      _rigidbody.velocity = projectileVelocity;
   }

   [Server]
   public void init (uint creatorID, int instanceID, Attack.ImpactMagnitude impactType, int abilityId, Vector2 startPos, Vector2 velocity, float lifetime = -1, float damageMultiplier = 1) {      
      _startTime = NetworkTime.time;
      _creatorNetId = creatorID;
      _instanceId = instanceID;
      _impactMagnitude = impactType;
      _abilityData = ShipAbilityManager.self.getAbility(abilityId);
      _lifetime = lifetime > 0 ? lifetime : DEFAULT_LIFETIME;
      _damageMultiplier = damageMultiplier;

      this.projectileVelocity = velocity;

      _rigidbody.velocity = velocity;
      transform.position = startPos;
   }

   [Server]
   public void initLob (uint creatorID, int instanceID, Attack.ImpactMagnitude impactType, int abilityId, Vector2 startPos, Vector2 velocity, float lobHeight, float lifetime = -1, float damageMultiplier = 1) {
      _startTime = NetworkTime.time;
      _creatorNetId = creatorID;
      _instanceId = instanceID;
      _impactMagnitude = impactType;
      _abilityData = ShipAbilityManager.self.getAbility(abilityId);
      _lifetime = lifetime > 0 ? lifetime : DEFAULT_LIFETIME;
      _damageMultiplier = damageMultiplier;
      _lobHeight = lobHeight;
      _isLobbed = true;

      this.projectileVelocity = velocity;

      _rigidbody.velocity = velocity;
      transform.position = startPos;

      _ballCollider = GetComponentInChildren<CircleCollider2D>();
      _ballCollider.enabled = false;
      StartCoroutine(CO_SetColliderAfter(_lifetime * 0.9f, true));
   }

   private IEnumerator CO_SetColliderAfter(float seconds, bool value) {
      yield return new WaitForSeconds(seconds);
      _ballCollider.enabled = value;
   }

   private void Update () {
      if (Util.isServer()) {
         double timeAlive = NetworkTime.time - _startTime;

         if (timeAlive > _lifetime && !_hasCollided) {
            processDestruction();
         } else if (NetworkTime.time - _lastVelocitySyncTime > 0.2) {
            _lastVelocitySyncTime = NetworkTime.time;
            projectileVelocity = _rigidbody.velocity;
         }

         if (_isLobbed) {
            updateHeight();
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
      if (!Util.isServer()) {
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

      _hasCollided = true;

      // The Server will handle applying damage
      if (Util.isServer()) {
         SeaEntity sourceEntity = SeaManager.self.getEntity(this._creatorNetId);

         // Ship process if the owner of the projectile and the collided entity has the same voyage, this prevents friendly fire
         if ((sourceEntity.voyageGroupId > 0 || hitEntity.voyageGroupId > 0) && (sourceEntity.voyageGroupId == hitEntity.voyageGroupId)) {
            return;
         }
         
         int damage = (int) (sourceEntity.damage * _damageMultiplier);
         hitEntity.currentHealth -= damage;

         // Apply the status effect
         StatusManager.self.create(Status.Type.Slow, 3f, hitEntity.netId);

         // Have the server tell the clients where the explosion occurred
         hitEntity.Rpc_ShowExplosion(sourceEntity.netId, transform.position, damage, Attack.Type.Cannon);

         // Registers Damage throughout the clients
         hitEntity.Rpc_NetworkProjectileDamage(_creatorNetId, Attack.Type.Cannon, transform.position);

         // Destroy the projectile
         processDestruction();
      }
   }

   private void processDestruction () {
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
         Instantiate(PrefabsManager.self.requestCannonSplashPrefab(_impactMagnitude), transform.position + new Vector3(0f, -.1f), Quaternion.identity);
         SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Splash_Cannon_1, this.transform.position);
      }

      // Detach the Trail Renderer so that it continues to show up a little while longer
      TrailRenderer trail = this.gameObject.GetComponentInChildren<TrailRenderer>();
      trail.autodestruct = true;
      trail.transform.SetParent(null);

      // For some reason, autodestruct doesn't always work resulting in infinite TrailRenderers being left in the scene, so we force it.
      Destroy(trail.gameObject, 3.0f);
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

   // The damage multiplier of this projectile considering the current travel force
   protected float _damageMultiplier = 1;

   // Determines the impact level of this projectile
   protected Attack.ImpactMagnitude _impactMagnitude = Attack.ImpactMagnitude.None;

   // The ability data cache
   protected ShipAbilityData _abilityData;

   // The time in seconds before the cannonball is destroyed
   protected float _lifetime;

   // The default lifetime if none is provided
   protected const float DEFAULT_LIFETIME = 1.25f;

   // A reference to the child object that contains the cannonball
   private CircleCollider2D _ballCollider;

   // If this cannonball should move with parabolic motion
   private bool _isLobbed = false;

   // How high this cannonball was lobbed
   private float _lobHeight = 0.0f;

   #endregion

}
