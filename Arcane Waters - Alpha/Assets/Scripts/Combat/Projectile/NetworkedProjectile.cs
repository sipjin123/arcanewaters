﻿using UnityEngine;
using Mirror;

public class NetworkedProjectile : MonoBehaviour {
   #region Public Variables

   // How long a projectile lives at most
   public float lifeTime = 1.25f;

   // How high the projectile should arch upwards
   public float archHeight = .10f;

   // How fast the projectile should move
   public float moveSpeed = 1.55f;

   // Our projectile sprite
   public GameObject projectileSpriteObj;

   // The sprites of the projectile
   public SpriteRenderer[] projectileSprites;

   // Our Rigid Body
   public Rigidbody2D body;

   // Our Circle Collider
   public CircleCollider2D circleCollider;

   // The attack type associated with this projectile
   public Attack.Type attackType;

   // The ability data cache
   public ShipAbilityData abilityData;

   // The behavior type on how this projectile will be destroyed
   public ProjectileEndType projectileEndType;

   // Our Start Point
   public Vector2 startPos;

   // Our End Point
   public Vector2 endPos;

   // Returns the instance id
   public int instanceId { get { return _instanceId; } }

   public enum ProjectileEndType {
      None = 0,
      Lifetime = 1,
      Collision = 2,
      EndPoint = 3,
   }

   #endregion

   public void init (uint netID, int instanceID, Attack.ImpactMagnitude impactType, int abilityId, Vector2 startPos) {
      this.startPos = startPos;
      transform.position = startPos;

      _creatorNetId = netID;
      _instanceId = instanceID;
      _impactMagnitude = impactType;

      ShipAbilityData newAbilityData = ShipAbilityManager.self.getAbility(abilityId);
      abilityData = newAbilityData;

      projectileEndType = ProjectileEndType.None;
      switch (abilityData.selectedAttackType) {
         case Attack.Type.Boulder:
            projectileEndType = ProjectileEndType.EndPoint;

            lifeTime = .75f;
            archHeight = .1f;
            moveSpeed = 1.55f;
            break;
         case Attack.Type.Venom:
            lifeTime = 1.25f;
            archHeight = .10f;
            moveSpeed = 1.55f;
            break;
         case Attack.Type.Cannon:
            lifeTime = NetworkedCannonBall.LIFETIME;
            archHeight = .10f;
            moveSpeed = NetworkedCannonBall.MOVE_SPEED;
            break;
      }
      attackType = abilityData.selectedAttackType;

      foreach (SpriteRenderer spriteRenderer in projectileSprites) {
         spriteRenderer.sprite = ImageManager.getSprite(abilityData.projectileSpritePath);
      }
   }

   protected virtual void Start () {
      _startTime = TimeManager.self.getSyncedTime();

      if (!Util.isBatch()) {
         // Play a sound effect
         AudioClipManager.AudioClipData audioClipData = AudioClipManager.self.getAudioClipData(abilityData.castSFXPath);
         if (audioClipData.audioPath.Length > 1) {
            AudioClip clip = audioClipData.audioClip;
            if (clip != null) {
               SoundManager.playClipOneShotAtPoint(clip, Camera.main.transform.position);
            }
         } else {
            SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Attack_Fire, this.transform.position);
         }
      }

      if (attackType == Attack.Type.Cannon) {
         // Create a cannon smoke effect at our creation point
         Vector2 offset = this.body.velocity.normalized * .1f;
         Instantiate(PrefabsManager.self.requestCannonSmokePrefab(_impactMagnitude), (Vector2) this.transform.position + offset, Quaternion.identity);

         // If it was our ship, shake the camera
         if (Global.player != null && _creatorNetId == Global.player.netId) {
            CameraManager.shakeCamera();
         }
      }
   }

   protected virtual void Update () {
      // Adjusts the height of the projectile sprite based in an arch
      float timeAlive = TimeManager.self.getSyncedTime() - _startTime;
      float lerpTime = 0;

      if (attackType == Attack.Type.Boulder) {
         // The boulder projectiles lerp is for target position
         lerpTime = (timeAlive / lifeTime);
      } else {
         // The other attack type uses lerp time for trajectory and lifespan
         lerpTime = 1f - (timeAlive / lifeTime);
      }

      float angleInDegrees = lerpTime * 180f;
      float ballHeight = Util.getSinOfAngle(angleInDegrees) * archHeight;

      Util.setLocalY(projectileSpriteObj.transform, ballHeight);

      if (projectileEndType == ProjectileEndType.EndPoint) {
         // Move from the start to the end point
         Util.setXY(this.transform, Vector2.Lerp(startPos, endPos, lerpTime));
      }

      if (timeAlive > lifeTime && !_hasCollided) {
         processDestruction();
      }
   }

   protected void processDestruction () {
      Destroy(this.gameObject);
   }

   public void callCollision (bool hitLand, Vector3 location) {
      switch (attackType) {
         case Attack.Type.Boulder:
            if (!hitLand) {
               Instantiate(PrefabsManager.self.requestCannonSplashPrefab(_impactMagnitude), this.transform.position + new Vector3(0f, -.1f), Quaternion.identity);
               SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Splash_Cannon_1, this.transform.position);
            }
            break;
         case Attack.Type.Venom:
            // Commands the server to process spawning of venom residue
            if (NetworkServer.active) {
               if (!hitLand) {
                  SeaEntity sourceEntity = SeaManager.self.getEntity(this._creatorNetId);
                  VenomResidue venomResidue = Instantiate(PrefabsManager.self.venomResiduePrefab, location, Quaternion.identity);
                  venomResidue.creatorNetId = _creatorNetId;
                  venomResidue.instanceId = _instanceId;
                  sourceEntity.Rpc_SpawnVenomResidue(_creatorNetId, _instanceId, circleCollider.transform.position);
               }
            }
            break;
      }

      // Plays SFX and VFX for land collision
      if (hitLand) {
         Instantiate(PrefabsManager.self.requestCannonSmokePrefab(_impactMagnitude), location, Quaternion.identity);
         SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Slash_Lightning, this.transform.position);
      }
   }

   public void setDirection (Direction direction, Vector3 endposNew) {
      projectileSpriteObj.transform.LookAt(endposNew);
      endPos = endposNew;
   }

   protected void OnTriggerStay2D (Collider2D other) {
      // Check if the other object is a Sea Entity
      SeaEntity hitEntity = other.transform.GetComponentInParent<SeaEntity>();

      // We only care about hitting other sea entities in our instance
      if (hitEntity == null || this._creatorNetId == hitEntity.netId || other.GetComponent<CombatCollider>() != null || hitEntity.instanceId != this._instanceId) {
         return;
      }

      SeaEntity sourceEntity = SeaManager.self.getEntity(this._creatorNetId);

      // The Server will handle applying damage
      if (NetworkServer.active) {
         int damage = (int) (sourceEntity.damage / 3f);
         hitEntity.currentHealth -= damage;

         switch (attackType) {
            case Attack.Type.Boulder:
               ShipAbilityData shipAbilityData = ShipAbilityManager.self.getAbility(Attack.Type.Mini_Boulder);

               // Spawn Mini Boulders upon Collision
               SeaManager.self.getEntity(_creatorNetId).fireAtSpot(transform.position, shipAbilityData.abilityId, 0, 0, transform.position);
               break;
            case Attack.Type.Venom:
               // Registers the poison action status to the achievementdata for recording
               AchievementManager.registerUserAchievement(hitEntity.userId, ActionType.Poisoned);

               // Spawn Damage Per Second Residue
               hitEntity.Rpc_AttachEffect(damage, Attack.Type.Venom);
               break;
         }
         // Registers Damage throughout the clients
         hitEntity.Rpc_NetworkProjectileDamage(_creatorNetId, attackType, circleCollider.transform.position);

         // Have the server tell the clients where the explosion occurred
         hitEntity.Rpc_ShowExplosion(sourceEntity.netId, hitEntity.transform.position, damage, attackType);
      }
      _hasCollided = true;

      processDestruction();
   }

   private void OnDestroy () {
      callCollision(Util.hasLandTile(this.transform.position), circleCollider.transform.position);

      // Don't need to handle any of these effects in Batch Mode
      if (Util.isBatch()) {
         return;
      }

      // Detach the Trail Renderer so that it continues to show up a little while longer
      TrailRenderer trail = this.gameObject.GetComponentInChildren<TrailRenderer>();
      trail.transform.parent = null;
      trail.autodestruct = true;
   }

   #region Private Variables

   // Our Start Time
   protected float _startTime;

   // Blocks update func if the projectile collided
   protected bool _hasCollided;

   // The source of this attack
   protected uint _creatorNetId;

   // The instance id for this projectile
   protected int _instanceId;

   // Determines the impact level of this projectile
   protected Attack.ImpactMagnitude _impactMagnitude = Attack.ImpactMagnitude.None;

   #endregion
}
