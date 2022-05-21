using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class UINavigationController : MonoBehaviour
{
   public CanvasGroup parentCanvasGroup;
   public GameObject[] items;
   public Button actionButton;

   #region Public Variables
   public bool isLocked;
   #endregion

   private void Start () {
      if (!parentCanvasGroup) {
         parentCanvasGroup = GetComponent<CanvasGroup>();
      }

      ResetItems();
   }

   private void ResetItems () {
      _currItemId = 0;

      // Init items data
      _itemsData = new List<ItemData>();
      foreach (GameObject item in items) {
         _itemsData.Add(new ItemData(item));
      }

      // Reset items selection
      foreach (var itemData in _itemsData) {
         itemData.deselectItem();
      }

      // Set default selection
      updateSelection(true);
   }

   private void Update () {
      // Skip for batch mode
      if (Util.isBatch()) return;

      // Skip update when chat is focused
      if (ChatManager.self.chatPanel.inputField.isFocused) return;

      // Check parentCanvasGroup status
      if (parentCanvasGroup) {
         if (parentCanvasGroup.interactable && !_canvasGroupInteractable) {
            OnEnable();
         }
         if (!parentCanvasGroup.interactable && _canvasGroupInteractable) {
            OnDisable();
         }

         _canvasGroupInteractable = parentCanvasGroup.interactable;
      }

      // Don't tick update when disabled (controlled by UINavigationManager), or parentCanvasGroup is not interactable
      if (!enabled || isLocked || (parentCanvasGroup && !parentCanvasGroup.interactable)) return;

      // Skip tick update if InputManager is not initialized yet 
      if (!InputManager.self) return;

      // Enable UIControl input group if its disabled
      if (!InputManager.self.inputMaster.UIControl.enabled) {
         InputManager.self.inputMaster.UIControl.Enable();
      }

      // Check if input field becomes selected after mouse click for ex.
      for (int id = 0; id < _itemsData.Count; id++) {
         if (_itemsData[id].isInputField && _itemsData[id].inputField.isFocused) {
            changeSelection(id);
         }
      }

      // Enter
      if (
         Keyboard.current.enterKey.wasPressedThisFrame &&
         !PanelManager.self.noticeScreen.isActive &&
         actionButton != null &&
         !_itemsData[_currItemId].isButton
      ) {
         if (actionButton.interactable && actionButton.gameObject.activeInHierarchy) {
            actionButton.onClick.Invoke();
         }
      }

      // Down
      if (
         InputManager.self.inputMaster.UIControl.MoveDown.WasPressedThisFrame() ||
         Keyboard.current.tabKey.wasPressedThisFrame
      ) {
         _isNavVisible = true;

         // If not input field and sKey pressed
         if (!(_itemsData[_currItemId].isInputField && Keyboard.current.sKey.wasPressedThisFrame)) {

            // Is dropdown open - delegate action
            if (_itemsData[_currItemId].isDropdownOpen) {
               _itemsData[_currItemId].downAction();
            }
            // otherwise - select next item
            else {
               changeSelection(_currItemId + 1, 1);
            }
         }
      }

      // Up
      if (InputManager.self.inputMaster.UIControl.MoveUp.WasPressedThisFrame()) {
         _isNavVisible = true;

         // If not input field and wKey pressed
         if (!(_itemsData[_currItemId].isInputField && Keyboard.current.wKey.wasPressedThisFrame)) {

            // Is dropdown open - delegate action
            if (_itemsData[_currItemId].isDropdownOpen) {
               _itemsData[_currItemId].upAction();
            }
            // otherwise - select prev item
            else {
               changeSelection(_currItemId - 1, -1);
            }
         }
      }

      // Left
      if (InputManager.self.inputMaster.UIControl.MoveLeft.WasPressedThisFrame()) {
         _isNavVisible = true;

         _itemsData[_currItemId].leftAction();
      }

      // Right
      if (InputManager.self.inputMaster.UIControl.MoveRight.WasPressedThisFrame()) {
         _isNavVisible = true;

         _itemsData[_currItemId].rightAction();
      }

      // Continue
      if (InputManager.self.inputMaster.General.Continue.WasPressedThisFrame()) {
         if (_itemsData[_currItemId].isActive) {
            _itemsData[_currItemId].interactItem();
         }
      }

      // Equip
      if (InputManager.self.inputMaster.UIControl.Equip.WasPressedThisFrame()) {
         if (_itemsData[_currItemId].isActive) {
            _itemsData[_currItemId].equipItem();
         }
      }

      // Use
      if (InputManager.self.inputMaster.UIControl.Use.WasPressedThisFrame()) {
         if (_itemsData[_currItemId].isActive) {
            _itemsData[_currItemId].useItem();
         }
      }

      // Space
      if (Keyboard.current.spaceKey.wasPressedThisFrame) {
         if (_itemsData[_currItemId].isActive) {
            _itemsData[_currItemId].interactItem();
         }
      }
   }

   private void changeSelection (int newItemId, int direction = 1) {
      bool isAnyItemActive = false;
      foreach (var item in _itemsData) {
         if (item.isActive) {
            isAnyItemActive = true;
            break;
         }
      }

      if (!isAnyItemActive) {
         return;
      }

      // If new item is not active - find active one
      if (!_itemsData[Mathf.Clamp(newItemId, 0, _itemsData.Count - 1)].isActive) {
         // If moving to the next item and new id is the last one - move to prev active item 
         if (direction == 1 && newItemId >= _itemsData.Count - 1) {
            changeSelection(newItemId - 1, -1);
            return;
         }
         // If moving to the prev item and new id is the first one - move fo next active item 
         if (direction == -1 && newItemId <= 0) {
            changeSelection(newItemId + 1, 1);
            return;
         }

         // Otherwise - continue
         changeSelection(newItemId + direction, direction);
         return;
      }

      // Process normal way
      _prevItemId = _currItemId;
      _currItemId = Mathf.Clamp(newItemId, 0, _itemsData.Count - 1);
      updateSelection();
   }

   public void updateSelection (bool force = false) {
      if (_prevItemId == _currItemId && !force) {
         return;
      }

      if (_itemsData != null && _prevItemId < _itemsData.Count) {
         _itemsData[_prevItemId].deselectItem();
      }

      if (_itemsData != null && _currItemId < _itemsData.Count) {
         if (force && !_isNavVisible)
            _itemsData[_currItemId].deselectItem();
         else
            _itemsData[_currItemId].selectItem();
      }
   }

   private void OnEnable () {
      UINavigationManager.self.ControllerEnabled(this);
      _isNavVisible = InputManager.self.isGamepadConnected;
      ResetItems();
   }

   private void OnDisable () {
      UINavigationManager.self.ControllerDisabled(this);
      InputManager.self.inputMaster.UIControl.Disable();
   }

   private class ItemData
   {
      #region Public Variables
      public readonly InputField inputField;
      public readonly Button button;
      public readonly Toggle toggle;
      public readonly Slider slider;
      public readonly Dropdown dropdown;
      public readonly UINavigationItem navigationItem;
      #endregion

      public ItemData (GameObject item) {
         _item = item;
         inputField = item.GetComponent<InputField>();
         button = item.GetComponent<Button>();
         toggle = item.GetComponent<Toggle>();
         slider = item.GetComponent<Slider>();
         dropdown = item.GetComponent<Dropdown>();
         navigationItem = item.GetComponent<UINavigationItem>();
         isDropdownOpen = false;
      }

      public bool isActive
      {
         get
         {
            return
               _item.activeSelf &&
               _item.activeInHierarchy &&
               (isButton && button.interactable || !isButton)
               ;
         }
      }
      public bool isInputField { get { return inputField != null; } }
      public bool isButton { get { return button != null; } }
      public bool isToggle { get { return toggle != null; } }
      public bool isSlider { get { return slider != null; } }
      public bool isDropdown { get { return dropdown != null; } }
      public bool isNavigationItem { get { return navigationItem != null; } }

      public bool isDropdownOpen { get; private set; }

      public void selectItem () {
         inputField?.Select();
         button?.Select();
         toggle?.Select();
         slider?.Select();
         dropdown?.Select();
         navigationItem?.Select();
      }

      public void deselectItem () {
         if (isDropdown) {
            isDropdownOpen = false;
            dropdown.Hide();
         }
         navigationItem?.Deselect();
      }

      public void equipItem () {
         navigationItem?.Equip();
      }

      public void useItem () {
         navigationItem?.Use();
      }

      public void interactItem () {
         if (isButton && button.interactable && button.gameObject.activeInHierarchy) {
            button.onClick.Invoke();
         }

         if (isToggle && toggle.gameObject.activeInHierarchy) {
            toggle.isOn = !toggle.isOn;
         }

         if (isDropdown && dropdown.gameObject.activeInHierarchy) {
            if (isDropdownOpen) {
               isDropdownOpen = false;
               dropdown.Hide();
            } else {
               isDropdownOpen = true;
               dropdown.Show();
            }
         }

         navigationItem?.Interact();
      }

      public void leftAction () {
         if (isSlider) {
            var step = slider.wholeNumbers && slider.maxValue - slider.minValue < 30
               ? 1
               : (slider.maxValue - slider.minValue) / 10f;
            slider.value = Mathf.Clamp(slider.value - step, slider.minValue, slider.maxValue);
         }
      }

      public void rightAction () {
         if (isSlider) {
            var step = slider.wholeNumbers && slider.maxValue - slider.minValue < 30
               ? 1
               : (slider.maxValue - slider.minValue) / 10f;
            slider.value = Mathf.Clamp(slider.value + step, slider.minValue, slider.maxValue);
         }
      }

      public void downAction () {
         if (isDropdown && isDropdownOpen) {
            dropdown.value = Mathf.Clamp(dropdown.value + 1, 0, dropdown.options.Count);
            dropdown.RefreshShownValue();
         }
      }

      public void upAction () {
         if (isDropdown && isDropdownOpen) {
            dropdown.value = Mathf.Clamp(dropdown.value - 1, 0, dropdown.options.Count);
            dropdown.RefreshShownValue();
         }
      }

      #region Private Variables
      private GameObject _item;
      #endregion
   }

   #region Private Variables
   private List<ItemData> _itemsData;
   private int _prevItemId;
   private int _currItemId;
   private bool _canvasGroupInteractable;
   private bool _isNavVisible;
   #endregion
}
