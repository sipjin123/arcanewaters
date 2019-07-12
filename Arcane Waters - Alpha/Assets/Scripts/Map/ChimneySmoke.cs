using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ChimneySmoke : ClientMonoBehaviour {
   #region Public Variables

   #endregion

   private void Update () {
      // Slowly move upwards and to the right
      Vector3 currentPos = this.transform.position;
      currentPos += new Vector3(.05f, .15f) * Time.deltaTime;
      this.transform.position = currentPos;
   }

   #region Private Variables

   #endregion
}
