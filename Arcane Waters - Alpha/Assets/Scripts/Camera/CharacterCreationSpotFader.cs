using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using Cinemachine;
using DG.Tweening;
using System;

public class CharacterCreationSpotFader : ClientMonoBehaviour
{
   #region Public Variables

   // The background image
   public Image backgroundImage;

   // The spot mask image
   public Image spotMask;

   // Whether the color of the mask sprite should match the background color
   public bool matchMaskWithBackgroundColor;

   // Self
   public static CharacterCreationSpotFader self;

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;
      _canvasGroup = GetComponent<CanvasGroup>();

      Util.disableCanvasGroup(_canvasGroup);
   }

   private void Start () {
      MAX_SPOT_SIZE = new Vector2(Screen.width * MAX_SIZE_MULTIPLIER, Screen.width * MAX_SIZE_MULTIPLIER);
      spotMask.rectTransform.sizeDelta = MAX_SPOT_SIZE;

      // Update the position of the spot mask if the resolution changes
      CameraManager.self.resolutionChanged += onResolutionChanged;
   }
   
   private void onResolutionChanged () {
      StartCoroutine(CO_UpdateSpotPosition());
   }

   private IEnumerator CO_UpdateSpotPosition () {
      // Wait until the camera size is updated
      yield return null;
      yield return null;

      // If the screen is visible, update the spot position
      if (_canvasGroup.alpha > 0) {
         spotMask.rectTransform.position = Camera.main.WorldToScreenPoint(_currentFocusPosition) + _highlightPlayerOffset;
      }
   }

   public void openSpotToMaxSize () {
      if (self == null) {
         return;
      }

      _currentSizeTween?.Kill();
      setFadeInitialValues();
      _currentSizeTween = spotMask.rectTransform.DOSizeDelta(MAX_SPOT_SIZE, _totalEffectTime * 0.5f).OnComplete(() => {
         Util.disableCanvasGroup(_canvasGroup);
      });
   }

   public void openSpotToMaxSize (Vector3 worldPosition) {
      spotMask.rectTransform.position = Camera.main.WorldToScreenPoint(worldPosition);
      openSpotToMaxSize();
   }

   public void closeAndHide () {
      closeSpotInternal(spotMask.rectTransform.position, true);
   }

   private void closeSpotInternal (Vector3 screenPosition, bool hideOnComplete = false) {
      setFadeInitialValues();
      _currentSizeTween?.Kill();

      spotMask.rectTransform.position = screenPosition;

      _currentSizeTween = spotMask.rectTransform.DOSizeDelta(Vector2.zero, _totalEffectTime * 0.1f);

      if (hideOnComplete) {
         _currentSizeTween.OnComplete(() => Util.disableCanvasGroup(_canvasGroup));
      }
   }

   public void closeTowardsOfflineChar (Vector3 worldPosition) {
      if (self == null) {
         return;
      }

      _currentFocusPosition = worldPosition;

      _currentSizeTween?.Kill();
      spotMask.rectTransform.sizeDelta = MAX_SPOT_SIZE;
      spotMask.rectTransform.position = Camera.main.WorldToScreenPoint(worldPosition) + _highlightPlayerOffset;
      setFadeInitialValues();
      _currentSizeTween = spotMask.rectTransform.DOSizeDelta(_highlightPlayerSpotSize, _totalEffectTime * 0.5f);
   }

   public void setColor (Color color) {
      if (self == null) {
         return;
      }

      _currentBackgroundColorTween?.Kill();
      backgroundImage.color = color;

      if (matchMaskWithBackgroundColor) {
         _currentSpotColorTween?.Kill();
         spotMask.color = color;
      }
   }

   private void setFadeInitialValues () {
      MAX_SPOT_SIZE = new Vector2(Screen.width * MAX_SIZE_MULTIPLIER, Screen.width * MAX_SIZE_MULTIPLIER);

      Util.enableCanvasGroup(_canvasGroup);
   }

   public bool isAnimatingAny () {
      return isAnimatingSize() || isAnimatingColor();
   }

   public bool isAnimatingSize () {
      return _currentSizeTween != null && (_currentSizeTween.active && !_currentSizeTween.IsComplete());
   }

   public bool isAnimatingColor () {
      return _currentBackgroundColorTween != null && _currentBackgroundColorTween.active && _currentBackgroundColorTween.IsPlaying();
   }

   private void OnDestroy () {
      StopAllCoroutines();

      if (isAnimatingAny()) {
         _currentBackgroundColorTween?.Kill();
         _currentSizeTween?.Kill();
      }

      if (CameraManager.self != null) {
         CameraManager.self.resolutionChanged -= onResolutionChanged;
      }
   }

   #region Private Variables

   // The default spot size, enough to highlight a player
   [SerializeField]
   private Vector2 _highlightPlayerSpotSize = new Vector2(200, 300);

   // The world position offset of the mask relative to the player
   [SerializeField]
   private Vector3 _highlightPlayerOffset = new Vector2(6, 100);

   // The default color of the background image
   [SerializeField]
   private Color _defaultColor = Color.black;

   // The total time to complete the effect
   [SerializeField]
   private float _totalEffectTime = 0.5f;

   // The world position we're currently focusing on
   private Vector3 _currentFocusPosition;

   // The tween that's changing the spot size
   private Tween _currentSizeTween;

   // The tween that's changing the color of the background
   private Tween _currentBackgroundColorTween;

   // The tween that's changing the color of the spot
   private Tween _currentSpotColorTween;

   // The canvas group
   private CanvasGroup _canvasGroup;

   // The max spot size, enough to go become invisible 
   private static Vector2 MAX_SPOT_SIZE;

   // The max size multiplier 
   private const float MAX_SIZE_MULTIPLIER = 3.0f;

   #endregion
}
