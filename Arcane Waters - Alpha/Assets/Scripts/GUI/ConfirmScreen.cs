using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ConfirmScreen : MonoBehaviour {
   #region Public Variables

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // Our various components that we need references to
   public Text text;
   public Text cancelButtonText;
   public Text confirmButtonText;
   public Text costText;
   public Text linkText;
   public Button confirmButton;
   public GameObject costRow;

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

   public void show(string newText, int cost) {
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

   public void show() {
      this.canvasGroup.alpha = 1f;
      this.canvasGroup.blocksRaycasts = true;
      this.canvasGroup.interactable = true;
      this.gameObject.SetActive(true);
   }

   public void hide () {
      this.canvasGroup.alpha = 0f;
      this.canvasGroup.blocksRaycasts = false;
      this.canvasGroup.interactable = false;
      this.linkText.text = "";
      this.gameObject.SetActive(false);
   }

   public void openURL () {
      string link = linkText.text;

      if (!link.StartsWith("http")) {
         link = "http://www." + link;
      }
      Application.OpenURL(link);
   }

   #region Private Variables

   #endregion
}
