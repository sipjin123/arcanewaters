using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SmoothFollow : ClientMonoBehaviour {
   #region Public Variables

   // The object we're following
   public GameObject followTarget;

   // How long it should take us to reach the target
   public float smoothTime = .1f;

   // Whether we want this object to go away when the follow target does
   public bool destroyIfNoTarget = false;

   #endregion

   void Update () {
      // If we don't have a follow target, we can't do anything yet
      if (followTarget == null) {
         if (destroyIfNoTarget) {
            Destroy(this.gameObject);
         }

         return;
      }

      // If we're too far away, just snap
      if (Vector2.Distance(this.transform.position, followTarget.transform.position) > 3f) {
         Util.setXY(this.transform, followTarget.transform.position);
         return;
      }

      // Smoothly move towards the position of the object that we're following
      Vector3 newPos = Vector3.SmoothDamp(this.transform.position, followTarget.transform.position, ref _followVelocity, smoothTime);

      // Apply the new position, keeping our Z position unchanged
      this.transform.position = new Vector3(
         newPos.x, newPos.y, this.transform.position.z
      );
   }

   #region Private Variables

   // The velocity at which we're moving
   protected Vector3 _followVelocity;

   #endregion
}
