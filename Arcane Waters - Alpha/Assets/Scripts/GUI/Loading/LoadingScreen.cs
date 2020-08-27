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

   // Canvas group of the entire loading screen
   public CanvasGroup mainCanvasGroup;

   // Canvas group of loading screen elements - progress bar, etc.
   public CanvasGroup elementsCanvasGroup;

   // The message that appears when the loading bar reaches 100%
   public Text loadingFinishedMessage;

   // The bar image
   public Image barImage;

   #endregion

   // progressHandler should return the loading progress from 0 to 1, where 1 will indicate that the loading is complete
   public void show (Func<float> progressHandler, IScreenFader fadeOutEffect, IScreenFader fadeInEffect) {
      _loadingObservers.Add(progressHandler);
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

   private IEnumerator CO_Show (IScreenFader fadeOutEffect, IScreenFader fadeInEffect) {
      // Remember start time of the loading screen
      float startTime = Time.time;

      // If we have a fade out effect, display it
      if (fadeOutEffect != null) {
         yield return new WaitForSeconds(fadeOutEffect.fadeOut());
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

         yield return new WaitForEndOfFrame();
      }

      // Reveal loading screen elements - progress bar, etc.
      elementsCanvasGroup.DOFade(1, 0.25f);

      // Show the loading bar at 0 percent for a short time
      barImage.fillAmount = 0;
      yield return new WaitForSeconds(0.25f);

      // For our current loading percentage, lets take the slowest of all current loading processes
      float progress;
      while ((progress = getCurrentProgress()) < 1f) {
         barImage.fillAmount = progress;

         // Check if we exceeded maximum wait time
         if (Time.time - startTime > LOADING_TIMEOUT) {
            D.editorLog("The maximum loading time was exceeded.");
            break;
         }

         yield return new WaitForEndOfFrame();
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
      float min = 1f;

      for (int i = 0; i < _loadingObservers.Count; i++) {
         float progress = _loadingObservers[i].Invoke();

         // Remove completed progresses
         if (progress >= 1f) {
            _loadingObservers.RemoveAt(i);
            i--;
            continue;
         }

         if (progress < min) {
            min = progress;
         }
      }

      return min;
   }

   #region Private Variables

   // List of functions, created by systems that are currently loading. Functions return their loading progress from 0 to 1
   private List<Func<float>> _loadingObservers = new List<Func<float>>();

   // Whether the loading screen is currently showing
   private bool _isShowing = false;

   // How much time should pass before showing the progress bar
   private const float MIN_TIME_BEFORE_SHOWING_BAR = 1f;

   // A timeout in case the loading screen gets frozen
   private const float LOADING_TIMEOUT = 15.0f;

   #endregion
}