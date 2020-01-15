using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class NetworkedVenomProjectile : MonoBehaviour
{
   #region Public Variables

   // Our venom sprite
   public GameObject venomProjectile;

   // Our Rigid Body
   public Rigidbody2D body;

   // Our Circle Collider
   public CircleCollider2D circleCollider;

   // How long a venom projectile lives at most
   public static float LIFETIME = 1.25f;

   // How high the venom projectile should arch upwards
   public static float ARCH_HEIGHT = .10f;

   // How fast the venom projectile should move
   public static float MOVE_SPEED = 1.55f;

   // Our End Point
   public Vector2 endPos;

   #endregion

   public void init (int userID, int instanceID, Attack.ImpactMagnitude impactType) {
      _creatorUserId = userID;
      _instanceId = instanceID;
      _impactMagnitude = impactType;
   }

   private void Start () {
      _startTime = TimeManager.self.getSyncedTime();

      // Play a sound effect
      SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Attack_Fire, this.transform.position);
   }

   public void setDirection (Direction direction, Vector3 endposNew) {
      venomProjectile.transform.LookAt(endposNew);
      endPos = endposNew;
   }

   private void Update () {
      // Adjusts the height of the venom projectile sprite based in an arch
      float timeAlive = TimeManager.self.getSyncedTime() - _startTime;
      float lerpTime = 1f - (timeAlive / LIFETIME);
      float angleInDegrees = lerpTime * 180f;
      float ballHeight = Util.getSinOfAngle(angleInDegrees) * ARCH_HEIGHT;

      Util.setLocalY(venomProjectile.transform, ballHeight);

      if (timeAlive > LIFETIME && !_hasCollided) {
         processDestruction();
      }
   }

   private void OnTriggerStay2D (Collider2D other) {
      // Check if the other object is a Sea Entity
      SeaEntity hitEntity = other.transform.GetComponentInParent<SeaEntity>();

      // We only care about hitting other sea entities in our instance
      if (hitEntity == null || this._creatorUserId == hitEntity.userId || other.GetComponent<CombatCollider>() != null || hitEntity.instanceId != this._instanceId) {
         return;
      }

      SeaEntity sourceEntity = SeaManager.self.getEntity(this._creatorUserId);

      // The Server will handle applying damage
      if (NetworkServer.active) {
         int damage = (int) (sourceEntity.damage / 3f);
         hitEntity.currentHealth -= damage;

         // Registers the poison action status to the achievementdata for recording
         AchievementManager.registerUserAchievement(hitEntity.userId, ActionType.Poisoned);

         // Spawn Damage Per Second Residue
         hitEntity.Rpc_AttachEffect(damage, Attack.Type.Venom);

         // Registers Damage throughout the clients
         hitEntity.Rpc_NetworkProjectileDamage(_creatorUserId, Attack.Type.Venom, circleCollider.transform.position);

         // Have the server tell the clients where the explosion occurred
         hitEntity.Rpc_ShowExplosion(hitEntity.transform.position, damage, Attack.Type.Venom);
      }
      _hasCollided = true;

      processDestruction();
   }

   private void processDestruction () {
      Destroy(this.gameObject);
   }

   public void callCollision (bool hitLand, Vector3 location) {
      // Commands the server to process spawning of venom residue
      if (NetworkServer.active) {
         if (!hitLand) {
            SeaEntity sourceEntity = SeaManager.self.getEntity(this._creatorUserId);
            sourceEntity.Rpc_SpawnVenomResidue(_creatorUserId, circleCollider.transform.position);
         } 
      }

      // Plays SFX and VFX for land collision
      if (hitLand) {
         Instantiate(PrefabsManager.self.requestCannonSmokePrefab(_impactMagnitude), location, Quaternion.identity);
         SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Slash_Lightning, this.transform.position);
      } 
   }

   private void OnDestroy () {
      callCollision(Util.hasLandTile(this.transform.position), circleCollider.transform.position);

      // Don't need to handle any of these effects in Batch Mode
      if (Application.isBatchMode) {
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

   // Determines the impact level of this projectile
   protected Attack.ImpactMagnitude _impactMagnitude = Attack.ImpactMagnitude.None;

   // The source of this attack
   private int _creatorUserId;

   // The instance id for this venom projectile
   private int _instanceId;

   #endregion
}
