using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using UnityEngine.EventSystems;

public class AutoCompleteOption : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
   #region Public Variables

   // The color of UI elements in selected/unselected states
   public Color backgroundUnselected, backgroundSelected, textUnselected, textSelected;

   // A reference to the rect transform of this object
   [HideInInspector]
   public RectTransform rectTransform;

   // An action  that will be performed when this option is clicked
   [HideInInspector]
   public System.Action<int> onClickedAction;

   // An action that will be performed when this option is selected
   public System.Action<int> onSelectedAction;

   // What the index of this option is in the list of auto-completes
   [HideInInspector]
   public int indexInList = 0;

   // A reference to the text container for the tooltip
   public TextMeshProUGUI tooltipText;

   // A reference to the tooltip object
   public GameObject tooltip;

   // A reference to the button component on this object
   public Button button;

   #endregion

   private void Awake () {
      _autoCompleteText = GetComponentInChildren<Text>();
      _autoCompleteText.text = "";
      _background = GetComponent<Image>();
      rectTransform = GetComponent<RectTransform>();
      button = GetComponent<Button>();
   }

   public void updateOption (CommandData newCommand) {
      _commandData = newCommand;

      setText(_commandData.getCommandInfo());
      tooltipText.text = _commandData.getDescription();
   }

   private void OnDisable () {
      tooltip.SetActive(false);
   }

   public void onClicked () {
      onClickedAction?.Invoke(indexInList);
   }

   public void onSelected () {
      onSelectedAction?.Invoke(indexInList);
   }

   public void setSelected (bool selected) {
      _isSelected = selected;

      _autoCompleteText.color = (_isSelected) ? textSelected : textUnselected;
      _background.color = (_isSelected) ? backgroundSelected : backgroundUnselected;
   }

   private void setText (string newText) {
      _autoCompleteText.text = newText;
   }

   public string getText () {
      return _commandData.getPrefix();
   }

   public void OnPointerEnter (UnityEngine.EventSystems.PointerEventData eventData) {
      tooltip.SetActive(true);
      onSelected();
   }

   public void OnPointerExit (UnityEngine.EventSystems.PointerEventData eventData) {
      tooltip.SetActive(false);
   }

   #region Private Variables

   // The text field that will show the value of this autoComplete
   private Text _autoCompleteText;

   // The image that is the background for this autoComplete
   private Image _background;

   // Whether this autoComplete is currently selected, and should change its appearance to look selected 
   private bool _isSelected = false;

   // A reference to the command data that this autocomplete represents
   private CommandData _commandData;

   #endregion
}
