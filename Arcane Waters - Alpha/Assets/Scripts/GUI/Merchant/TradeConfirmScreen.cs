using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class TradeConfirmScreen : MonoBehaviour
{
   #region Public Variables

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

   // The Crop Type we're dealing with
   public Crop.Type cropType;

   // The maximum amount of crops that can be sold
   public int maxAmount;

   // The price per unit of crop
   public int pricePerUnit;

   // Our various components that we need references to
   public Text text;
   public Text cancelButtonText;
   public Text confirmButtonText;
   public Button confirmButton;
   public InputField amountInput;
   public Text sellValueText;
   public Image cropImage;

   #endregion

   public void showYesNo (string newText) {
      this.show(newText);

      // Update the buttons
      cancelButtonText.text = "No";
      confirmButtonText.text = "Yes";
   }

   public void show (string newText) {
      text.text = newText;

      // Standard button text
      cancelButtonText.text = "Cancel";
      confirmButtonText.text = "Confirm";

      setAmount(maxAmount);
      cropImage.sprite = ImageManager.getSprite("Cargo/" + cropType);

      // Now make us visible
      show();
   }

   public void show () {
      setAmount(_amount);
      this.canvasGroup.alpha = 1f;
      this.canvasGroup.blocksRaycasts = true;
      this.canvasGroup.interactable = true;
      this.gameObject.SetActive(true);
   }

   public void hide () {
      this.canvasGroup.alpha = 0f;
      this.canvasGroup.blocksRaycasts = false;
      this.canvasGroup.interactable = false;
      this.gameObject.SetActive(false);
   }

   public void onUpButtonClicked () {
      setAmount(_amount + 1);
   }

   public void onDownButtonClicked () {
      setAmount(_amount - 1);
   }

   public void onMaxButtonClicked () {
      setAmount(maxAmount);
   }

   public void onAmountInputValueChanged () {
      // While the user is writing the value, only update the displayed sell value when possible
      if (int.TryParse(amountInput.text, out int parsedAmount)) {
         sellValueText.text = (parsedAmount * pricePerUnit).ToString();
      } else {
         sellValueText.text = "0";
      }
   }

   public void onAmountInputEndEdit () {
      if (int.TryParse(amountInput.text, out int parsedAmount)) {
         setAmount(parsedAmount);
      } else {
         // Restore the previous value
         setAmount(_amount);
      }
   }

   private void setAmount(int amount) {
      _amount = Mathf.Clamp(amount, 0, maxAmount);
      amountInput.SetTextWithoutNotify(_amount.ToString());
      sellValueText.text = (_amount * pricePerUnit).ToString();
   }

   public int getAmount () {
      return _amount;
   }

   #region Private Variables

   // The amount to trade
   private int _amount = 0;

   #endregion
}
