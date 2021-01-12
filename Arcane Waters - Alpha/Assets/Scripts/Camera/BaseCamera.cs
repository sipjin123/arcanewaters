using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using Cinemachine;

public class BaseCamera : MonoBehaviour {
   #region Public Variables

   // The default PPU scale
   public const float DEFAULT_PPU_SCALE = 100.0f;

   // The Camera quake effect
   public CameraFilterPack_FX_EarthQuake quakeEffect;

   // Whether to use a custom PPU scale or the default one
   public bool useCustomPPUScale = false;

   // The PPU scale of this camera
   public float customPPUScale = -1.0f;

   // Is this camera targeting a battle scebe, or a scene which features a battle scene
   public bool isBattleScreenCamera;

   #endregion

   public virtual void Awake () {
      _cam = GetComponent<Camera>();
      _vcam = GetComponent<CinemachineVirtualCamera>();
      _pixelFadeEffect = GetComponent<PixelFadeEffect>();
   }

   protected virtual void Start () {

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
         _vcam.m_Lens.OrthographicSize = (Screen.height / 2) / getPPUScale();
      } else if (_cam != null) {
         _cam.orthographicSize = (Screen.height / 2) / getPPUScale();
      }
   }

   public virtual float getPPUScale () {
      float ppu = useCustomPPUScale ? customPPUScale : DEFAULT_PPU_SCALE;
      return ppu * getConstantCameraScalingFactor(isBattleScreenCamera);
   }

   public static float getConstantCameraScalingFactor (bool isBattleScreen = false) {
      // NOTE: as of writing, we are using the 'battle' screen for title and character screens,
      // so for them isBattleScreen should be set to 'true'

      float sizeFactor =
         (Screen.width >= ScreenSettingsManager.largeScreenWidth && Screen.height >= ScreenSettingsManager.largeScreenHeight)
         ? 2f
         : 1f;

      float typeFactor = isBattleScreen ? 3f : 2f;

      return sizeFactor * typeFactor;
   }

   public CinemachineFramingTransposer getFramingTransposer () {
      if (_framingTransposer == null) {
         if (getVirtualCamera() != null) {
            // Cache the CinemachineFramingTransponser for future uses
            _framingTransposer = getVirtualCamera().GetCinemachineComponent<CinemachineFramingTransposer>();
         }
      }

      return _framingTransposer;
   }

   #region Private Variables

   // Our associated camera
   protected Camera _cam;

   // The associated virtual camera
   [SerializeField]
   protected CinemachineVirtualCamera _vcam;

   // Our Pixel Fade effect
   protected PixelFadeEffect _pixelFadeEffect;

   // The framing transposer cinemachine component
   protected CinemachineFramingTransposer _framingTransposer;

   #endregion
}
