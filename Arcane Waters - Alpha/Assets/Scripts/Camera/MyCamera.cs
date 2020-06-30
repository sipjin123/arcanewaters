using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;

public class MyCamera : BaseCamera 
{
   #region Public Variables

   // The default PPU scale
   public const float DEFAULT_PPU_SCALE = 400.0f;

   // The PPU scale when creating a character
   public const float CHARACTER_CREATION_PPU_SCALE = 600.0f;

   // Reference to the main GUI canvas
   public Canvas mainGUICanvas;

   #endregion

   public override void Awake () {
      base.Awake();
      _vcam = GetComponent<Cinemachine.CinemachineVirtualCamera>();

      _defaultOrthographicSize = Screen.height / DEFAULT_PPU_SCALE;
      _orthographicSize = _defaultOrthographicSize;

      _initialSettings = getVirtualCameraSettings();
   }

   void Update () {
      float scaleFactor = 1;
      if (mainGUICanvas != null) {
         scaleFactor = mainGUICanvas.scaleFactor;
      }

      _vcam.m_Lens.OrthographicSize = _orthographicSize / scaleFactor;
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

   public VirtualCameraSettings getVirtualCameraSettings () {
      VirtualCameraSettings settings = new VirtualCameraSettings();
      settings.position = _vcam.transform.position;
      settings.ppuScale = Screen.height / _vcam.m_Lens.OrthographicSize;

      return settings;
   }

   #region Private Variables

   // The current orthographic size
   protected float _orthographicSize = -1;

   // The default orthographic size for pixel perfect
   protected float _defaultOrthographicSize = -1;

   // The tween used for animating the orthographic size
   protected Tween _orthoSizeTween;

   // The tween used for animating the position
   protected Tween _positionTween;

   // Our Cinemachine Virtual Camera
   protected Cinemachine.CinemachineVirtualCamera _vcam;

   // The default settings
   protected VirtualCameraSettings _initialSettings;

   // The time needed to complete the animation
   protected const float ANIMATION_TIME = 0.25f;

   #endregion
}
