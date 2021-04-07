using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;

public class CameraFader : MonoBehaviour {
   #region Public Variables

   // Singleton instance
   public static CameraFader self;

   // The duration of fade effects for this fader
   public static float FADE_DURATION = 1.0f;

   #endregion

   private void Awake () {
      self = this;
   }

   void Start () {
      _canvasGroup = GetComponent<CanvasGroup>();
      _loadingIndicatorCanvasGroup = _loadingIndicator.GetComponent<CanvasGroup>();
   }

   public void fadeIn () {
      fadeIn(1.0f);
   }

   public void fadeOut () {
      fadeOut(1.0f);
   }

   public Tween fadeIn (float endAlpha) {
      if (_fadeTween != null) {
         _fadeTween.Kill();
      }

      if (_loadingIndicatorFadeTween != null) {
         _loadingIndicatorFadeTween.Kill();
      }
      
      _fadeTween = _canvasGroup.DOFade(endAlpha, FADE_DURATION);
      _loadingIndicatorCanvasGroup.alpha = 0.0f;
      _loadingIndicatorFadeTween = _loadingIndicatorCanvasGroup.DOFade(1.0f, FADE_DURATION);
      return _fadeTween;
   }

   public Tween fadeOut (float startAlpha) {
      if (_fadeTween != null) {
         _fadeTween.Kill();
      }

      if (_loadingIndicatorFadeTween != null) {
         _loadingIndicatorFadeTween.Kill();
      }

      _canvasGroup.alpha = startAlpha;
      _fadeTween = _canvasGroup.DOFade(0.0f, FADE_DURATION);
      _loadingIndicatorCanvasGroup.alpha = 1.0f;
      _loadingIndicatorFadeTween = _loadingIndicatorCanvasGroup.DOFade(0.0f, FADE_DURATION);
      return _fadeTween;
   }

   public void setLoadingIndicatorVisibility (bool isVisible) {
      _loadingIndicator.SetActive(isVisible);
   }

   #region Private Variables

   // Our Canvas Group
   protected CanvasGroup _canvasGroup;

   // A reference to the active tween for this fader
   protected Tween _fadeTween;

   // A reference to the active tween for fading the loading indicator
   protected Tween _loadingIndicatorFadeTween;

   // A reference to the loading indicator
   [SerializeField]
   protected GameObject _loadingIndicator;

   // A reference to the canvas group for our loading indicator
   protected CanvasGroup _loadingIndicatorCanvasGroup;

   #endregion
}
