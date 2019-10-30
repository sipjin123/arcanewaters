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

   // The animator of the circle
   public Animator animator;

   #endregion

   public void Start () {
      // Set the speed at which the circle shrinks
      animator.SetFloat("speed", 1/(endTime - startTime));
   }

   void Update () {
      // If we've been alive long enough, destroy ourself
      if (TimeManager.self.getSyncedTime() > this.endTime) {
         Destroy(this.gameObject);
         return;
      }
   }

   #region Private Variables

   #endregion
}
