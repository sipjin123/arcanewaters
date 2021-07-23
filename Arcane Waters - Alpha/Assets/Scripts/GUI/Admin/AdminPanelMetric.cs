using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AdminPanelMetric : MonoBehaviour {
   #region Public Variables

   // Reference to the label containing the name of the metric
   public Text metricNameLabel;

   // Reference to the label containing the value of the metric
   public Text metricValueLabel;

   // Reference to the canvas group
   public CanvasGroup canvasGroup;

   #endregion

   public void setName(string name) {
      if (metricNameLabel == null) {
         return;
      }

      metricNameLabel.text = name;
   }

   public void setValue(string value) {
      if (metricValueLabel == null) {
         return;
      }

      metricValueLabel.text = value;
   }

   public string getName () {
      if (metricNameLabel == null) {
         return "";
      }

      return metricNameLabel.text;
   }

   public string getValue () {
      if (metricValueLabel == null) {
         return "";
      }

      return metricValueLabel.text;
   }

   public void hide () {
      if (canvasGroup == null) {
         return;
      }

      canvasGroup.alpha = 0;
   }

   public void show () {
      if (canvasGroup == null) {
         return;
      }

      canvasGroup.alpha = 1;
   }

   #region Private Variables
      
   #endregion
}
