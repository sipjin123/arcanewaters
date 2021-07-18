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
      _effector = GetComponent<AreaEffector2D>();
   }

   private void OnEnable () {
      _activeEffectors.Add(this);
      setEffector(effectorsEnabled);
   }

   private void OnDisable () {
      _activeEffectors.Remove(this);
   }

   public void setEffector (bool enabled) {
      _effector.enabled = enabled;
   }

   public static void setEffectors (bool enabled) {
      if (enabled == effectorsEnabled) {
         return;
      }

      effectorsEnabled = enabled;
      foreach (StairEffector effector in _activeEffectors) {
         if (effector) {
            effector.setEffector(enabled);
         }
      }
   }

   #region Private Variables

   // A reference to the AreaEffector slowing the player's movement for these stairs
   private AreaEffector2D _effector;

   // A list of all active stair effectors
   private static List<StairEffector> _activeEffectors = new List<StairEffector>();

   #endregion
}
