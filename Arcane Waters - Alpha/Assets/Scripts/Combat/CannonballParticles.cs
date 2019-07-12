using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CannonballParticles : MonoBehaviour {
   #region Public Variables

   #endregion

   private void Start () {
      // Note our starting position
      _lastPos = this.transform.position;
   }

   private void Update () {
      // If we haven't moved, don't do anything
      if (Vector2.Distance(this.transform.position, _lastPos) == 0f) {
         return;
      }

      // Check which direction we're moving
      Vector2 direction = (Vector2) this.transform.position - _lastPos;

      // Rotate according to the angle
      float angle = Util.angle(direction);
      this.transform.rotation = Quaternion.Euler(0f, 0f, 360f - angle);

      // Note our position for the next frame
      _lastPos = this.transform.position;
   }

   #region Private Variables

   // Our position last frame
   protected Vector2 _lastPos;
      
   #endregion
}
