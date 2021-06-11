﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TriggerDetector : MonoBehaviour {
   #region Public Variables

   // An event called whenever onTriggerEnter is called for this object, which other objects can subscribe to
   public System.Action<Collider2D> onTriggerEnter;

   // An event called whenever onTriggerExit is called for this object, which other objects can subscribe to
   public System.Action<Collider2D> onTriggerExit;

   #endregion

   private void OnTriggerEnter2D (Collider2D collision) {
      onTriggerEnter?.Invoke(collision);
   }

   private void OnTriggerExit2D (Collider2D collision) {
      onTriggerExit?.Invoke(collision);
   }

   #region Private Variables

   #endregion
}
