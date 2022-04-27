using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Text;
using Cinemachine;
using DG.Tweening;
using System;
using System.Linq;
using System.Collections.Generic;

public class LoadingScreen : FullScreenSeparatePanel
{
   #region Public Variables

   // The type of process that is requiring the loading screen
   public enum LoadingType { Login = 1, MapCreation = 2, XmlExtraction = 3, CharacterCreation = 4 }

   // Canvas group of the entire loading screen
   public CanvasGroup mainCanvasGroup;

   // Canvas group of loading screen elements - progress bar, etc.
   public CanvasGroup elementsCanvasGroup;

   // The message that appears when the loading bar reaches 100%
   public Text loadingFinishedMessage;

   // The bar image
   public Image barImage;

   // The error notice section
   public GameObject errorSection;

   // The error notice text
   public Text errorSectionNotice;
   
   #endregion

   private void Awake () {
      _pixelEffectFader = CameraManager.defaultCamera.getPixelFadeEffect();
   }

   public void show (LoadingType loadingType) {
      _loadingProcesses.Add(new LoadingProcess { type = loadingType, progress = 0 });

      if (!_isShowing) {
         _isShowing = true;

         // It is possible that existing loading routines are finishing up, cancel them
         StopAllCoroutines();

         mainCanvasGroup.alpha = 0f;
         mainCanvasGroup.blocksRaycasts = true;
         mainCanvasGroup.interactable = true;
         elementsCanvasGroup.alpha = 0f;
         loadingFinishedMessage.enabled = false;
         errorSection.SetActive(false);

         // Disable input
         InputManager.toggleInput(false);

         StartCoroutine(CO_Show(_pixelEffectFader, _pixelEffectFader));
      }
   }

   public void hide (params LoadingType[] loadingTypes) {
      // Remove the loading processes
      foreach (LoadingType type in loadingTypes) {
         _loadingProcesses.RemoveAll(lp => lp.type == type);
      }
   }

   public void setProgress (LoadingType loadingType, float progress) {
      foreach (LoadingProcess loadingProcess in _loadingProcesses) {
         if (loadingProcess.type == loadingType) {
            loadingProcess.progress = progress;
         }
      }
   }

   private IEnumerator CO_Show (IScreenFader fadeOutEffect, IScreenFader fadeInEffect) {
      // Remember start time of the loading screen
      _startTime = Time.time;

      // If we have a fade out effect, display it
      if (fadeOutEffect != null) {
         Global.isScreenTransitioning = true;
         float fadeTime = fadeOutEffect.fadeOut() + ADDITIONAL_WAIT_TIME;
         mainCanvasGroup.DOFade(1.0f, fadeTime);
         yield return new WaitForSeconds(fadeTime);
         Global.isScreenTransitioning = false;
      }

      // Show a black screen for a short time
      for (float time = 0; time < MIN_TIME_BEFORE_SHOWING_BAR; time += Time.deltaTime) {
         // If at this point the loading is finished, don't show the progress bar at all
         if (getCurrentProgress() >= 1f) {
            hide(fadeInEffect);
            yield break;
         }

         yield return null;   
      }

      // Reveal loading screen elements - progress bar, etc.
      elementsCanvasGroup.DOFade(1, 0.25f);

      // Show the loading bar at 0 percent for a short time
      barImage.fillAmount = 0;
      yield return new WaitForSeconds(0.25f);

      // For our current loading percentage, lets take the slowest of all current loading processes

      while (_loadingProcesses.Count > 0) {
         float progress = getCurrentProgress();
         barImage.fillAmount = progress;

         // Check if we exceeded maximum wait time
         if (!errorSection.activeSelf && Time.time - _startTime > LOADING_TIMEOUT) {
            if (SteamManager.Initialized && _loadingProcesses[0].type == LoadingType.Login) {
               errorSectionNotice.text = "The Steam login server is currently down and unable to authenticate any login requests. Steam should have this fixed soon!";
            } 
            else {
               errorSectionNotice.text = "It looks like something went wrong. You can wait a little longer or go back to the title screen.";
            }
            
            errorSection.SetActive(true);
            D.log("The maximum loading time was exceeded.");
         }

         yield return null;
      }

      // Mark loading process as finished, in case a new loading process wants to be initialized exactly at this time
      _isShowing = false;

      // Show the loading bar at 100 percent for a short time
      barImage.fillAmount = 1f;
      loadingFinishedMessage.enabled = true;

      hide(fadeInEffect);
   }

   public void hide (IScreenFader fadeInEffect) {
      StartCoroutine(CO_Hide(fadeInEffect));
   }

   private IEnumerator CO_Hide (IScreenFader fadeInEffect) {
      // Fade out loading screen elements - progress bar, etc.
      elementsCanvasGroup.DOFade(0, 0.4f);
      yield return new WaitForSeconds(0.5f);

      // Fade out loading screen
      float duration = fadeInEffect.fadeIn();
      mainCanvasGroup.DOKill();
      mainCanvasGroup.alpha = 1.0f;
      mainCanvasGroup.DOFade(0.0f, duration);
      yield return new WaitForSeconds(duration);

      mainCanvasGroup.blocksRaycasts = false;
      mainCanvasGroup.interactable = false;
      _isShowing = false;
      Global.isScreenTransitioning = false;

      // Re-enable input
      InputManager.toggleInput(true);
   }

   public bool isShowing () {
      return _isShowing;
   }

   public float getCurrentProgress () {
      // Use slowest observer for current value
      return _loadingProcesses.Count == 0 ? 1f : _loadingProcesses.Min(lp => lp.progress);
   }

   public IScreenFader getFader () {
      return _pixelEffectFader;
   }

   public void onLogOutButtonPressed () {
      errorSection.SetActive(false);
      _startTime = Time.time;
      Util.stopHostAndReturnToTitleScreen();

      // Wait a few frames for the client to be stopped, then clear any remaining loading process
      Invoke(nameof(clearLoadingProcesses), 0.5f);
   }

   private void clearLoadingProcesses () {
      hide(Enum.GetValues(typeof(LoadingType)).Cast<LoadingType>().ToArray());
   }

   public void onWaitButtonPressed () {
      _startTime = Time.time;
      errorSection.SetActive(false);
   }

   public void setAlpha (float f) {
      mainCanvasGroup.alpha = f;
   }

   #region Private Variables

   // List of processes that require the loading screen at a given point of time
   [SerializeField]
   private List<LoadingProcess> _loadingProcesses = new List<LoadingProcess>();

   // Whether the loading screen is currently showing
   private bool _isShowing = false;

   // How much time should pass before showing the progress bar
   private const float MIN_TIME_BEFORE_SHOWING_BAR = 1f;

   // A timeout in case the loading screen gets frozen
   private const float LOADING_TIMEOUT = 35.0f;

   // Some extra time to wait after a fade transition ended
   private const float ADDITIONAL_WAIT_TIME = 0.25f;

   // A reference to the screen fader used for transitions
   private IScreenFader _pixelEffectFader;

   // The time at which the loading screen started to show
   private float _startTime = 0f;

   [Serializable]
   public class LoadingProcess
   {
      // The type of process
      public LoadingType type;

      // Progress of the loading
      public float progress;
   }

   #endregion
}