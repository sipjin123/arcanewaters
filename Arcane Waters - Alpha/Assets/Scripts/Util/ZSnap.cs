using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ZSnap : MonoBehaviour {
   #region Public Variables

   // The amount of vertical offset to apply for this object
   public float offsetZ;

   // Whether or not this object should ZSnap inside of Update()
   public bool isActive = false;

   // A game object we can position at our desired sort point
   public GameObject sortPoint;

   #endregion

   void Start () {
      // Disable this component for better performance when we're running in batchmode
      if (Application.isBatchMode) {
         this.enabled = false;
         return;
      }

      if (sortPoint != null) {
         offsetZ = sortPoint.transform.localPosition.y;
      }

      // Avoid positions that are between pixel values
      roundoutPosition();

      // Check if an explicit offset was provided
      /*if (offsetZ == 0) {
         // Store the Y offset of our circle collider
         if (GetComponent<CircleCollider2D>() != null) {
            _colliderY = GetComponent<CircleCollider2D>().offset.y;
         } else if (GetComponent<BoxCollider2D>() != null) {
            _colliderY = GetComponent<BoxCollider2D>().offset.y;
         } else if (GetComponent<PolygonCollider2D>() != null) {
            _colliderY = GetComponent<PolygonCollider2D>().offset.y;
         }
      }*/

      snapZ();
   }

   /// <summary>
   /// Makes the position of the gameobject have less numbers after point,
   /// to avoid positions that are in-between pixel values
   /// </summary>
   public void roundoutPosition () {
      this.transform.position = new Vector2(
         Util.Truncate(this.transform.position.x), 
         Util.Truncate(this.transform.position.y));
   }

   void FixedUpdate () {
      if (_firstFrame) {
         snapZ();
         _firstFrame = false;
      }

      if (isActive) {
         snapZ();
      }
   }

   public void snapZ () {
      // Initialize our new Z position to a truncated version of the Y position
      float newZ = transform.position.y;
      newZ = Util.TruncateTo100ths(newZ);

      // Adjust our Z position based on our collider's Y position
      transform.position = new Vector3(
          transform.position.x,
          transform.position.y,
          (newZ + _colliderY + offsetZ) / 100f
      );
   }

   #region Private Variables

   // Store the Y offset of our circle collider
   protected float _colliderY;

   // Whether this is our first Fixed Update frame
   protected bool _firstFrame = true;

   #endregion
}
