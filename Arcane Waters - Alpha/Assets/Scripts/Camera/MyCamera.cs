using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MyCamera : BaseCamera {
   #region Public Variables

   #endregion

   void Start () {
      _vcam = GetComponent<Cinemachine.CinemachineVirtualCamera>();
   }

   void Update () {
      _vcam.m_Lens.OrthographicSize = Screen.height / 400f;
   }

   #region Private Variables

   // Our Cinemachine Virtual Camera
   protected Cinemachine.CinemachineVirtualCamera _vcam;

   #endregion
}
