using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AttachedLightning : MonoBehaviour {
   #region Public Variables

   // The chain points to snap on to
   public Transform endPoint, startPoint;

   // If this has been activated
   public bool hasActivated = false;

   #endregion

   private void Update () {
      if (!hasActivated) {
         return;
      }

      if (startPoint == null || endPoint == null) {
         gameObject.SetActive(false);
         hasActivated = false;
         return;
      }

      // Chain links have a distance cap
      float distanceBetweenPoints = Vector2.Distance(startPoint.position, endPoint.position);
      if (distanceBetweenPoints > 1.5f) {
         gameObject.SetActive(false);
         hasActivated = false;
      }
   }

   #region Private Variables

   #endregion
}
