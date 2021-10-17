using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class StairEffector : MonoBehaviour {
   #region Public Variables

   // Whether all stair effectors are currently enabled or not
   public static bool effectorsEnabled = true;

   #endregion

   private void Awake () {
      _collider = GetComponent<Collider2D>();
   }

   private void OnEnable () {
      _activeEffectors.Add(this);
   }

   private void OnDisable () {
      _activeEffectors.Remove(this);
   }

   public static void setEffectorCollisions (Collider2D otherCollider, bool shouldCollide) {
      foreach (StairEffector effector in _activeEffectors) {
         Physics2D.IgnoreCollision(effector._collider, otherCollider, !shouldCollide);
      }
      effectorsEnabled = shouldCollide;
   }

   #region Private Variables

   // A reference to the collider for this effector
   private Collider2D _collider;

   // A list of all active stair effectors
   private static List<StairEffector> _activeEffectors = new List<StairEffector>();

   #endregion
}
