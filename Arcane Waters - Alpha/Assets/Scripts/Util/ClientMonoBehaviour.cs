using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ClientMonoBehaviour : MonoBehaviour {
   #region Public Variables

   #endregion

   protected virtual void Awake () {
      // We don't want to waste time on Client scripts in Batch Mode
      if (Application.isBatchMode) {
         this.enabled = false;
      }
   }

   #region Private Variables

   #endregion
}
