using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class NetworkedCannonBall : NetworkedProjectile
{
   #region Public Variables

   // Static values for this specific projectile
   public static float MOVE_SPEED = 2.0f;
   public static float LIFETIME = 1.0f;

   #endregion

   protected override void Start () {
      base.Start();
   }

   protected override void Update () {
      base.Update();
   }

   protected override void OnTriggerStay2D (Collider2D other) {
      // Cannonball handles collision OnTriggerEnter, so we disable OnTriggerStay from NetworkedProjectile
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

      // The Server will handle applying damage
      if (NetworkServer.active) {
         SeaEntity sourceEntity = SeaManager.self.getEntity(this._creatorNetId);
         int initialDamage = (int) (sourceEntity.damage * _distanceDamageMultiplier);
         int finalDamage = hitEntity.applyDamage(initialDamage, sourceEntity.netId, attackType);

         // Apply the status effect
         StatusManager.self.create(Status.Type.Slowed, 1.0f, 3f, hitEntity.netId);

         // Have the server tell the clients where the explosion occurred
         hitEntity.Rpc_ShowExplosion(sourceEntity.netId, circleCollider.transform.position, finalDamage, attackType, false);

         // Registers Damage throughout the clients
         hitEntity.Rpc_NetworkProjectileDamage(_creatorNetId, attackType, circleCollider.transform.position);
      }

      _hasCollided = true;

      processDestruction();
   }

   private void OnDestroy () {
      // Don't need to handle any of these effects in Batch Mode
      if (Util.isBatch()) {
         return;
      }

      // If we didn't hit an enemy and the cannon ball is hitting the water, either show a splash or some smoke
      if (projectileSpriteObj.transform.localPosition.y <= .02f) {
         // Was there a Land collider where the cannonball hit?
         if (Util.hasLandTile(this.transform.position)) {
            Instantiate(PrefabsManager.self.requestCannonSmokePrefab(_impactMagnitude), circleCollider.transform.position, Quaternion.identity);
            //SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Slash_Lightning, this.transform.position);
         } else {
            Instantiate(PrefabsManager.self.requestCannonSplashPrefab(_impactMagnitude), circleCollider.transform.position + new Vector3(0f, -.1f), Quaternion.identity);

            // FMOD sfx for water
            //SoundEffectManager.self.playCannonballImpact(SoundEffectManager.Cannonball.Water_Impact, this.transform.position);
            //SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Splash_Cannon_1, this.transform.position);
         }
      }

      // Detach the Trail Renderer so that it continues to show up a little while longer
      TrailRenderer trail = this.gameObject.GetComponentInChildren<TrailRenderer>();
      trail.transform.parent = null;
      trail.autodestruct = true;
   }

   #region Private Variables

   #endregion
}
