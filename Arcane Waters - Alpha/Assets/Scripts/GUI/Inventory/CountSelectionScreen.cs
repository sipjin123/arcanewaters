using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class CountSelectionScreen : SubPanel
{
   #region Public Variables

   // The number selected
   public static int selectedCount = 1;

   // The slider allowing to set the number
   public Slider countSlider;

   // The text showing the count value
   public Text countText;

   // The confirm button
   public Button selectButton;

   // The cancel button
   public Button cancelButton;

   #endregion

   public void show (int value, int minValue, int maxValue, Func<int, string> descriptionTextHandler = null) {
      // Handle description text
      _descriptionTextHandler = descriptionTextHandler;
      foreach (GameObject ob in _descriptionRowObjects) {
         ob.SetActive(_descriptionTextHandler != null);
      }

      // Set the slider
      countSlider.minValue = minValue;
      countSlider.maxValue = maxValue;
      countSlider.SetValueWithoutNotify(value);
      selectedCount = value;

      // This is a small hack to make sure the slider's handle is on the right side when there's only 1 item available
      countSlider.direction = countSlider.minValue == countSlider.maxValue
         ? Slider.Direction.RightToLeft
         : Slider.Direction.LeftToRight;

      updateSelectedItemCount();

      // Make the panel visible
      base.show();
   }

   public void updateSelectedItemCount () {
      selectedCount = (int) countSlider.value;
      countText.text = selectedCount.ToString();

      if (_descriptionTextHandler != null) {
         _descriptionText.text = _descriptionTextHandler(selectedCount);
      }
   }

   #region Private Variables

   // Items we activate for description row
   [SerializeField]
   private List<GameObject> _descriptionRowObjects = new List<GameObject>();

   // Description text
   [SerializeField]
   private Text _descriptionText = null;

   // Logic for updating description text
   private Func<int, string> _descriptionTextHandler = null;

   #endregion
}
