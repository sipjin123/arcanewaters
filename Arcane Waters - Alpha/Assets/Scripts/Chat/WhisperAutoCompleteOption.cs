using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class WhisperAutoCompleteOption : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
   #region Public Variables

   #endregion

   // The color of UI elements in selected/unselected states
   public Color textColorInactive, textColorActive;

   // The text field that will show the name of this auto-complete
   public TextMeshProUGUI nameText;

   // An action  that will be performed when this option is clicked
   [HideInInspector]
   public System.Action<int> onClickedAction;

   // An action that will be performed when this option is selected
   public System.Action<int> onSelectedAction;

   // What the index of this option is in the list of auto-completes
   [HideInInspector]
   public int indexInList = 0;

   // A reference to the button component on this object
   [HideInInspector]
   public Button button;

   private void Awake () {
      nameText.text = "";
      button = GetComponent<Button>();
   }

   public void updateOption (string newName) {
      _name = newName;
      updateColors();
   }

   public void onClicked () {
      onClickedAction?.Invoke(indexInList);
   }

   public void onSelected () {
      _isSelected = true;
      updateColors();
   }

   public void onDeselected () {
      _isSelected = false;
      updateColors();
   }

   private void updateColors () {
      Color textColor = (_isSelected) ? textColorActive : textColorInactive;
      string colorString = "#" + ColorUtility.ToHtmlStringRGBA(textColor);
      nameText.text = string.Format("<color={0}>{1}</color>", colorString, _name);
   }

   public string getText () {
      return _name;
   }

   public void OnPointerEnter (PointerEventData eventData) {
      // Don't trigger PointerEnter events if the mouse hasn't moved, to avoid triggering on objects enabled under the mouse
      if (MouseUtils.mousePosition == ChatManager.self.autoCompletePanel.lastMousePos) {
         return;
      }

      onSelected();
      onSelectedAction?.Invoke(indexInList);
   }

   public void OnPointerExit (PointerEventData eventData) {
      onDeselected();
   }

   #region Private Variables

   // Whether this autoComplete is currently selected, and should change its appearance to look selected 
   private bool _isSelected = false;

   private string _name;

   #endregion
}
