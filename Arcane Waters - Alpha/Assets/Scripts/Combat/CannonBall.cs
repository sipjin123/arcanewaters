using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CannonBall : GenericSeaProjectile {
   #region Public Variables

   // The Type of cannon ball this is
   public Attack.Type attackType = Attack.Type.Cannon;

   // Our cannon ball sprite
   public GameObject cannonBall;

   // Projectile Sprite
   public SpriteRenderer staticSprite;

   // Animated sprite
   public SpriteRenderer animatedSprite;

   #endregion

   void Update () {
      // If a target object has been specified, update our end position
      if (_targetObject != null) {
         _endPos = _targetObject.transform.position;
      }

      // Move from the start to the end point
      float totalLifetime = _endTime - _startTime;
      float lerpTime = (TimeManager.self.getSyncedTime() - _startTime) / totalLifetime;
      Util.setXY(this.transform, Vector2.Lerp(_startPos, _endPos, lerpTime));

      // Adjusts the height of the cannon ball sprite based in an arch
      Util.setLocalY(cannonBall.transform, AttackManager.getArcHeight(_startPos, _endPos, lerpTime, true));

      // If we've been alive long enough, destroy ourself
      if (TimeManager.self.getSyncedTime() > this._endTime) {
         bool hitEnemy = false;

         // Create an explosion effect on the client if any targets were hit
         if (Global.player != null) {
            foreach (Collider2D hit in SeaEntity.getHitColliders(this.transform.position)) {
               if (hit != null) {
                  NetEntity entity = hit.GetComponent<NetEntity>();

                  // Make sure we don't hit the creator of the cannon ball
                  if (entity == _creator) {
                     continue;
                  }

                  // Make sure the target is in our same instance
                  if (entity != null && entity.instanceId == Global.player.instanceId) {
                     hitEnemy = true;

                     // If we hit a ship, show some flying particles
                     if (entity is ShipEntity && attackType != Attack.Type.Ice) {
                        ExplosionManager.createExplosion(entity.transform.position);
                     }

                     break;
                  }

                  Boss boss = hit.GetComponent<Boss>();
                  if (boss != null) {
                     hitEnemy = true;
                  }

                  // Did we hit a treasure site gate
                  TreasureSiteGate gate = hit.GetComponent<TreasureSiteGate>();

                  if (gate != null && gate.colliderContainsPoint(this.transform.position) && this._creator == Global.player) {
                     gate.wasHit();
                  }
               }
            }
         }

         // If we didn't hit an enemy, then show an effect based on whether we hit land or water
         if (!hitEnemy) {
            // Was there a Land collider where the cannonball hit?
            if (Util.hasLandTile(_endPos)) {
               Instantiate(PrefabsManager.self.requestCannonSmokePrefab(_impactMagnitude), this.transform.position, Quaternion.identity);
               SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Slash_Lightning, this.transform.position);
            } else {
               Instantiate(PrefabsManager.self.requestCannonSplashPrefab(_impactMagnitude), this.transform.position + new Vector3(0f, -.1f), Quaternion.identity);
               SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Splash_Cannon_1, this.transform.position);
            }
         }

         // Detach the Trail Renderer so that it contains to show up a little while longer
         TrailRenderer trail = this.gameObject.GetComponentInChildren<TrailRenderer>();
         trail.transform.parent = null;
         trail.autodestruct = true;

         // Now destroy
         Destroy(this.gameObject);
         return;
      }
   }

   #region Private Variables

   #endregion
}
