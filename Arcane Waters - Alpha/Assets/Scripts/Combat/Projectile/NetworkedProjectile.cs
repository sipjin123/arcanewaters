using UnityEngine;
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

   // Whether the projectile travels in an arc
   public bool usesArc;

   // Returns the instance id
   public int instanceId { get { return _instanceId; } }

   public enum ProjectileEndType {
      None = 0,
      Lifetime = 1,
      Collision = 2,
      EndPoint = 3,
   }

   #endregion

   protected virtual void Start () {
      _startTime = NetworkTime.time;

      if (!Util.isBatch()) {
         // Play a sound effect
         AudioClipManager.AudioClipData audioClipData = AudioClipManager.self.getAudioClipData(abilityData.castSFXPath);
         if (audioClipData.audioPath.Length > 1) {
            AudioClip clip = audioClipData.audioClip;
            if (clip != null) {
               SoundManager.playClipAtPoint(clip, Camera.main.transform.position);
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

   public void init (uint netID, int instanceID, Attack.ImpactMagnitude impactType, int abilityId, Vector2 startPos, float lifetime = -1, bool usesArc = false, float damageMultiplier = 1) {
      this.startPos = startPos;
      this.usesArc = usesArc;
      transform.position = startPos;

      _damageMultiplier = damageMultiplier;
      _creatorNetId = netID;
      _instanceId = instanceID;
      _impactMagnitude = impactType;

      ShipAbilityData newAbilityData = ShipAbilityManager.self.getAbility(abilityId);
      abilityData = newAbilityData;

      projectileEndType = ProjectileEndType.None;

      switch (abilityData.selectedAttackType) {
         case Attack.Type.Boulder:
            projectileEndType = ProjectileEndType.EndPoint;

            lifeTime = lifetime > 0 ? lifetime : .75f;
            archHeight = .1f;
            moveSpeed = 1.55f;
            break;
         case Attack.Type.Venom:
            lifeTime = lifetime > 0 ? lifetime : 1.25f;
            archHeight = .10f;
            moveSpeed = 1.55f;
            break;
         case Attack.Type.Cannon:
            lifeTime = lifetime > 0 ? lifetime : NetworkedCannonBall.LIFETIME;
            archHeight = .10f;
            moveSpeed = NetworkedCannonBall.MOVE_SPEED;
            break;
      }
      attackType = abilityData.selectedAttackType;

      foreach (SpriteRenderer spriteRenderer in projectileSprites) {
         spriteRenderer.sprite = ImageManager.getSprite(abilityData.projectileSpritePath);
      }
   }

   protected virtual void Update () {
      // Adjusts the height of the projectile sprite based in an arch
      double timeAlive = NetworkTime.time - _startTime;
      float lerpTime = 0;

      if (attackType == Attack.Type.Boulder) {
         // The boulder projectiles lerp is for target position
         lerpTime = (float)(timeAlive / lifeTime);
      } else {
         // The other attack type uses lerp time for trajectory and lifespan
         lerpTime = (float)(1f - (timeAlive / lifeTime));
      }

      if (usesArc) {
         float angleInDegrees = lerpTime * 180f;
         float ballHeight = Util.getSinOfAngle(angleInDegrees) * archHeight;

         Util.setLocalY(projectileSpriteObj.transform, ballHeight);
      }

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

   protected virtual void OnTriggerStay2D (Collider2D other) {
      // Check if the other object is a Sea Entity
      SeaEntity hitEntity = other.transform.GetComponentInParent<SeaEntity>();

      // We only care about hitting other sea entities in our instance
      if (hitEntity == null || this._creatorNetId == hitEntity.netId || other.GetComponent<CombatCollider>() != null || hitEntity.instanceId != this._instanceId) {
         return;
      }

      SeaEntity sourceEntity = SeaManager.self.getEntity(this._creatorNetId);

      // The Server will handle applying damage
      if (NetworkServer.active) {
         float baseDamage = 0;
         if (sourceEntity is SeaMonsterEntity) {
            SeaMonsterEntity seaMonsterEntity = (SeaMonsterEntity) sourceEntity;
            SeaMonsterEntityData seaMonsterData = SeaMonsterManager.self.getMonster(seaMonsterEntity.monsterType);
            ShipAbilityData seaMonsterAbilityData = ShipAbilityManager.self.getAbility(seaMonsterEntity.seaMonsterData.skillIdList[0]);
            float damageModifier = seaMonsterAbilityData.damageModifier;
            baseDamage = SeaMonsterEntity.BASE_SEAMONSTER_DAMAGE + (SeaMonsterEntity.BASE_SEAMONSTER_DAMAGE * damageModifier);

            // TODO: Observe damage formula on live build
            D.editorLog("The network projectile damage is"+ " : " +baseDamage+ " : " +damageModifier+ " Modified: " + ((baseDamage / 3f) * _damageMultiplier), Color.cyan);
         }

         int totalDamage = (int) ((baseDamage / 3f) * _damageMultiplier);
         hitEntity.currentHealth -= totalDamage;

         switch (attackType) {
            case Attack.Type.Boulder:
               ShipAbilityData shipAbilityData = ShipAbilityManager.self.getAbility(Attack.Type.Mini_Boulder);
               if (shipAbilityData != null) {
                  // Spawn Mini Boulders upon Collision
                  SeaManager.self.getEntity(_creatorNetId).fireAtSpot(transform.position, shipAbilityData.abilityId, 0, 0, transform.position);
               } 
               break;
            case Attack.Type.Venom:
               // Registers the poison action status to the achievementdata for recording
               AchievementManager.registerUserAchievement(hitEntity, ActionType.Poisoned);

               // Spawn Damage Per Second Residue
               hitEntity.Rpc_AttachEffect(totalDamage, Attack.Type.Venom);
               break;
         }
         // Registers Damage throughout the clients
         hitEntity.Rpc_NetworkProjectileDamage(_creatorNetId, attackType, circleCollider.transform.position);

         // Have the server tell the clients where the explosion occurred
         hitEntity.Rpc_ShowExplosion(sourceEntity.netId, hitEntity.transform.position, totalDamage, attackType);
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

   #endregion
}
