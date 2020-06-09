using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Text;
using Cinemachine;
using DG.Tweening;

public class LoadingScreen : MonoBehaviour
{
   #region Public Variables

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // The message that appears when the loading bar reaches 100%
   public Text loadingFinishedMessage;

   // The bar image
   public Image barImage;

   #endregion

   public void show () {
      // Don't show again if the process already started
      if (_isPreparingToShow || isShowing()) {
         return;
      }

      _startingCamera = getActiveCamera();
      _isPreparingToShow = true;

      this.gameObject.SetActive(true);
      this.canvasGroup.alpha = 0f;
      this.canvasGroup.blocksRaycasts = false;
      this.canvasGroup.interactable = false;
      loadingFinishedMessage.enabled = false;

      setPercentage(0f);

      StopAllCoroutines();
      StartCoroutine(CO_Show());
   }

   public void hide () {
      StopAllCoroutines();
      this.canvasGroup.alpha = 0;
      this.canvasGroup.blocksRaycasts = false;
      this.canvasGroup.interactable = false;
      loadingFinishedMessage.enabled = false;
      _isPreparingToShow = false;
      try {
         SpotFader.self.openSpotToMaxSize(Global.player.transform);
      } catch {
         D.error("Issue with Spot Fader");
      }
   }

   public bool isShowing () {
      return this.gameObject.activeSelf && this.canvasGroup.alpha >= 1f;
   }

   public void setPercentage (float percentage) {
      _percentage = Mathf.Clamp(percentage, 0, 1f);
   }

   private IEnumerator CO_Show () {
      // If the circle fader is running, wait until its animation ends
      if (SpotFader.self != null) {
         // If we have a player, close towards the player position. Otherwise, close towards the center of the screen.
         if (Global.player != null) {
            SpotFader.self.closeSpot(Global.player.transform.position);
         } else {
            SpotFader.self.closeSpotTowardsCenter();
         }

         // Wait a frame so the tween starts
         yield return null;

         while (SpotFader.self.isAnimatingSize()) {
            yield return null;
         }
      }

      float waitTime = 0.0f;

      // Show a empty (black) screen for a short time
      while (waitTime < MIN_TIME_BEFORE_SHOWING_BAR) {
         // If at this point the loading is finished, don't show the progress bar at all
         if (_percentage >= 1f || hasCameraChanged()) {
            hide();
            yield break;
         }

         waitTime += Time.deltaTime;
         yield return null;
      }

      this.canvasGroup.DOFade(1, 0.25f);
      this.canvasGroup.blocksRaycasts = true;
      this.canvasGroup.interactable = true;

      // Show the loading bar at 0 percent for a short time
      barImage.fillAmount = 0;
      yield return new WaitForSeconds(0.25f);

      waitTime = 0.0f;

      // Show the correct percentage
      while (_percentage < 1f) {
         if (hasCameraChanged() && Global.player != null) {
            _percentage = 1f;
            break;
         }

         barImage.fillAmount = _percentage;
         waitTime += Time.deltaTime;

         if (waitTime > LOADING_TIMEOUT) {
            D.editorLog("The maximum loading time was exceeded.");
            break;
         }

         yield return null;
      }

      while (!hasCameraChanged()) {
         yield return null;
      }

      // Show the loading bar at 100 percent for a short time
      barImage.fillAmount = 1f;
      loadingFinishedMessage.enabled = true;
      this.canvasGroup.DOFade(0, 0.2f);

      yield return new WaitForSeconds(0.25f);

      hide();
   }

   protected bool hasCameraChanged () {
      if (Global.player == null || !AreaManager.self.hasArea(Global.player.areaKey)) {
         return false;
      }

      return _startingCamera != getActiveCamera();
   }

   protected ICinemachineCamera getActiveCamera () {
      if (Camera.main == null) {
         return null;
      }

      return Camera.main.GetComponent<CinemachineBrain>().ActiveVirtualCamera;
   }

   #region Private Variables

   // The percentage of loading
   private float _percentage;

   // The active camera when started showing the loading screen
   private ICinemachineCamera _startingCamera;

   // True while the screen is being or about to be shown
   private bool _isPreparingToShow = false;

   // How much time should pass before showing the progress bar
   private const float MIN_TIME_BEFORE_SHOWING_BAR = 1f;

   // A timeout in case the loading screen gets frozen
   private const float LOADING_TIMEOUT = 15.0f;

   #endregion
}