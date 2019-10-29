using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AttackCircle : MonoBehaviour {
   #region Public Variables

   // The creator of this Attack Circle
   public SeaEntity creator;

   // Our Start Point
   public Vector2 startPos;

   // Our End Point
   public Vector2 endPos;

   // Our Start Time
   public float startTime;

   // Our End Time
   public float endTime;

   // Whether the attack circle has been placed yet
   public bool hasBeenPlaced = false;

   // The circle collider
   public CircleCollider2D circleCollider;

   #endregion

   void Update () {
      // If we've been alive long enough, destroy ourself
      if (TimeManager.self.getSyncedTime() > this.endTime) {
         Destroy(this.gameObject);
         return;
      }

      // If this isn't our attack circle, we don't get to control it
      if (hasBeenPlaced || Util.getMyUserId() != creator.userId) {
         return;
      }

      // Set the attack circle to wherever the mouse is
      this.transform.position = Util.getMousePos();

      //// If it's inside one of the attack boxes, we're done
      //if (creator.leftAttackBox.OverlapPoint(this.transform.position) || creator.rightAttackBox.OverlapPoint(this.transform.position)) {
      //   return;
      //}

      //// Calculate the closest point on the left and right attack boxes
      //ColliderDistance2D leftDistance = circleCollider.Distance(creator.leftAttackBox);
      //ColliderDistance2D rightDistance = circleCollider.Distance(creator.rightAttackBox);
      //Vector2 closestLeft = leftDistance.pointB;
      //Vector2 closestRight = rightDistance.pointB;

      //// Set the mouse position to which point is closest on the attack boxes
      //this.transform.position = (rightDistance.distance < leftDistance.distance) ? rightDistance.pointB : leftDistance.pointB;
   }

   #region Private Variables

   #endregion
}
