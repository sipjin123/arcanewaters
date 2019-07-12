using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class DestroyAfterAnimation : MonoBehaviour {
   #region Public Variables

   #endregion

   void Start () {
      // Destroy this object after the animation finishes
      Destroy(this.gameObject, GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length);
   }

   #region Private Variables

   #endregion
}
