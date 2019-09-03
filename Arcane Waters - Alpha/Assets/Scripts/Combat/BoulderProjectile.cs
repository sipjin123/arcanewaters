using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class BoulderProjectile : MonoBehaviour
{
   #region Public Variables

   // The Type of projectile this is
   public Attack.Type attackType = Attack.Type.Boulder;

   // The creator of this Attack Circle
   public SeaEntity creator;

   // The target of the attack, if any
   public GameObject targetObject;

   // Our Start Point
   public Vector2 startPos;

   // Our End Point
   public Vector2 endPos;

   // Our Start Time
   public float startTime;

   // Our End Time
   public float endTime;

   // Our boulder projectile sprite
   public GameObject boulderProjectile;

   #endregion

   public void setDirection (Direction direction) {
      transform.LookAt(endPos);
   }

   void Update () {
      // If a target object has been specified, update our end position
      if (targetObject != null) {
         endPos = targetObject.transform.position;
      }

      // Move from the start to the end point
      float totalLifetime = endTime - startTime;
      float lerpTime = (TimeManager.self.getSyncedTime() - startTime) / totalLifetime;
      Util.setXY(this.transform, Vector2.Lerp(startPos, endPos, lerpTime));
      
      // If we've been alive long enough, destroy ourself
      if (TimeManager.self.getSyncedTime() > this.endTime) {
         bool hitEnemy = false;

         // Create an explosion effect on the client if any targets were hit
         if (Global.player != null) {
            foreach (Collider2D hit in SeaEntity.getHitColliders(this.transform.position)) {
               if (hit != null) {
                  NetEntity entity = hit.GetComponent<NetEntity>();

                  // Make sure the target is in our same instance
                  if (entity != null && entity.instanceId == Global.player.instanceId) {
                     hitEnemy = true;

                     // If we hit a ship, show some flying particles
                     if (entity is ShipEntity && attackType != Attack.Type.Ice) {
                        ExplosionManager.createExplosion(entity.transform.position);
                        Instantiate(PrefabsManager.self.boulderCollisionPrefab, this.transform.position, Quaternion.identity);
                        SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Slash_Lightning, this.transform.position);
                     } 

                     break;
                  }

                  Boss boss = hit.GetComponent<Boss>();
                  if (boss != null) {
                     hitEnemy = true;
                  }
               }
            }
         }

         // If we didn't hit an enemy, then show an effect based on whether we hit land or water
         if (!hitEnemy) {
            // Was there a Land collider where the projectile hit?
            if (Util.hasLandTile(endPos)) {
               Instantiate(PrefabsManager.self.cannonSmokePrefab, this.transform.position, Quaternion.identity);
               SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Slash_Lightning, this.transform.position);
            } else {
               Instantiate(PrefabsManager.self.cannonSplashPrefab, this.transform.position + new Vector3(0f, -.1f), Quaternion.identity);
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
