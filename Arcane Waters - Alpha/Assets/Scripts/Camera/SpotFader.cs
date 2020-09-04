using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using Cinemachine;
using DG.Tweening;

public class SpotFader : ClientMonoBehaviour, IScreenFader {
   #region Public Variables

   // The background image
   public Image backgroundImage;

   // The spot mask image
   public Image spotMask;

   // Whether the color of the mask sprite should match the background color
   public bool matchMaskWithBackgroundColor;

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

      _currentBackgroundColorTween?.Kill();
      _currentBackgroundColorTween = backgroundImage.DOColor(color, time >= 0 ? time : _totalEffectTime * 0.5f);

      if (matchMaskWithBackgroundColor) {
         _currentSpotColorTween?.Kill();
         _currentSpotColorTween = spotMask.DOColor(color, time >= 0 ? time : _totalEffectTime * 0.5f);
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

   public void openSpotToMaxSize (Transform transform) {
      StartCoroutine(CO_OpenSpotWhenPositionIsValid(transform));
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
         _currentBackgroundColorTween?.Kill();
         _currentSizeTween?.Kill();
      }
   }

   public float fadeIn () {
      // If we have a player, close towards the player position. Otherwise, close towards the center of the screen.
      if (Global.player != null) {
         openSpotToMaxSize(Global.player.transform.position);
      } else if (CharacterScreen.self.isShowing() && CharacterSpot.lastInteractedSpot != null && CharacterSpot.lastInteractedSpot.character != null) {
         openSpotToMaxSize(CharacterSpot.lastInteractedSpot.character.transform.position);
      } else {
         openSpotToMaxSize(Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f)));
      }

      return _totalEffectTime;
   }

   public float fadeOut () {
      fadeBackgroundColor(_defaultColor);

      // If we have a player, close towards the player position. Otherwise, close towards the center of the screen.
      if (Global.player != null) {
         closeSpot(Global.player.transform.position);
      } else if (CharacterScreen.self.isShowing() && CharacterSpot.lastInteractedSpot != null && CharacterSpot.lastInteractedSpot.character != null) {
         closeSpot(CharacterSpot.lastInteractedSpot.character.transform.position);
      } else {
         closeSpotTowardsCenter();
      }

      return _totalEffectTime;
   }

   #region Private Variables

   // The default color of the background image
   [SerializeField]
   private Color _defaultColor = Color.black;

   // The total time to complete the effect
   [SerializeField]
   private float _totalEffectTime = 0.5f;

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
