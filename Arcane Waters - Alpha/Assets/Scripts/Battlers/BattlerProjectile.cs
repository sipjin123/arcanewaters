using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class BattlerProjectile : MonoBehaviour {
   #region Public Variables

   // If projectile has initialized
   public bool isInitialized;

   // The original position of the projectile
   public Vector2 startPosition; 

   // The target position of the projectile
   public Vector2 targetPosition;

   // The time before the projectile reaches target
   public float timeToReachTarget;

   // The life time of the object
   public float timeAlive;

   // Distance to the target before stopping
   public const float STOP_DISTANCE = .1f;

   #endregion

   public void setTrajectory (Vector2 startPos, Vector2 targetPosition, float targetTime) {
      isInitialized = true;
      this.startPosition = startPos;
      this.targetPosition = targetPosition;
      timeAlive = 0;
      timeToReachTarget = targetTime;
   }

   private void Update () {
      if (isInitialized) {
         timeAlive += Time.deltaTime / timeToReachTarget;
         transform.position = Vector2.Lerp(startPosition, targetPosition, timeAlive);
         if (Vector2.Distance(transform.position, targetPosition) < STOP_DISTANCE) {
            gameObject.SetActive(false);
            Destroy(this.gameObject);
         }
      }
   }
}
