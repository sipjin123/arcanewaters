using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using Cinemachine;

public class BaseCamera : MonoBehaviour {
   #region Public Variables

   // The default PPU scale
   public const float DEFAULT_PPU_SCALE = 400.0f;

   // The Camera quake effect
   public CameraFilterPack_FX_EarthQuake quakeEffect;

   // Allows increasing or decreasing the orthographic size of the camera
   public float scaleModifier = 1.0f;

   #endregion

   public virtual void Awake () {
      _cam = GetComponent<Camera>();
      _vcam = GetComponent<CinemachineVirtualCamera>();
      _pixelFadeEffect = GetComponent<PixelFadeEffect>();
   }

   public Camera getCamera () {
      return _cam;
   }

   public CinemachineVirtualCamera getVirtualCamera () {
      return _vcam;
   }

   public float getDepth () {
      return _cam.depth;
   }

   public void setDepth (float newDepth) {
      _cam.depth = newDepth;
   }

   public void setOrthoSize (float newScale) {
      float ppu = 100f * newScale;
      _cam.orthographicSize = (Screen.height / 2f) / ppu;
   }

   public PixelFadeEffect getPixelFadeEffect () {
      return _pixelFadeEffect;
   }

   public void shakeCamera (float duration) {
      StartCoroutine(CO_ShakeCamera(duration));
   }

   private IEnumerator CO_ShakeCamera (float duration) {
      quakeEffect.enabled = true;

      // Let the effect take place for the specified number of seconds
      yield return new WaitForSeconds(duration);

      quakeEffect.enabled = false;
   }

   public virtual void onResolutionChanged () {
      if (_vcam != null) {
         _vcam.m_Lens.OrthographicSize = Screen.height / DEFAULT_PPU_SCALE;
      } else if (_cam != null) {
         _cam.orthographicSize = Screen.height / DEFAULT_PPU_SCALE;
      }
   }

   #region Private Variables

   // Our associated camera
   protected Camera _cam;

   // The associated virtual camera
   protected CinemachineVirtualCamera _vcam;

   // Our Pixel Fade effect
   protected PixelFadeEffect _pixelFadeEffect;

   #endregion
}
