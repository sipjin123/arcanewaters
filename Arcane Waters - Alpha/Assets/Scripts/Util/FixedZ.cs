using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FixedZ : MonoBehaviour {
   #region Public Variables

   // The Z position we want this object to always have
   public float newZ;

   #endregion

   void Start () {
      // Disable this component for better performance when we're running in batchmode
      if (Util.isBatch()) {
         this.enabled = false;
         return;
      }
   }

   void FixedUpdate () {
      if (this.transform.position.z != newZ) {
         Util.setZ(this.transform, newZ);
      }
   }

   #region Private Variables

   #endregion
}
