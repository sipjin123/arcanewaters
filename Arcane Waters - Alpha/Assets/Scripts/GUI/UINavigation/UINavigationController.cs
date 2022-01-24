using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class UINavigationController : MonoBehaviour {
   public GameObject[] items;
   public Button actionButton;

   #region Public Variables
   public bool isLocked;
   #endregion

   private void Awake () {
      // Init items data
      _itemsData = new List<ItemData>();
      foreach (GameObject item in items) {
         if (item.activeSelf) {
            _itemsData.Add(new ItemData(item));
         }
      }
   }

   private void Start () {
      updateSelection(true);
   }

   private void Update () {
      // Don't tick update when disabled (controlled by UINavigationManager)
      if (!enabled || isLocked) return;
      
      // Enable UIControl input group if its disabled
      if (!InputManager.self.inputMaster.UIControl.enabled) {
         InputManager.self.inputMaster.UIControl.Enable();
      }
      
      // Check if input field becomes selected after mouse click for ex.
      for (int id=0; id<_itemsData.Count; id++) {
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
         if (actionButton.interactable) {
            actionButton.onClick.Invoke();
         }
      }

      // Down
      if (
         InputManager.self.inputMaster.UIControl.MoveDown.WasPressedThisFrame() ||
         Keyboard.current.tabKey.wasPressedThisFrame
      ) {
         if (!(_itemsData[_currItemId].isInputField && Keyboard.current.sKey.wasPressedThisFrame)) {
            changeSelection(_currItemId + 1);
         }
      }

      // Up
      if (InputManager.self.inputMaster.UIControl.MoveUp.WasPressedThisFrame()) {
         if (!(_itemsData[_currItemId].isInputField && Keyboard.current.wKey.wasPressedThisFrame)) {
            changeSelection(_currItemId - 1);
         }
      }

      // Left
      if (InputManager.self.inputMaster.UIControl.MoveLeft.WasPressedThisFrame()) {
      }

      // Right
      if (InputManager.self.inputMaster.UIControl.MoveRight.WasPressedThisFrame()) {
      }

      // Continue
      if (InputManager.self.inputMaster.General.Continue.WasPressedThisFrame()) {
         _itemsData[_currItemId].interactItem();
      }

      // Equip
      if (InputManager.self.inputMaster.UIControl.Equip.WasPressedThisFrame()) {
         _itemsData[_currItemId].equipItem();
      }

      // Use
      if (InputManager.self.inputMaster.UIControl.Use.WasPressedThisFrame()) {
         _itemsData[_currItemId].useItem();
      }
      
      // Space
      if (Keyboard.current.spaceKey.wasPressedThisFrame) {
         _itemsData[_currItemId].interactItem();
      }
   }

   private void changeSelection(int newItemId) {
      _prevItemId = _currItemId;
      _currItemId = Mathf.Clamp(newItemId, 0, _itemsData.Count-1) ;
      updateSelection();
   }
   
   public void updateSelection(bool force=false) {
      if (_prevItemId == _currItemId && !force) {
         return;
      }

      _itemsData[_prevItemId].deselectItem();
      _itemsData[_currItemId].selectItem();
   }

   private void OnEnable () {
      UINavigationManager.self.ControllerEnabled(this);
      updateSelection();
   }

   private void OnDisable () {
      UINavigationManager.self.ControllerDisabled(this);
      InputManager.self.inputMaster.UIControl.Disable();
   }

   private struct ItemData {
      public readonly InputField inputField;
      public readonly Button button;
      public readonly Toggle toggle;
      public readonly UINavigationItem navigationItem;

      public ItemData(GameObject item) {
         inputField = item.GetComponent<InputField>();
         button = item.GetComponent<Button>();
         toggle = item.GetComponent<Toggle>();
         navigationItem = item.GetComponent<UINavigationItem>();
      }
      
      public bool isInputField { get { return inputField != null; } }
      public bool isButton { get { return button != null; } }
      public bool isToggle { get { return toggle != null; } }
      public bool isNavigationItem { get { return navigationItem != null; } }

      public void selectItem () {
         inputField?.Select();
         button?.Select();
         toggle?.Select();
         navigationItem?.Select();
      }

      public void deselectItem () {
         navigationItem?.Deselect();
      }

      public void equipItem () {
         navigationItem?.Equip();
      }
      
      public void useItem () {
         navigationItem?.Use();
      }

      public void interactItem () {
         if (isButton && button.interactable) {
            button.onClick.Invoke();
         }
         
         if (isToggle) {
            toggle.isOn = !toggle.isOn;
         }
         
         navigationItem?.Interact();
      }
   }
   
   #region Private Variables
   private List<ItemData> _itemsData;
   private int _prevItemId;
   private int _currItemId;
   #endregion
}
