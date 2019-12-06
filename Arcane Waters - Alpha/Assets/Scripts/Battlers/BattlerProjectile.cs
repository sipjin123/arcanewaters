using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class BattlerProjectile : MonoBehaviour {
   #region Public Variables

   // If projectile has initialized
   public bool init;

   // The original position of the projectile
   public Vector2 originPosition; 

   // The target position of the projectile
   public Vector2 targetPosition;

   // The time before the projectile reaches target
   public float timeToReachTarget;

   // The timer of the object
   public float time;

   // The magnitude target before the projectile is destroyed
   public const float STOP_MAGNITUDE = .95f;

   #endregion

   public void setTrajectory (Vector2 originPosition, Vector2 targetPosition, float targetTime) {
      init = true;
      this.originPosition = originPosition;
      this.targetPosition = targetPosition;
      time = 0;
      timeToReachTarget = targetTime;
   }

   private void Update () {
      if (init) {
         time += Time.deltaTime / timeToReachTarget;
         transform.position = Vector2.Lerp(originPosition, targetPosition, time);
         if (time >= STOP_MAGNITUDE) {
            gameObject.SetActive(false);
            Destroy(this.gameObject);
         }
      }
   }
}
