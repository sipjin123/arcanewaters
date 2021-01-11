using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AutoCompleteOption : MonoBehaviour {
   #region Public Variables

   // The color of UI elements in selected/unselected states
   public Color backgroundUnselected, backgroundSelected, textUnselected, textSelected;

   // A reference to the rect transform of this object
   [HideInInspector]
   public RectTransform rectTransform;

   #endregion

   private void Awake () {
      _autoCompleteText = GetComponentInChildren<Text>();
      _background = GetComponent<Image>();
      rectTransform = GetComponent<RectTransform>();
   }

   public void setSelected (bool selected) {
      _isSelected = selected;

      _autoCompleteText.color = (_isSelected) ? textSelected : textUnselected;
      _background.color = (_isSelected) ? backgroundSelected : backgroundUnselected;
   }

   public void setText (string newText) {
      _autoCompleteText.text = newText;
   }

   public string getText () {
      return _autoCompleteText.text;
   }

   #region Private Variables

   // The text field that will show the value of this autoComplete
   private Text _autoCompleteText;

   // The image that is the background for this autoComplete
   private Image _background;

   // Whether this autoComplete is currently selected, and should change its appearance to look selected 
   private bool _isSelected = false;

   #endregion
}
