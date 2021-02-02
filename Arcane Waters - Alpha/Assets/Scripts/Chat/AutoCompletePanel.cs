using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

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

   public int NumAutoCompletes {
      get {
         if (_autoCompleteCommands == null || _autoCompleteCommands.Count < 1) {
            return 0;
         }
         return _autoCompleteCommands.Count;
      }
   }

   private AutoCompleteOption SelectedAutoComplete {
      get
      {
         if (_selectedAutoComplete == -1 || _selectedAutoComplete >= _autoCompleteOptions.Count) {
            return null;
         } else {
            return _autoCompleteOptions[_selectedAutoComplete];
         }
      }
   }

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

      if (Input.GetKeyDown(KeyCode.DownArrow)) {
         onDownPressed();
      }

      if (Input.GetKeyDown(KeyCode.UpArrow)) {
         onUpPressed();
      }

      if (Input.GetKeyDown(KeyCode.Return)) {
         onEnterPressed();
      }
   }

   private void LateUpdate () {
      lastMousePos = Util.getMousePos();
   }

   private void onScrolled (Vector2 pos) {
      if (!SelectedAutoComplete) {
         return;
      }

      if (isSelectedOptionVisible() && !inputFieldFocused) {
         SelectedAutoComplete.setTooltip(true);
      }  else {
         SelectedAutoComplete.setTooltip(false);
      }
   }

   private bool isSelectedOptionVisible () {
      if (SelectedAutoComplete) {
         Rect viewportRect = Util.rectTransformToScreenSpace(viewport);
         Rect optionRect = Util.rectTransformToScreenSpace(SelectedAutoComplete.rectTransform);
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
      _scrollRect.verticalNormalizedPosition = Util.getNormalisedScrollValue(_selectedAutoComplete, NumAutoCompletes);
   }

   private void onUpPressed () {
      if (!isActive()) {
         return;
      }

      if (!inputFieldFocused) {
         deselectOldOption();
         _selectedAutoComplete = _selectedAutoComplete - 1;
      } else if (inputFieldFocused) {
         _selectedAutoComplete = NumAutoCompletes - 1;
      }

      updateSelectedButton();
      _scrollRect.verticalNormalizedPosition = Util.getNormalisedScrollValue(_selectedAutoComplete, NumAutoCompletes);
   }

   private void onEnterPressed () {
      if (!inputFieldFocused && _anyButtonSelected) {
         string autoComplete = _autoCompleteOptions[_selectedAutoComplete].getText();
         ChatPanel.self.focusInputField();
         ChatPanel.self.inputField.text = autoComplete;
      }
   }

   private void updateSelectedButton () {
      if (_selectedAutoComplete >= NumAutoCompletes) {
         ChatPanel.self.inputField.Select();
         StartCoroutine(ChatPanel.self.CO_MoveCaretToEnd());
         _anyButtonSelected = false;
      } else if (_selectedAutoComplete < 0) {
         ChatPanel.self.inputField.Select();
         StartCoroutine(ChatPanel.self.CO_MoveCaretToEnd());
         _anyButtonSelected = false;
      } else {
         SelectedAutoComplete?.button.Select();
         SelectedAutoComplete?.setTooltip(true); ;
         SelectedAutoComplete?.onSelected();
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

      // Enable and set up the auto-completes that are needed
      for (int i = 0; i < _autoCompleteCommands.Count; i++) {
         AutoCompleteOption option = _autoCompleteOptions[i];
         option.gameObject.SetActive(true);
         // option.setText(_autoCompletes[i]);
         option.updateOption(_autoCompleteCommands[i]);
         option.indexInList = i;
      }

      // Disable any auto-completes not being used
      for (int i = _autoCompleteCommands.Count; i < _autoCompleteOptions.Count; i++) {
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
      int newAutoCompletesNeeded = _autoCompleteCommands.Count - _autoCompleteOptions.Count;

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
      SelectedAutoComplete?.onDeselected();
      SelectedAutoComplete?.setTooltip(false);
   }

   #region Private Variables

   // A list of references to the auto-complete options for the chat
   private List<AutoCompleteOption> _autoCompleteOptions = new List<AutoCompleteOption>();

   // A copy of the command  data passed on to  us last
   private List<CommandData> _autoCompleteCommands;

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
