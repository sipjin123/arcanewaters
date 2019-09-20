using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class NetworkedVenomProjectile : MonoBehaviour
{
   #region Public Variables

   // The source of this attack
   public int creatorUserId;

   // The instance id for this venom projectile
   public int instanceId;

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
      if (hitEntity == null || this.creatorUserId == hitEntity.userId || other.GetComponent<CombatCollider>() != null || hitEntity.instanceId != this.instanceId) {
         return;
      }

      SeaEntity sourceEntity = SeaManager.self.getEntity(this.creatorUserId);

      // The Server will handle applying damage
      if (NetworkServer.active) {
         int damage = (int) (sourceEntity.damage / 3f);
         hitEntity.currentHealth -= damage;
         hitEntity.noteAttacker(sourceEntity);

         // Apply the status effect
         StatusManager.self.create(Status.Type.Slow, 3f, hitEntity.userId);

         // Have the server tell the clients where the explosion occurred
         hitEntity.Rpc_AttachEffect(damage, Attack.Type.Venom);
         hitEntity.Rpc_ShowExplosion(hitEntity.transform.position, damage, Attack.Type.Venom);

         ExplosionManager.createSlimeExplosion(circleCollider.transform.position);

         SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Slash_Lightning, this.transform.position);
      }

      _hasCollided = true;

      processDestruction();
   }

   private void processDestruction () {
      callCollision(Util.hasLandTile(this.transform.position), circleCollider.transform.position);
      Destroy(this.gameObject);
   }

   public void callCollision (bool hitLand, Vector3 location) {
      if (hitLand) {
         Instantiate(PrefabsManager.self.cannonSmokePrefab, location, Quaternion.identity);
         SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Slash_Lightning, this.transform.position);
      } else {
         location.z = 0;
         GameObject venomResidue = Instantiate(PrefabsManager.self.venomResiduePrefab, location, Quaternion.identity);
         venomResidue.GetComponent<VenomResidue>().creatorUserId = this.creatorUserId;
         ExplosionManager.createSlimeExplosion(circleCollider.transform.position);

         SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Coralbow_Attack, this.transform.position);
      }
   }

   private void OnDestroy () {
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

   #endregion
}
