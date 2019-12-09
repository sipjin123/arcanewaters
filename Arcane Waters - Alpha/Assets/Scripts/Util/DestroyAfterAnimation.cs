using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class DestroyAfterAnimation : MonoBehaviour {
   #region Public Variables

   // Reference to the animator
   public Animator animator;

   #endregion

   void Start () {
      // Destroy this object after the animation finishes
      Destroy(this.gameObject, animator == null ? GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).length : animator.GetCurrentAnimatorStateInfo(0).length);
   }

   #region Private Variables

   #endregion
}
