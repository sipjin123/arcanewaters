﻿using UnityEngine;
using UnityEngine.UI;

public class WorldMapPanelPin : MonoBehaviour
{
   #region Public Variables

   // Reference to the object that contains the information about the pin
   public WorldMapSpot spot;

   // Reference to the control that displays the image of the pin
   public Image image;

   // Reference to the rect transform of the pin
   public RectTransform rect;

   // Tooltip
   public ToolTipComponent tooltip;

   // The label of the pin
   public TMPro.TextMeshProUGUI label;

   // The iconlabel of the pin
   public TMPro.TextMeshProUGUI iconLabel;

   // Should the label be repositioned
   public bool shouldNudgeLabel = false;

   #endregion

   public void setLabel (string text) {
      _name = text;

      if (label == null) {
         return;
      }

      label.SetText(text);

      if (iconLabel == null) {
         return;
      }

      iconLabel.SetText(text);

      if (text.Length > 0) {
         iconLabel.SetText(text.Substring(0,1).ToString());
      }
   }

   public string getLabel () {
      return _name;
   }

   public void setSprite(Sprite sprite) {
      if (image == null) {
         return;
      }

      image.sprite = sprite;
   }

   public void toggle(bool show) {
      gameObject.SetActive(show);

   }

   public void toggleLabel (bool show) {
      if (label == null) {
         return;
      }

      label.gameObject.SetActive(show);
   }

   public void toggleIcon (bool show) {
      if (image == null) {
         return;
      }

      image.enabled = show;
   }

   public void setLabelColor (Color color) {
      if (label == null) {
         return;
      }

      label.color = color;
   }

   public void setIconLabelColor (Color color) {
      if (iconLabel == null) {
         return;
      }

      iconLabel.color = color;
   }

   public void setTooltip (string displayName) {
      tooltip.message = displayName;
   }

   #region Private Variables

   // The content of the label
   private string _name;

   #endregion

}
