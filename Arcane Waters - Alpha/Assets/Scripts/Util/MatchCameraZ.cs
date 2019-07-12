using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MatchCameraZ : ClientMonoBehaviour {
   #region Public Variables

   #endregion

   void Update () {
      Util.setZ(this.transform, Camera.main.transform.position.z);
   }

   #region Private Variables

   #endregion
}
