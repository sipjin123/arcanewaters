using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using Cinemachine;

public class SoonGif_CameraControl : MonoBehaviour
{
   #region Public Variables

   // The camera we control
   public CinemachineVirtualCamera targetCamera;

   // Target sizes
   public float zoomInSize = 1f;
   public float zoomOutSize = 16f;

   // Zoom speed
   public float zoomSpeed = 20f;

   #endregion

   private void Update () {
      float targetZoom = zoomInSize;
      if (KeyUtils.GetKey(UnityEngine.InputSystem.Key.Numpad8)) {
         targetZoom = zoomOutSize;
      }

      LensSettings lens = targetCamera.m_Lens;

      lens.OrthographicSize = Mathf.MoveTowards(lens.OrthographicSize, targetZoom, zoomSpeed * Time.deltaTime);

      // Adjust size for PPU scale
      float pixelWorldSize = 0.0512f;
      lens.OrthographicSize = Mathf.RoundToInt(lens.OrthographicSize / pixelWorldSize) * pixelWorldSize;

      targetCamera.m_Lens = lens;
   }

   #region Private Variables



   #endregion
}
