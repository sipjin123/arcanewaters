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

public class LoadingScreen : MonoBehaviour
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

   #endregion

   public void show (LoadingType loadingType, IScreenFader fadeOutEffect, IScreenFader fadeInEffect) {
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

         StartCoroutine(CO_Show(fadeOutEffect, fadeInEffect));
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
      float startTime = Time.time;

      // If we have a fade out effect, display it
      if (fadeOutEffect != null) {
         Global.isScreenTransitioning = true;
         float fadeTime = fadeOutEffect.fadeOut() + ADDITIONAL_WAIT_TIME;
         yield return new WaitForSeconds(fadeTime);
         Global.isScreenTransitioning = false;
      }

      // Blacken the screen
      mainCanvasGroup.alpha = 1f;

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
         if (Time.time - startTime > LOADING_TIMEOUT) {
            D.log("The maximum loading time was exceeded.");
            break;
         }

         yield return null;
      }

      // Mark loading process as finished, in case a new loading process wants to be initialized exactly at this time
      _isShowing = false;

      // Show the loading bar at 100 percent for a short time
      barImage.fillAmount = 1f;
      loadingFinishedMessage.enabled = true;

      // Fade out loading screen elements - progress bar, etc.
      elementsCanvasGroup.DOFade(0, 0.2f);
      yield return new WaitForSeconds(0.25f);

      hide(fadeOutEffect);
   }

   public void hide (IScreenFader fadeInEffect) {
      mainCanvasGroup.alpha = 0;
      mainCanvasGroup.blocksRaycasts = false;
      mainCanvasGroup.interactable = false;
      _isShowing = false;
      Global.isScreenTransitioning = false;

      // If we have a fade in effect, show it
      if (fadeInEffect != null) {
         fadeInEffect.fadeIn();
      }
   }

   public bool isShowing () {
      return _isShowing;
   }

   public float getCurrentProgress () {
      // Use slowest observer for current value
      return _loadingProcesses.Count == 0 ? 1f : _loadingProcesses.Min(lp => lp.progress);
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
   private const float LOADING_TIMEOUT = 45.0f;

   // Some extra time to wait after a fade transition ended
   private const float ADDITIONAL_WAIT_TIME = 0.25f;

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