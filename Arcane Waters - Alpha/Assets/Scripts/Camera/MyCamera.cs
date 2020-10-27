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

   // The height of the confiner
   public const float CONFINER_DEFAULT_HEIGHT = 0.162f;

   // The width of the confiner
   public const float CONFINER_DEFAULT_WIDTH = 0.16f;

   #endregion

   public override void Awake () {
      base.Awake();
      _vcam = GetComponent<Cinemachine.CinemachineVirtualCamera>();
   }

   protected override void Start () {
      CameraManager.self.registerCamera(this);
      setInternalOrthographicSize();

      _initialSettings = getVirtualCameraSettings();
      _initialSettings.ppuScale = getPPUScale();
   }

   private void OnDestroy () {
      if (CameraManager.self != null) {
         CameraManager.self.unregisterCamera(this);
      }
   }

   public override void onResolutionChanged () {
      if (gameObject.activeInHierarchy) {
         setInternalOrthographicSize();
      }
   }
      
   public static float getCharacterCreationPPUScale () {
      return 800.0f;
   }

   public void setInternalOrthographicSize () {
      _orthographicSize = (Screen.height / 2) / getPPUScale();

      // Sets the confiner to the default scale if the current scale has been modified
      if (_vcam.transform.parent != null) {
         if (_vcam.transform.parent.GetComponent<Area>() != null) {
            CinemachineConfiner confiner = _vcam.GetComponent<CinemachineConfiner>();
            if (confiner != null) {
               if (confiner.m_BoundingShape2D != null) {
                  Vector2 confinerScale = confiner.m_BoundingShape2D.transform.localScale;
                  if (confinerScale.y != CONFINER_DEFAULT_HEIGHT || confinerScale.x != CONFINER_DEFAULT_WIDTH) {
                     confiner.m_BoundingShape2D.transform.localScale = new Vector3(CONFINER_DEFAULT_WIDTH, CONFINER_DEFAULT_HEIGHT, 1);
                     D.debug(gameObject.name + ": CAMERA CONFINER Scaled into default. Height: " + CONFINER_DEFAULT_HEIGHT + ", Width: " + CONFINER_DEFAULT_WIDTH);
                  }
               }
            }

            // If the current resolution requires a bigger camera size than the current confiner bounds, modify the confiner scales            
            if (confiner != null && confiner.m_BoundingShape2D != null) {
               Bounds confinerBounds = confiner.m_BoundingShape2D.bounds;
               float camWidth = _orthographicSize * 2 * Screen.width / Screen.height;
               float confinerWidth = confinerBounds.size.x / confiner.m_BoundingShape2D.transform.localScale.x;
               float newScaleX = camWidth / confinerWidth;
                              
               if (newScaleX > confiner.m_BoundingShape2D.transform.localScale.x) {
                  Vector3 scale = confiner.m_BoundingShape2D.transform.localScale;
                  scale.x = newScaleX;
                  confiner.m_BoundingShape2D.transform.localScale = scale;
               }
            }
         } 
      }

      _vcam.m_Lens.OrthographicSize = _orthographicSize;
   }

   public Tween setOrthographicSize (float size) {
      Debug.Log("Setting size " + size);
      _orthoSizeTween?.Kill();
      _orthoSizeTween = DOTween.To(() => _orthographicSize, (x) => _orthographicSize = x, size, ANIMATION_TIME)
         .OnUpdate(() => _vcam.m_Lens.OrthographicSize = _orthographicSize);

      return _orthoSizeTween;
   }

   public Tween setPosition (Vector3 position) {
      _positionTween?.Kill();
      _positionTween = _vcam.transform.DOMove(position, ANIMATION_TIME);

      return _positionTween;
   }

   public void setDefaultSettings (float timeBefore = 0) {
      setSettings(_initialSettings, timeBefore);
   }

   public Sequence setSettings (VirtualCameraSettings settings, float timeBefore = 0) {
      Sequence s = DOTween.Sequence();
      s.AppendInterval(timeBefore);
      s.Append(setOrthographicSize((Screen.height / 2) / settings.ppuScale));
      s.Join(setPosition(settings.position));

      // Save the current settings
      _currentSettings = settings;

      _isUsingCustomSettings = _currentSettings.position != _initialSettings.position || _currentSettings.ppuScale != _initialSettings.ppuScale;

      return s;
   }

   public VirtualCameraSettings getVirtualCameraSettings () {
      VirtualCameraSettings settings = new VirtualCameraSettings();
      settings.position = _vcam.transform.position;
      settings.ppuScale = (Screen.height / 2) / _vcam.m_Lens.OrthographicSize;

      return settings;
   }

   public override float getPPUScale () {
      return _isUsingCustomSettings ? _currentSettings.ppuScale : base.getPPUScale();
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

   // The current camera settings
   protected VirtualCameraSettings _currentSettings;

   // Whether the current settings are different from the initial ones
   protected bool _isUsingCustomSettings;

   // The time needed to complete the animation
   protected const float ANIMATION_TIME = 0.25f;

   #endregion
}
