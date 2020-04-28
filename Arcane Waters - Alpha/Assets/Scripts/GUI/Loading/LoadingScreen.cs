using UnityEngine;
using System.Collections.Generic;
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
      this.canvasGroup.alpha = 1f;
      this.canvasGroup.blocksRaycasts = true;
      this.canvasGroup.interactable = true;
      loadingFinishedMessage.enabled = false;
      setPercentage(0f);
   }

   public void hide () {
      this.canvasGroup.alpha = 0f;
      this.canvasGroup.blocksRaycasts = false;
      this.canvasGroup.interactable = false;
      loadingFinishedMessage.enabled = false;
   }

   public void setPercentage(float percentage) {
      percentage = Mathf.Clamp(percentage, 0, 1f);
      barImage.fillAmount = percentage;

      if (percentage >= 1f) {
         loadingFinishedMessage.enabled = true;
      }
   }

   #region Private Variables

   #endregion
}