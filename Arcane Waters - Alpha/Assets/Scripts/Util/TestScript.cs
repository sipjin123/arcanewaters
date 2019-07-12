using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TestScript : MonoBehaviour {
   #region Public Variables

   #endregion

   private void Update () {
      if (Input.GetKeyUp(KeyCode.Alpha1)) {
         GetComponent<SimpleAnimation>().playAnimation(Anim.Type.Run_East);
      }
      if (Input.GetKeyUp(KeyCode.Alpha2)) {
         GetComponent<SimpleAnimation>().playAnimation(Anim.Type.Run_North);
      }
   }

   #region Private Variables

   #endregion
}
