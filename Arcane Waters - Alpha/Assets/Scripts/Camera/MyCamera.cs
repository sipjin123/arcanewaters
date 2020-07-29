using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;
using Cinemachine;

public class MyCamera : BaseCamera 
{
   #region Public Variables

   // The PPU scale when creating a character
   public const float CHARACTER_CREATION_PPU_SCALE = 1200.0f;

   // Reference to the main GUI canvas
   public Canvas mainGUICanvas;

   // The scale of the confiner at wide screen
   public static float CONFINER_WIDESCREEN_SCALE = .3f;
   public static float CONFINER_DEFAULT_SCALE = 0.16f;

   #endregion

   public override void Awake () {
      base.Awake();
      _vcam = GetComponent<Cinemachine.CinemachineVirtualCamera>();
   }

   private void Start () {
      CameraManager.self.registerCamera(this);
      setInternalOrthographicSize();
   }

   public override void onResolutionChanged () {
      if (gameObject.activeInHierarchy) {
         setInternalOrthographicSize();
      }
   }

   private void setInternalOrthographicSize () {
      _orthographicSize = Screen.height / DEFAULT_PPU_SCALE * scaleModifier;

      CinemachineConfiner confiner = _vcam.GetComponent<CinemachineConfiner>(); 
      if (confiner != null) {
         if (confiner.m_BoundingShape2D != null) {
            float confinerScale = confiner.m_BoundingShape2D.transform.localScale.x;
            if (confinerScale != CONFINER_DEFAULT_SCALE) {
               confiner.m_BoundingShape2D.transform.localScale = new Vector3(CONFINER_DEFAULT_SCALE, CONFINER_DEFAULT_SCALE, 1);
            }
         }
      }

      foreach (ResolutionOrthoClamp resolutionData in CameraManager.self.resolutionList) {
         if (Screen.width >= resolutionData.resolutionWidth && _orthographicSize >= resolutionData.orthoCap) {
            if (confiner != null) {
               if (confiner.m_BoundingShape2D != null) {
                  confiner.m_BoundingShape2D.transform.localScale = new Vector3(CONFINER_WIDESCREEN_SCALE, CONFINER_DEFAULT_SCALE, 1);
               }
            }
            break;
         }
      }

      _initialSettings = getVirtualCameraSettings();
      _initialSettings.ppuScale = DEFAULT_PPU_SCALE;
   }

   void Update () {
      _vcam.m_Lens.OrthographicSize = _orthographicSize / getScaleFactor();
   }

   public Tween setOrthographicSize (float size) {
      _orthoSizeTween?.Kill();
      _orthoSizeTween = DOTween.To(() => _orthographicSize, (x) => _orthographicSize = x, size, ANIMATION_TIME);

      return _orthoSizeTween;
   }

   public Tween setPosition (Vector3 position) {
      _positionTween?.Kill();
      _positionTween = _vcam.transform.DOMove(position, ANIMATION_TIME);

      return _positionTween;
   }

   public void setDefaultSettings () {
      setSettings(_initialSettings);
   }

   public Sequence setSettings (VirtualCameraSettings settings) {
      Sequence s = DOTween.Sequence();
      s.Join(setOrthographicSize(Screen.height / settings.ppuScale));
      s.Join(setPosition(settings.position));

      return s;
   }

   private float getScaleFactor () {
      float scaleFactor = 1;
      if (mainGUICanvas != null) {
         scaleFactor = mainGUICanvas.scaleFactor *.75f;
         scaleFactor = Mathf.Clamp(scaleFactor, 1, 1.5f);
      }

      return scaleFactor;
   }

   public VirtualCameraSettings getVirtualCameraSettings () {
      VirtualCameraSettings settings = new VirtualCameraSettings();
      settings.position = _vcam.transform.position;
      settings.ppuScale = Screen.height / _vcam.m_Lens.OrthographicSize;

      return settings;
   }

   #region Private Variables

   // The current orthographic size
   [SerializeField]
   protected float _orthographicSize = -1;

   // The tween used for animating the orthographic size
   protected Tween _orthoSizeTween;

   // The tween used for animating the position
   protected Tween _positionTween;

   // The default settings
   protected VirtualCameraSettings _initialSettings;

   // The time needed to complete the animation
   protected const float ANIMATION_TIME = 0.25f;

   #endregion
}