using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class NetworkedTentacleProjectile : MonoBehaviour
{
   #region Public Variables

   // Our tentacle sprite
   public GameObject tentacleProjectile;

   // Our Rigid Body
   public Rigidbody2D body;

   // Our Circle Collider
   public CircleCollider2D circleCollider;

   // How long a tentacle projectile lives at most
   public static float LIFETIME = .5f;//1.25f;

   // How high the tentacle projectile should arch upwards
   public static float ARCH_HEIGHT = .10f;

   // How fast the tentacle projectile should move
   public static float MOVE_SPEED = 1.55f;

   // Our End Point
   public Vector2 endPos;

   // Our Start Point
   public Vector2 startPos;

   #endregion

   private void Start () {
      _startTime = TimeManager.self.getSyncedTime();

      startPos = transform.position;

      // Play a sound effect
      SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Attack_Fire, this.transform.position);
   }

   public void setDirection (Direction direction, Vector3 endposNew) {
      tentacleProjectile.transform.LookAt(endposNew);
      endPos = endposNew;
   }

   private void Update () {
      // Adjusts the height of the tentacle projectile sprite based in an arch
      float timeAlive = TimeManager.self.getSyncedTime() - _startTime;
      //float lerpTime = 1f - (timeAlive / LIFETIME);
      float lerpTime = (timeAlive / LIFETIME);
      float angleInDegrees = lerpTime * 180f;
      float ballHeight = Util.getSinOfAngle(angleInDegrees) * ARCH_HEIGHT;

      // Move from the start to the end point
      Util.setXY(this.transform, Vector2.Lerp(startPos, endPos, lerpTime));

      Util.setLocalY(tentacleProjectile.transform, ballHeight);

      if (timeAlive > LIFETIME && !_hasCollided) {
         processDestruction();
      }
   }

   private void OnTriggerStay2D (Collider2D other) {
      // Check if the other object is a Sea Entity
      SeaEntity hitEntity = other.transform.GetComponentInParent<SeaEntity>();

      // We only care about hitting other sea entities in our instance
      if (hitEntity == null || _creatorUserId == hitEntity.userId || other.GetComponent<CombatCollider>() != null || hitEntity.instanceId != _instanceId) {
         return;
      }

      SeaEntity sourceEntity = SeaManager.self.getEntity(this._creatorUserId);

      // The Server will handle applying damage
      if (NetworkServer.active) {
         int damage = (int) (sourceEntity.damage / 3f);
         hitEntity.currentHealth -= damage;
         hitEntity.noteAttacker(sourceEntity);

         // Apply the status effect
         StatusManager.self.create(Status.Type.Slow, 3f, hitEntity.userId);

         // Have the server tell the clients where the explosion occurred
         hitEntity.Rpc_ShowExplosion(hitEntity.transform.position, damage, Attack.Type.Tentacle_Range);

         ExplosionManager.createRockExplosion(circleCollider.transform.position);

         SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Boulder, this.transform.position);
      }
      _hasCollided = true;

      processDestruction();
   }

   private void processDestruction () {
      Destroy(this.gameObject);
   }

   public void callCollision (bool hitLand, Vector3 location) {
      if (hitLand) {
         Instantiate(PrefabsManager.self.requestCannonSmokePrefab(_impactMagnitude), location, Quaternion.identity);
         SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Boulder, this.transform.position);
      } else {
         Instantiate(PrefabsManager.self.requestCannonSplashPrefab(_impactMagnitude), this.transform.position + new Vector3(0f, -.1f), Quaternion.identity);
         SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Splash_Cannon_1, this.transform.position);
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
