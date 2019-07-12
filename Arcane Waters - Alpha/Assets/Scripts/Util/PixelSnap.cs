using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PixelSnap : MonoBehaviour {
   #region Public Variables

   #endregion

   void LateUpdate () {
      pixelSnap();
   }

   private void pixelSnap () {
      float ppu = 100f * 2f;

      // Round the pixel value
      float nextX = Mathf.Round(ppu * transform.position.x);
      float nextY = Mathf.Round(ppu * transform.position.y);
      transform.position = new Vector3(
          nextX / ppu,
          nextY / ppu,
          transform.position.z
      );
   }

   #region Private Variables

   #endregion
}
