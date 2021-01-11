using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AutoCompletePanel : MonoBehaviour {
   #region Public Variables

   #endregion

   private void Awake () {
      Canvas canvas = gameObject.AddComponent<Canvas>();
      canvas.overrideSorting = true;
      canvas.sortingOrder = 5;

      GameObject optionPrefab = Resources.Load<GameObject>("Prefabs/AutoCompleteOption");

      // Create and position auto-complete options
      for (int i = 0; i < MAX_COMMANDS_VISIBLE; i++) {
         AutoCompleteOption newOption = Instantiate(optionPrefab, this.transform).GetComponent<AutoCompleteOption>();

         // Adjust height based on which option this is
         Vector2 pos = newOption.rectTransform.anchoredPosition;
         pos.y += (i + 1) * AUTOCOMPLETE_VERTICAL_SPACING;
         newOption.rectTransform.anchoredPosition = pos;

         newOption.gameObject.SetActive(false);
         _autoCompleteOptions.Add(newOption);
      }
   }

   public bool isActive () {
      return _autoCompleteOptions[0].isActiveAndEnabled;
   }

   public void setAutoCompletes (List<string> autoCompletes) {
      _autoCompletes = autoCompletes;
      updateAutoCompletes();
   }

   public void updateAutoCompletes () {
      // If there are no auto-completes, disable all
      if (_autoCompletes == null || _autoCompletes.Count == 0) {
         foreach(AutoCompleteOption option in _autoCompleteOptions) {
            option.gameObject.SetActive(false);
         }
         return;
      }

      int autoCompletesToDisplay = Mathf.Clamp(_autoCompletes.Count, 0, MAX_COMMANDS_VISIBLE);

      // Enable and setup the auto-completes that are needed
      for (int i = 0; i < autoCompletesToDisplay; i++) {
         AutoCompleteOption option = _autoCompleteOptions[i];
         option.gameObject.SetActive(true);
         option.setText(_autoCompletes[i + _lowestVisibleIndex]);
         option.setSelected(false);
      }

      // Disable any auto-completes not being used
      for (int i = autoCompletesToDisplay; i < MAX_COMMANDS_VISIBLE; i++) {
         _autoCompleteOptions[i].gameObject.SetActive(false);
      }

      setSelectedOption(_selectedAutoComplete);
   }

   public string getSelectedCommand () {
      if (_autoCompleteOptions[_selectedAutoComplete]) {
         return _autoCompleteOptions[_selectedAutoComplete].getText();
      } else {
         return "";
      }
   }

   public void moveSelection (bool moveUp) {
      // If we are at the top of the screen, scroll up
      if (moveUp && _selectedAutoComplete == MAX_COMMANDS_VISIBLE - 1) {
         int highestPoint = Mathf.Clamp(_autoCompletes.Count - (MAX_COMMANDS_VISIBLE - 1), 0, int.MaxValue);

         if (highestPoint == 0) {
            _lowestVisibleIndex = 0;
         } else {
            _lowestVisibleIndex = (_lowestVisibleIndex + 1) % highestPoint;
         }
         
         // If we have wrapped around to the bottom, select the bottom option
         if (_lowestVisibleIndex == 0) {
            setSelectedOption(0);
         } else {
            setSelectedOption(_selectedAutoComplete);
         }

         // If we are at the bottom of the screen, scroll down
      } else if (!moveUp && _selectedAutoComplete == 0) {
         _lowestVisibleIndex = (_lowestVisibleIndex - 1);
         // If we were at the very bottom, scroll to the top
         if (_lowestVisibleIndex < 0) {
            _lowestVisibleIndex = Mathf.Clamp(_autoCompletes.Count - MAX_COMMANDS_VISIBLE, 0, int.MaxValue);
            int autoCompletesToDisplay = Mathf.Clamp(_autoCompletes.Count, 0, MAX_COMMANDS_VISIBLE);
            setSelectedOption(autoCompletesToDisplay - 1);
            
            // Otherwise, move the view down normally
         } else {
            setSelectedOption(0);
         }
         // Otherwise, just move the selection
      } else {
         int move = (moveUp) ? 1 : -1;
         setSelectedOption(_selectedAutoComplete = (_selectedAutoComplete + move) % _autoCompletes.Count);
      }

      updateAutoCompletes();
   }

   public void resetSelection () {
      setSelectedOption(0);
      _lowestVisibleIndex = 0;
   }

   public void setSelectedOption (int selectedIndex) {
      if (_autoCompleteOptions[_selectedAutoComplete]) {
         _autoCompleteOptions[_selectedAutoComplete].setSelected(false);
      }

      if (_autoCompleteOptions[selectedIndex]) {
         _selectedAutoComplete = selectedIndex;
         _autoCompleteOptions[selectedIndex].setSelected(true);
      }
   }

   #region Private Variables

   // A list of references to the auto-complete options for the chat
   private List<AutoCompleteOption> _autoCompleteOptions = new List<AutoCompleteOption>();

   // A copy of the auto-completes passed to us last
   private List<string> _autoCompletes;

   // The index of the auto-complete option that is selected
   private int _selectedAutoComplete = 0;

   // The index of the lowest visible auto-complete option, to keep track of our list scrolling
   private int _lowestVisibleIndex = 0;

   // The maximum number of auto-completes to display
   private const int MAX_COMMANDS_VISIBLE = 10;

   // The amount of vertical space between each auto-complete option
   private const int AUTOCOMPLETE_VERTICAL_SPACING = 34;

   #endregion
}
