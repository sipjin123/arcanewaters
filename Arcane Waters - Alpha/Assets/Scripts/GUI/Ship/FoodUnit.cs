﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class FoodUnit : MonoBehaviour
{
   #region Public Variables

   // The images we use to indicate how much of this food is left
   public Image foodFillImage;
   public Image foodBgImage;
   public Image foodEatenImage;

   #endregion

   public void setAmountLeft (float amount) {
      foodEatenImage.enabled = amount <= 0;
      foodFillImage.enabled = amount > 0;
      foodBgImage.enabled = amount > 0;

      if (amount > 0) {
         foodFillImage.fillAmount = Mathf.Clamp01(amount);
      }
   }

   public void activate () {
      if (!gameObject.activeSelf) {
         gameObject.SetActive(true);
      }
   }

   public void deactivate () {
      if (gameObject.activeSelf) {
         gameObject.SetActive(false);
      }
   }

   #region Private Variables

   #endregion
}
