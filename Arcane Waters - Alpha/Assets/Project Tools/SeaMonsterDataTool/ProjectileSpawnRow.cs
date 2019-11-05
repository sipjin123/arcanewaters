using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class ProjectileSpawnRow : MonoBehaviour {
   #region Public Variables

   // Coordinates
   public InputField xValue, yValue, zValue;

   // The direction of the spawn point
   public Slider directionSlider;

   // Text display of direction
   public Text directionText;

   // Deletes the template
   public Button deleteButton;

   #endregion

   public void initData() {
      directionSlider.maxValue = Enum.GetValues(typeof(Direction)).Length;
      directionSlider.onValueChanged.AddListener(_ => {
         directionText.text = ((Direction) directionSlider.value).ToString();
      });
   }

   #region Private Variables
      
   #endregion
}
