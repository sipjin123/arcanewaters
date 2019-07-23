using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TradeConfirmScreen : MonoBehaviour
{
   #region Public Variables

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // The Crop Type we're dealing with
   public Crop.Type cropType;

   // The maximum amount of crops that can be sold
   public int maxAmount;

   // Our various components that we need references to
   public Text text;
   public Text cancelButtonText;
   public Text confirmButtonText;
   public Text costText;
   public Button confirmButton;
   public GameObject costRow;
   public Slider slider;
   public Text sliderText;

   #endregion

   public void showYesNo (string newText) {
      this.show(newText);

      // Update the buttons
      cancelButtonText.text = "No";
      confirmButtonText.text = "Yes";
   }

   public void show (string newText) {
      this.show(newText, 0);
   }

   public void show (string newText, int cost) {
      text.text = newText;

      // Only show the Cost row if there's a cost associated
      if (cost > 0) {
         costRow.SetActive(true);
         costText.text = cost + "";
      } else {
         costRow.SetActive(false);
      }

      // Standard button text
      cancelButtonText.text = "Cancel";
      confirmButtonText.text = "Confirm";

      // Now make us visible
      show();
   }

   public void show () {
      sliderChanged();
      this.canvasGroup.alpha = 1f;
      this.canvasGroup.blocksRaycasts = true;
      this.canvasGroup.interactable = true;
      this.gameObject.SetActive(true);
   }

   public void hide () {
      this.canvasGroup.alpha = 0f;
      this.canvasGroup.blocksRaycasts = false;
      this.canvasGroup.interactable = false;
   }

   public void sliderChanged () {
      int cropCount = CargoBoxManager.self.getCargoCount(this.cropType);

      // Allow to sell up to the maximum of the offer
      cropCount = cropCount < maxAmount ? cropCount : maxAmount;

      // Update the text associated with the slider
      sliderText.text = (int) (slider.value * cropCount) + "";
   }

   #region Private Variables

   #endregion
}
