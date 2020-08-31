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

   // The Speed of the projectile
   public float projectileSpeed;

   // Distance to the target before stopping
   public const float STOP_DISTANCE = .1f;

   // Reference to the sprite
   public SpriteRenderer projectileRenderer;

   // The shadow object
   public GameObject shadowObj;

   #endregion

   public void setTrajectory (Vector2 startPos, Vector2 targetPosition, float projectileSpeed) {
      isInitialized = true;
      this.startPosition = startPos;
      this.targetPosition = targetPosition;
      this.projectileSpeed = projectileSpeed;
   }

   private void Update () {
      if (isInitialized) {
         transform.position = Vector2.MoveTowards(transform.position, targetPosition, projectileSpeed * Time.deltaTime);
         if (Vector2.Distance(transform.position, targetPosition) < STOP_DISTANCE) {
            isInitialized = false;
            StartCoroutine(CO_StopProjectile());
         }
      }
   }

   IEnumerator CO_StopProjectile () {
      EffectManager.show(Effect.Type.Hit, transform.position, .15f);
      yield return new WaitForSeconds(.1f);

      gameObject.SetActive(false);
      Destroy(this.gameObject);
   }
}
