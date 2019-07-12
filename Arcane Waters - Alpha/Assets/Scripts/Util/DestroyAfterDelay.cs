using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class DestroyAfterDelay : MonoBehaviour {
   #region Public Variables

   // How long to wait before destroying this object
   public float delay = 3f;

   #endregion

   void Start () {
      Invoke("destroyThis", delay);
   }

   protected void destroyThis () {
      Destroy(this.gameObject);
   }

   #region Private Variables

   #endregion
}
