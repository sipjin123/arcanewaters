using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class EffectResidue : MonoBehaviour {
   #region Public Variables

   #endregion

   private void Update () {
      transform.localPosition = new Vector3(0, 0, transform.localPosition.z);
   }

   #region Private Variables

   #endregion
}
