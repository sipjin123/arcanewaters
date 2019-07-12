using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class NetworkedCannonBall : MonoBehaviour {
   #region Public Variables

   // The source of this attack
   public int creatorUserId;

   // The instance id for this cannon ball
   public int instanceId;

   // Our cannon ball sprite
   public GameObject cannonBall;

   // Our Rigid Body
   public Rigidbody2D body;

   // Our Circle Collider
   public CircleCollider2D circleCollider;

   // How long a cannon ball lives at most
   public static float LIFETIME = 1.25f;

   // How high the cannon ball should arch upwards
   public static float ARCH_HEIGHT = .10f;

   // How fast the cannon ball should move
   public static float MOVE_SPEED = 2f;

   #endregion

   private void Start () {
      _startTime = TimeManager.self.getSyncedTime();

      // Create a cannon smoke effect at our creation point
      Vector2 offset = this.body.velocity.normalized * .1f;
      Instantiate(PrefabsManager.self.cannonSmokePrefab, (Vector2)this.transform.position + offset, Quaternion.identity);

      // Play a sound effect
      SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Ship_Cannon_2, this.transform.position);

      // If it was our ship, shake the camera
      if (Global.player != null && creatorUserId == Global.player.userId) {
         CameraManager.shakeCamera();
      }
   }

   private void Update () {
      // Adjusts the height of the cannon ball sprite based in an arch
      float timeAlive = TimeManager.self.getSyncedTime() - _startTime;
      float lerpTime = 1f - (timeAlive / LIFETIME);
      float angleInDegrees = lerpTime * 180f;
      float ballHeight = Util.getSinOfAngle(angleInDegrees) * ARCH_HEIGHT;
      Util.setLocalY(cannonBall.transform, ballHeight);
   }

   private void OnTriggerEnter2D (Collider2D other) {
      // Check if the other object is a Sea Entity
      SeaEntity hitEntity = other.transform.GetComponentInParent<SeaEntity>();

      // We only care about hitting other sea entities in our instance
      if (hitEntity == null || hitEntity.instanceId != this.instanceId || hitEntity.userId == this.creatorUserId) {
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
         hitEntity.Rpc_ShowExplosion(circleCollider.transform.position, damage, Attack.Type.Cannon);
      }

      // Get rid of the cannon ball
      Destroy(this.gameObject);
   }

   private void OnDestroy () {
      // Don't need to handle any of these effects in Batch Mode
      if (Application.isBatchMode) {
         return;
      }

      // If we didn't hit an enemy and the cannon ball is hitting the water, either show a splash or some smoke
      if (cannonBall.transform.localPosition.y <= .02f) {
         // Was there a Land collider where the cannonball hit?
         if (Util.hasLandTile(this.transform.position)) {
            Instantiate(PrefabsManager.self.cannonSmokePrefab, circleCollider.transform.position, Quaternion.identity);
            SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Slash_Lightning, this.transform.position);
         } else {
            Instantiate(PrefabsManager.self.cannonSplashPrefab, circleCollider.transform.position + new Vector3(0f, -.1f), Quaternion.identity);
            SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Splash_Cannon_1, this.transform.position);
         }
      }

      // Detach the Trail Renderer so that it continues to show up a little while longer
      TrailRenderer trail = this.gameObject.GetComponentInChildren<TrailRenderer>();
      trail.transform.parent = null;
      trail.autodestruct = true;
   }

   #region Private Variables

   // Our Start Time
   protected float _startTime;

   #endregion
}
