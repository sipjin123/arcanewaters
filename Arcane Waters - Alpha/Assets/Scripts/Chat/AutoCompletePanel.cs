using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class AutoCompletePanel : MonoBehaviour {
   #region Public Variables

   //  A reference to the gameobject that holds the scroll view for this auto-complete panel
   public GameObject scrollViewContainer;

   // A reference to the transform that holds the content for the auto-complete panel.
   public Transform contentArea;

   // A reference to the recttransform of the viewport for the autcomplete panel's scroll view
   public RectTransform viewport;

   // Whether the chat input field has the user's focus
   public bool inputFieldFocused = false;

   // Where the mouse was last frame
   public Vector3 lastMousePos = Vector3.zero;

   #endregion

   private void Awake () {
      _scrollRect = GetComponentInChildren<ScrollRect>();
      _optionPrefab = Resources.Load<GameObject>("Prefabs/Auto-completes/AutoCompleteOption");
      _scrollRect.onValueChanged.AddListener(onScrolled);

      for (int i = 0; i < NUM_INITIAL_OPTIONS; i++) {
         addNewOption();
      }

      setActive(false);
   }

   private void Update () {
      if (!isActive()) {
         return;
      }

      if (Input.GetKeyDown(KeyCode.Return)) {
         onEnterPressed();
      }
   }

   private void LateUpdate () {
      lastMousePos = Input.mousePosition;
   }

   private void onScrolled (Vector2 pos) {
      AutoCompleteOption selectedOption = getSelectedAutoComplete();
      if (!selectedOption) {
         return;
      }

      if (isSelectedOptionVisible() && !inputFieldFocused) {
         selectedOption.setTooltip(true);
      }  else {
         selectedOption.setTooltip(false);
      }
   }

   private bool isSelectedOptionVisible () {
      AutoCompleteOption selectedOption = getSelectedAutoComplete();

      if (selectedOption) {
         Rect viewportRect = Util.rectTransformToScreenSpace(viewport);
         Rect optionRect = Util.rectTransformToScreenSpace(selectedOption.rectTransform);
         Vector2 topMiddle = optionRect.center + Vector2.up * optionRect.height / 2.0f;
         Vector2 bottomMiddle = optionRect.center - Vector2.up * optionRect.height / 2.0f;
         if (viewportRect.Contains(topMiddle) && viewportRect.Contains(bottomMiddle)) {
            return true;
         } else {
            return false;
         }
      } else {
         return false;
      }
   }

   private void onDownPressed () {
      if (!isActive()) {
         return;
      }

      if (!inputFieldFocused) {
         deselectOldOption();
         _selectedAutoComplete = (_selectedAutoComplete + 1);
      } else if (inputFieldFocused) {
         _selectedAutoComplete = 0;
      }

      updateSelectedButton();
      _scrollRect.verticalNormalizedPosition = Util.getNormalisedScrollValue(_selectedAutoComplete, getNumAutoCompletes());
   }

   private void onUpPressed () {
      if (!isActive()) {
         return;
      }

      if (!inputFieldFocused) {
         deselectOldOption();
         _selectedAutoComplete = _selectedAutoComplete - 1;
      } else if (inputFieldFocused) {
         _selectedAutoComplete = getNumAutoCompletes() - 1;
      }

      updateSelectedButton();
      _scrollRect.verticalNormalizedPosition = Util.getNormalisedScrollValue(_selectedAutoComplete, getNumAutoCompletes());
   }

   private void onEnterPressed () {
      if (!inputFieldFocused && _anyButtonSelected) {
         string autoComplete = _autoCompleteOptions[_selectedAutoComplete].getText();
         ChatPanel.self.focusInputField();
         ChatPanel.self.inputField.text = autoComplete;
      }
   }

   private void updateSelectedButton () {
      if (_selectedAutoComplete >= getNumAutoCompletes()) {
         ChatPanel.self.inputField.Select();
         StartCoroutine(ChatPanel.self.CO_MoveCaretToEnd());
         _anyButtonSelected = false;
      } else if (_selectedAutoComplete < 0) {
         ChatPanel.self.inputField.Select();
         StartCoroutine(ChatPanel.self.CO_MoveCaretToEnd());
         _anyButtonSelected = false;
      } else {
         AutoCompleteOption selectedOption = getSelectedAutoComplete();
         selectedOption?.button.Select();
         selectedOption?.setTooltip(true); ;
         selectedOption?.onSelected();
         _anyButtonSelected = true;
      }
   }

   public void onMouseEnterPanel () {
      _mouseOverPanel = true;
      updatePanel();
   }

   public void onMouseExitPanel () {
      _mouseOverPanel = false;
      updatePanel();
   }

   public void updatePanel () {
      if (inputFieldFocused || _mouseOverPanel) {
         ChatManager.self.tryAutoCompleteChatCommand();
      } else if (!_anyButtonSelected) {
         setAutoCompletes(null);
      }

      if (inputFieldFocused && !_mouseOverPanel) {
         deselectOldOption();
      }

   }

   public bool isActive () {
      return scrollViewContainer.activeInHierarchy;
   }

   public void setActive (bool isActive) {
      scrollViewContainer.SetActive(isActive);
   }

   public void setAutoCompletes (List<CommandData> autoCompleteCommands) {
      _autoCompleteCommands = autoCompleteCommands;
      updateAutoCompletes();
   }

   public void setAutoCompletesWithParameters (List<Tuple<CommandData, string>> autoCompletes) {
      _autoCompleteCommandsWithParameters = autoCompletes;
      updateAutoCompletes();
   }

   public void updateAutoCompletes () {
      // If there are no auto-completes, disable all
      if (_autoCompleteCommands == null || _autoCompleteCommands.Count == 0) {
         scrollViewContainer.SetActive(false);
         InputManager.enableKey(KeyCode.UpArrow);
         InputManager.enableKey(KeyCode.DownArrow);
         return;
      }

      InputManager.disableKey(KeyCode.UpArrow);
      InputManager.disableKey(KeyCode.DownArrow);

      scrollViewContainer.SetActive(true);

      resizeAutocompletes();

      int optionCount = 0;

      foreach (CommandData data in _autoCompleteCommands) {
         AutoCompleteOption option = _autoCompleteOptions[optionCount];
         option.autocompleteParameter = "";
         option.gameObject.SetActive(true);
         option.updateOption(data);
         option.indexInList = optionCount;
         optionCount++;
      }

      foreach (Tuple<CommandData, string> parameterData in _autoCompleteCommandsWithParameters) {
         AutoCompleteOption option = _autoCompleteOptions[optionCount];
         option.autocompleteParameter = parameterData.Item2;
         option.gameObject.SetActive(true);
         option.updateOption(parameterData.Item1);
         option.indexInList = optionCount;
         optionCount++;
      }

      // Disable any auto-completes not being used
      for (int i = getNumAutoCompletes(); i < _autoCompleteOptions.Count; i++) {
         _autoCompleteOptions[i].gameObject.SetActive(false);
      }
   }

   private void addNewOption () {
      AutoCompleteOption newOption = Instantiate(_optionPrefab, contentArea).GetComponent<AutoCompleteOption>();
      newOption.gameObject.SetActive(false);
      newOption.onClickedAction += optionClicked;
      newOption.onSelectedAction += optionSelected;
      _autoCompleteOptions.Add(newOption);
   }

   public void resizeAutocompletes () {
      // Find out how many new auto-completes we need
      int newAutoCompletesNeeded = getNumAutoCompletes() - _autoCompleteOptions.Count;

      // We have too many options, and need to disable some
      if (newAutoCompletesNeeded < 0) {
         for (int i = _autoCompleteOptions.Count - 1; i >= Mathf.Abs(newAutoCompletesNeeded); i--) {
            _autoCompleteOptions[i].gameObject.SetActive(false);
         }
         // We don't have enough options, and need to create more
      } else if (newAutoCompletesNeeded > 0) {
         for (int i = 0; i < newAutoCompletesNeeded; i++) {
            addNewOption();
         }
      }
   }

   public void optionClicked (int indexInList) {
      string autoComplete = _autoCompleteOptions[indexInList].getText();
      ChatPanel.self.focusInputField();
      ChatPanel.self.inputField.text = autoComplete;
   }

   public void optionSelected (int indexInList) {
      deselectOldOption();
      _selectedAutoComplete = indexInList;
      updateSelectedButton();
   }

   private void deselectOldOption () {
      AutoCompleteOption selectedOption = getSelectedAutoComplete();
      selectedOption?.onDeselected();
      selectedOption?.setTooltip(false);
   }
   
   private int getNumAutoCompletes () {
      if (_autoCompleteCommands == null || _autoCompleteCommands.Count < 1) {
         return 0;
      }

      if (_autoCompleteCommandsWithParameters == null || _autoCompleteCommandsWithParameters.Count < 1) {
         return _autoCompleteCommands.Count;
      }

      return _autoCompleteCommands.Count + _autoCompleteCommandsWithParameters.Count;
   }

   private AutoCompleteOption getSelectedAutoComplete () {
      if (_selectedAutoComplete == -1 || _selectedAutoComplete >= _autoCompleteOptions.Count) {
         return null;
      } else {
         return _autoCompleteOptions[_selectedAutoComplete];
      }
   }

   #region Private Variables

   // A list of references to the auto-complete options for the chat
   private List<AutoCompleteOption> _autoCompleteOptions = new List<AutoCompleteOption>();

   // A copy of the command data passed on to us last
   private List<CommandData> _autoCompleteCommands;

   // A copy of the auto-complete parameter command data pairs passed on to us last
   private List<Tuple<CommandData, string>> _autoCompleteCommandsWithParameters = new List<Tuple<CommandData, string>>();

   // The index of the auto-complete option that is selected
   private int _selectedAutoComplete = -1;

   // A reference to the prefab for an auto-complete option
   private GameObject _optionPrefab;

   // The maximum number of auto-completes to display
   private const int MAX_COMMANDS_VISIBLE = 10;

   // The amount of vertical space between each auto-complete option
   private const int AUTOCOMPLETE_VERTICAL_SPACING = 34;

   // The number of options to create on startup
   private const int NUM_INITIAL_OPTIONS = 10;

   // A reference to the scroll rect for the auto-complete panel
   private ScrollRect _scrollRect;

   // Whether the player has their mouse over the autocomplete panel
   private bool _mouseOverPanel = false;

   // Whether we have a button selected currently
   private bool _anyButtonSelected = false;

   #endregion
}
