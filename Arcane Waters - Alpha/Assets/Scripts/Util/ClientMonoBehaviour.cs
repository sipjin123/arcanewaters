using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ClientMonoBehaviour : MonoBehaviour
{
   #region Public Variables

   // Override the base enabled logic to force disable in batch mode
   public new bool enabled
   {
      get
      {
         return base.enabled;
      }
      set
      {
         if (Util.isBatch()) {
            base.enabled = false;
         } else {
            base.enabled = value;
         }
      }
   }

   #endregion

   protected virtual void Awake () {
      // We don't want to waste time on Client scripts in Batch Mode
      if (Util.isBatch()) {
         enabled = false;
      }
   }

   #region Private Variables

   #endregion
}
