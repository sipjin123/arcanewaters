using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using Cinemachine;

public class DefaultCamera : BaseCamera {
   #region Public Variables
      
   #endregion

   #region Private Variables
      
   #endregion

   protected override void Start() {
      base.Start();
      if (Util.isBatch()) {
         var cinemachineBrain = GetComponent<CinemachineBrain>();
         if (cinemachineBrain) {
            cinemachineBrain.enabled = false;
         }

         var cam = GetComponent<Camera>();
         if (cam) {
            cam.enabled = false;
         }
      }
   }
}
