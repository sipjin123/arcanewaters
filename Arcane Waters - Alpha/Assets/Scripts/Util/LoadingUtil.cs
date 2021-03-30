using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class LoadingUtil : MonoBehaviour {
   #region Public Variables

   // Singleton instance
   public static LoadingUtil self;

   #endregion

   private void Awake () {
      D.adminLog("LoadingUtil.Awake...", D.ADMIN_LOG_TYPE.Initialization);
      self = this;
      D.adminLog("LoadingUtil.Awake: OK", D.ADMIN_LOG_TYPE.Initialization);
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
