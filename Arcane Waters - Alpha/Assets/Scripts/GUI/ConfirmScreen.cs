using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using UnityEngine.Events;

public class ConfirmScreen : FullScreenSeparatePanel
{
   #region Public Variables

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // Our various components that we need references to
   public Text text;
   public Text cancelButtonText;
   public Text confirmButtonText;
   public Text costText;
   public Text linkText;
   public Text descriptionText;
   public TMP_Text deleteText;
   public TMP_InputField confirmInputField;
   public Button confirmButton;
   public Button cancelButton;
   public GameObject descriptionRow;
   public GameObject costRow;
   public GameObject goInputField;

   // The event triggered when hidden
   public UnityEvent hideEvent = new UnityEvent();

   #endregion

   public void showYesNo (string newText) {
      this.show(newText);

      // Update the buttons
      cancelButtonText.text = "No";
      confirmButtonText.text = "Yes";
   }

   public void show (string newText, int cost = 0, string newDescription = "") {
      transform.SetAsLastSibling();

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

      // Only show the description if it's not null
      if (string.IsNullOrWhiteSpace(newDescription)) {
         descriptionRow.SetActive(false);
      } else {
         descriptionRow.SetActive(true);
         descriptionText.text = newDescription;
      }

      show();
   }

   public void show () {
      this.canvasGroup.alpha = 1f;
      this.canvasGroup.blocksRaycasts = true;
      this.canvasGroup.interactable = true;
      this.gameObject.SetActive(true);
   }

   public void enableConfirmInputField (string keyWord) {
      // Deactive "Confirm" Button until player types in the key word into the inputfield
      PanelManager.self.confirmScreen.confirmButton.interactable = false;

      // Activate inputfield
      PanelManager.self.confirmScreen.goInputField.SetActive(true);
      PanelManager.self.confirmScreen.confirmInputField.placeholder.GetComponent<TextMeshProUGUI>().text = "Type " + keyWord;

      // Wait for input
      PanelManager.self.confirmScreen.confirmInputField.onValueChanged.AddListener((inputWord) => {
         bool isInputMatched = inputWord.ToUpper() == keyWord.ToUpper(); 
         PanelManager.self.confirmScreen.confirmButton.interactable = isInputMatched;
      });
   }

   public void hide () {
      hideEvent.Invoke();

      this.canvasGroup.alpha = 0f;
      this.canvasGroup.blocksRaycasts = false;
      this.canvasGroup.interactable = false;
      this.linkText.text = "";
      this.gameObject.SetActive(false);

      // Undo the effects of the input field so it is not displayed the next time the confirm panel is used
      PanelManager.self.confirmScreen.confirmInputField.text = "";
      PanelManager.self.confirmScreen.confirmInputField.onValueChanged.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.interactable = true;
      this.goInputField.SetActive(false);
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
