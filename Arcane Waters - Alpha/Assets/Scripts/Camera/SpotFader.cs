using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using Cinemachine;
using DG.Tweening;

public class SpotFader : ClientMonoBehaviour {
   #region Public Variables

   // The background image
   public Image backgroundImage;

   // The spot mask image
   public Image spotMask;

   // Self
   public static SpotFader self;

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
   }

   public void fadeBackgroundColor (Color color, float time = -1) {
      if (self == null) {
         return;
      }

      _currentColorTween?.Kill();
      _currentColorTween = backgroundImage.DOColor(color, time >= 0 ? time : _totalEffectTime * 0.5f);
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

   public void openSpotToMaxSize (Transform transform) {
      StartCoroutine(CO_OpenSpotWhenPositionIsValid(transform));
   }


   public void closeTowardsOfflineChar (Vector3 worldPosition) {
      if (self == null) {
         return;
      }

      _currentSizeTween?.Kill();
      spotMask.rectTransform.sizeDelta = MAX_SPOT_SIZE;
      spotMask.rectTransform.position = Camera.main.WorldToScreenPoint(worldPosition);
      setFadeInitialValues();
      _currentSizeTween = spotMask.rectTransform.DOSizeDelta(_highlightPlayerSpotSize, _totalEffectTime * 0.5f);
   }

   public void closeSpot () {
      closeSpotInternal(spotMask.rectTransform.position);
   }

   public void closeSpotTowardsCenter () {
      closeSpotInternal(Camera.main.ViewportToScreenPoint(new Vector3(0.5f, 0.5f)));
   }

   public void closeSpot (Vector3 worldPosition) {
      Camera cam = Camera.main;

      if (cam != null) {
         closeSpotInternal(Camera.main.WorldToScreenPoint(worldPosition));
      }
   }

   private void closeSpotInternal (Vector3 screenPosition) {
      setFadeInitialValues();
      _currentSizeTween?.Kill();
            
      spotMask.rectTransform.position = screenPosition;
      
      _currentSizeTween = spotMask.rectTransform.DOSizeDelta(Vector2.zero, _totalEffectTime * 0.5f);
   }

   public void setColor (Color color) {
      if (self == null) {
         return;
      }

      _currentColorTween?.Kill();
      backgroundImage.color = color;
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
      return _currentColorTween != null && _currentColorTween.active && _currentColorTween.IsPlaying();
   }

   private IEnumerator CO_OpenSpotWhenPositionIsValid (Transform transform) {
      Camera cam = Camera.main;
      Vector3 position = cam.WorldToScreenPoint(transform.position);
      
      while (position.x < 0 || position.x > Screen.width || position.y < 0 || position.y > Screen.height) {
         position = cam.WorldToScreenPoint(transform.position);
         yield return null;
      }

      openSpotToMaxSize(transform.position);
   }

   private void OnDestroy () {
      StopAllCoroutines();

      if (isAnimatingAny()) {
         _currentColorTween?.Kill();
         _currentSizeTween?.Kill();
      }
   }

   #region Private Variables

   // The default spot size, enough to highlight a player
   [SerializeField]
   private Vector2 _highlightPlayerSpotSize = new Vector2(400, 400);

   // The default color of the background image
   [SerializeField]
   private Color _defaultColor = Color.black;

   // The total time to complete the effect
   [SerializeField]
   private float _totalEffectTime = 0.5f;

   // The tween that's changing the spot size
   private Tween _currentSizeTween;

   // The tween that's changing the color
   private Tween _currentColorTween;

   // The canvas group
   private CanvasGroup _canvasGroup;

   // The max spot size, enough to go become invisible 
   private static Vector2 MAX_SPOT_SIZE;

   // The max size multiplier 
   private const float MAX_SIZE_MULTIPLIER = 3.0f;

   #endregion
}
