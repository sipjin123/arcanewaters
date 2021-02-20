using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TargetFollow : ClientMonoBehaviour {
   #region Public Variables

   // The object we're following
   public GameObject followTarget;

   // How long it should take us to reach the target
   public float smoothTime = .1f;

   // Whether we want this object to go away when the follow target does
   public bool destroyIfNoTarget = false;

   // A position offset in the Z axis
   public float zOffset = 0;

   // Whether the target should be followed smoothly or not
   public bool followSmoothly = true;

   #endregion

   private void OnEnable () {
      if (followTarget != null) {
         snapToTargetPosition();
      }   
   }

   void Update () {
      // If we don't have a follow target, we can't do anything yet
      if (followTarget == null) {
         if (destroyIfNoTarget) {
            Destroy(this.gameObject);
         }

         return;
      }

      if (followSmoothly) {
         // If we're too far away, just snap
         if (Vector2.Distance(this.transform.position, followTarget.transform.position) > MAX_DISTANCE_FROM_TARGET) {
            snapToTargetPosition();
            return;
         }

         // Smoothly move towards the position of the object that we're following
         Vector3 newPos = Vector3.SmoothDamp(this.transform.position, followTarget.transform.position, ref _followVelocity, smoothTime);
         newPos.z = followTarget.transform.position.z + zOffset;

         transform.position = newPos;
      } else {
         snapToTargetPosition();
      }
   }

   protected void snapToTargetPosition () {
      Vector3 newPos = followTarget.transform.position;
      newPos.z += zOffset;

      transform.position = newPos;
   }

   #region Private Variables

   // The velocity at which we're moving
   protected Vector3 _followVelocity;

   // The maximum distance from the target when following smoothly
   protected const float MAX_DISTANCE_FROM_TARGET = 0.2f;
   #endregion
}
