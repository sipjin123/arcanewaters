using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class StanceTemplate : MonoBehaviour
{
   #region Public Variables

   // Basic Components
   public Slider slider;
   public Text label;
   public Button deleteButton;

   #endregion

   public void Init () {
      slider.maxValue = Enum.GetValues(typeof(Battler.Stance)).Length - 1;
      slider.onValueChanged.AddListener(_ => {
         label.text = ((Battler.Stance) slider.value).ToString();
      });
   }
}