using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class GenericEffector : MonoBehaviour {
   #region Public Variables

   public enum Type
   {
      None = 0,
      Current = 1,
      Stair = 2,
      SeaAbility = 3,
      Ledge = 4,
   }

   // What category of effector this is
   public GenericEffector.Type effectorType = GenericEffector.Type.None;

   // Whether all effectors are currently enabled or not
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

   public static void setEffectorCollisions (Collider2D otherCollider, bool shouldCollide, GenericEffector.Type effectorType) { 
      foreach (GenericEffector effector in _activeEffectors) {
         if (effectorType == effector.effectorType) {
            Physics2D.IgnoreCollision(effector._collider, otherCollider, !shouldCollide);
         }
      }
   }

   #region Private Variables

   // A reference to the collider for this effector
   private Collider2D _collider;

   // A list of all active effectors
   private static List<GenericEffector> _activeEffectors = new List<GenericEffector>();

   #endregion
}
