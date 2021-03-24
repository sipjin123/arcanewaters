using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class AutoCompleteOption : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
   #region Public Variables

   // The color of UI elements in selected/unselected states
   public Color commandColorInactive, commandColorActive, parameterColorInactive, parameterColorActive;

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
   [HideInInspector]
   public Button button;

   // The text field that will show the value of this autoComplete
   public TextMeshProUGUI autoCompleteText;

   // A reference to the layout element for our tooltip
   public LayoutElement tooltipLayout;

   // An auto-completed parameter for this auto-complete option to display
   public string autocompleteParameter = "";

   #endregion

   private void Awake () {
      autoCompleteText.text = "";
      rectTransform = GetComponent<RectTransform>();
      button = GetComponent<Button>();
      _tooltipPreferredWidth = tooltipLayout.preferredWidth;
   }

   public void updateOption (CommandData newCommand) {
      _commandData = newCommand;
      updateColors();
      tooltipText.text = _commandData.getDescription();
   }

   public void setTooltip (bool isEnabled) {
      tooltip.SetActive(isEnabled);

      // If the tooltip  will go off screen, stop  it from  going off screen
      if (isEnabled) {
         Rect screenRect = Util.rectTransformToScreenSpace(tooltipLayout.GetComponent<RectTransform>());
         float distanceToRightEdge = Screen.width - screenRect.xMin;
         if  (distanceToRightEdge < _tooltipPreferredWidth) {
            tooltipLayout.preferredWidth = distanceToRightEdge;
         } else {
            tooltipLayout.preferredWidth = _tooltipPreferredWidth;
         }
      }
   }

   private void OnDisable () {
      setTooltip(false);
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
      Color commandColor = (_isSelected) ? commandColorActive : commandColorInactive;
      Color parameterColor = (_isSelected) ? parameterColorActive : parameterColorInactive;
      string commandColorString = "#" + ColorUtility.ToHtmlStringRGBA(commandColor);
      string parameterColorString = "#" + ColorUtility.ToHtmlStringRGBA(parameterColor);

      if (autocompleteParameter == "") {
         autoCompleteText.text = string.Format("<color={0}>{1}:</color> <color={2}>{3}</color>", commandColorString, _commandData.getPrefix(), parameterColorString, _commandData.getParameters());
      } else {
         autoCompleteText.text = string.Format("<color={0}>{1}</color>", commandColorString, _commandData.getPrefix() + " " + autocompleteParameter);
      }
   }

   public string getText () {
      return _commandData.getPrefix() + " " + autocompleteParameter;
   }

   public void OnPointerEnter (UnityEngine.EventSystems.PointerEventData eventData) {
      // Don't trigger PointerEnter events if the mouse hasn't moved, to avoid triggering on objects enabled under the mouse
      if (Mouse.current.position.ReadValue() == ChatManager.self.autoCompletePanel.lastMousePos) {
         return;
      }

      setTooltip(true);
      onSelected();
      onSelectedAction?.Invoke(indexInList);
   }

   public void OnPointerExit (UnityEngine.EventSystems.PointerEventData eventData) {
      setTooltip(false);
      onDeselected();
   }

   #region Private Variables

   // Whether this autoComplete is currently selected, and should change its appearance to look selected 
   private bool _isSelected = false;

   // A reference to the command data that this autocomplete represents
   private CommandData _commandData;

   // The starting preferred width of a tooltip
   private float _tooltipPreferredWidth;

   #endregion
}
