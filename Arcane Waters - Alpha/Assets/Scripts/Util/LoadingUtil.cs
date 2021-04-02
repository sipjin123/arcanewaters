using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class LoadingUtil : GenericGameManager {
   #region Public Variables

   // Singleton instance
   public static LoadingUtil self;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   public static void executeAfterFade (Action action) {
      self.StartCoroutine(self.CO_ExecuteAfterFade(action));
   }

   private IEnumerator CO_ExecuteAfterFade (Action action) {
      // Wait for screen to fade out before starting to load
      while (Global.isScreenTransitioning) {
         yield return null;
      }

      action?.Invoke();
   }

   #region Private Variables

   #endregion
}
