using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class BaseCamera : MonoBehaviour {
   #region Public Variables

   // The Camera quake effect
   public CameraFilterPack_FX_EarthQuake quakeEffect;

   #endregion

   public virtual void Awake () {
      _cam = GetComponent<Camera>();
      _pixelFadeEffect = GetComponent<PixelFadeEffect>();
   }

   public Camera getCamera () {
      return _cam;
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

   #region Private Variables

   // Our associated camera
   protected Camera _cam;

   // Our Pixel Fade effect
   protected PixelFadeEffect _pixelFadeEffect;

   #endregion
}
