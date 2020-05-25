using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Text;

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
      this.canvasGroup.alpha = 0f;
      this.canvasGroup.blocksRaycasts = false;
      this.canvasGroup.interactable = false;
      loadingFinishedMessage.enabled = false;
   }

   public bool isShowing () {
      return this.gameObject.activeSelf && this.canvasGroup.alpha >= 1f;
   }

   public void setPercentage (float percentage) {
      _percentage = Mathf.Clamp(percentage, 0, 1f);
   }

   private IEnumerator CO_Show () {
      // Wait a few frames, so that the circle fader has time to start its animation
      yield return new WaitForSeconds(0.1f);

      // If the circle fader is running, wait until its animation ends
      while (CircleFader.self != null && CircleFader.self.isAnimating()) {
         yield return null;
      }

      // Show a empty (black) screen for a short time
      yield return new WaitForSeconds(0.2f);

      // If at this point the loading is finished, don't show the loading bar at all
      if (_percentage >= 1f) {
         hide();
         yield break;
      }

      this.canvasGroup.alpha = 1f;
      this.canvasGroup.blocksRaycasts = true;
      this.canvasGroup.interactable = true;

      // Show the loading bar at 0 percent for a short time
      barImage.fillAmount = 0;
      yield return new WaitForSeconds(0.5f);

      // Show the correct percentage
      while (_percentage < 1f) {
         barImage.fillAmount = _percentage;
         yield return null;
      }

      // Show the loading bar at 100 percent for a short time
      barImage.fillAmount = 1f;
      loadingFinishedMessage.enabled = true;
      yield return new WaitForSeconds(0.5f);

      hide();
   }

   #region Private Variables

   // The percentage of loading
   private float _percentage;

   #endregion
}