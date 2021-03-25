using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.InputSystem;

public class WhisperAutoCompletePanel : MonoBehaviour {
   #region Public Variables

   //  A reference to the gameobject that holds the scroll view for this auto-complete panel
   public GameObject scrollViewContainer;

   // A reference to the transform that holds the content for the auto-complete panel.
   public Transform contentArea;

   // Whether the chat input field has the user's focus
   [HideInInspector]
   public bool inputFieldFocused = false;

   #endregion

   private void Awake () {
      _optionPrefab = Resources.Load<GameObject>("Prefabs/Auto-completes/WhisperAutoCompleteOption");

      for (int i = 0; i < NUM_INITIAL_OPTIONS; i++) {
         addNewOption();
      }

      setActive(false);
   }

   private void Update () {
      if (!isActive()) {
         return;
      }

      if (KeyUtils.GetKeyDown(Key.Enter)) {
         onEnterPressed();
      }
   }

   private void onEnterPressed () {
      if (!inputFieldFocused && _anyButtonSelected) {
         string autoComplete = _autoCompleteOptions[_selectedAutoComplete].getText();
         ChatPanel.self.focusWhisperInputField();
         ChatPanel.self.nameInputField.text = autoComplete;
      }
   }

   private void updateSelectedButton () {
      WhisperAutoCompleteOption selectedOption = getSelectedAutoComplete();
      selectedOption?.button.Select();
      selectedOption?.onSelected();
      _anyButtonSelected = true;
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
         ChatManager.self.tryAutoCompleteWhisperName();
      } else {
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

   public void setAutoCompletes (List<string> autoCompleteNames) {
      _autoCompleteNames = autoCompleteNames;
      updateAutoCompletes();
   }

   public void updateAutoCompletes () {
      // If there are no auto-completes, disable all
      if (_autoCompleteNames == null || _autoCompleteNames.Count == 0) {
         scrollViewContainer.SetActive(false);
         InputManager.enableKey(Keyboard.current.upArrowKey);
         InputManager.enableKey(Keyboard.current.downArrowKey);
         return;
      }

      InputManager.disableKey(Keyboard.current.upArrowKey);
      InputManager.disableKey(Keyboard.current.downArrowKey);

      scrollViewContainer.SetActive(true);

      resizeAutoCompletes();

      int optionCount = 0;

      foreach (string name in _autoCompleteNames) {
         WhisperAutoCompleteOption option = _autoCompleteOptions[optionCount];
         option.gameObject.SetActive(true);
         option.updateOption(name);
         option.indexInList = optionCount;
         optionCount++;
      }

      // Disable any auto-completes not being used
      for (int i = getNumAutoCompletes(); i < _autoCompleteOptions.Count; i++) {
         _autoCompleteOptions[i].gameObject.SetActive(false);
      }
   }

   private void addNewOption () {
      WhisperAutoCompleteOption newOption = Instantiate(_optionPrefab, contentArea).GetComponent<WhisperAutoCompleteOption>();
      newOption.gameObject.SetActive(false);
      newOption.onClickedAction += optionClicked;
      newOption.onSelectedAction += optionSelected;
      _autoCompleteOptions.Add(newOption);
   }

   public void resizeAutoCompletes () {
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
      ChatPanel.self.focusWhisperInputField();
      ChatPanel.self.nameInputField.text = autoComplete;
   }

   public void optionSelected (int indexInList) {
      deselectOldOption();
      _selectedAutoComplete = indexInList;
      updateSelectedButton();
   }

   private void deselectOldOption () {
      WhisperAutoCompleteOption selectedOption = getSelectedAutoComplete();
      selectedOption?.onDeselected();
   }

   private int getNumAutoCompletes () {
      if (_autoCompleteNames == null || _autoCompleteNames.Count < 1) {
         return 0;
      }

      return _autoCompleteNames.Count;
   }

   private WhisperAutoCompleteOption getSelectedAutoComplete () {
      if (_selectedAutoComplete == -1 || _selectedAutoComplete >= _autoCompleteOptions.Count) {
         return null;
      } else {
         return _autoCompleteOptions[_selectedAutoComplete];
      }
   }

   #region Private Variables

   // A list of references to the auto-complete options for the whisper name input
   private List<WhisperAutoCompleteOption> _autoCompleteOptions = new List<WhisperAutoCompleteOption>();

   // A copy of the command data passed on to us last
   private List<string> _autoCompleteNames;

   // The index of the auto-complete option that is selected
   private int _selectedAutoComplete = -1;

   // A reference to the prefab for a whisper auto-complete option
   private GameObject _optionPrefab;

   // The maximum number of auto-completes to display
   private const int MAX_COMMANDS_VISIBLE = 10;

   // The number of options to create on startup
   private const int NUM_INITIAL_OPTIONS = 10;

   // Whether the player has their mouse over the auto-complete panel
   private bool _mouseOverPanel = false;

   // Whether we have a button selected currently
   private bool _anyButtonSelected = false;

   #endregion
}
