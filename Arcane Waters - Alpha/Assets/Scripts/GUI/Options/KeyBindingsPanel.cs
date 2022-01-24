using UnityEngine;
using System;
using System.Collections.Generic;
using SubjectNerd.Utilities;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class KeyBindingsPanel : Panel
{
   #region Public Variables

   // Graphic that covers the UI while we wait for user to press a key
   public GameObject inputBlocker;

   // Keybinding section entry
   public KeybindingsSection keybindingsSectionPref;
   // Keybindings list entry
   public KeybindingsEntry entryPref;

   // Parent for keyboard list entries
   public Transform entryKeyboardParent;
   // Parent for gamepad list entries
   public Transform entryGamepadParent;
   
   #endregion

   public override void show () {
      if (!_initialized) {
         initialize();
      }

      base.show();

      // Disable blocker
      inputBlocker.SetActive(false);
   }
   
   public void restoreDefaults() {
      InputManager.self.restoreDefaults();
      refreshTexts();
   }

   private void refreshTexts () {
      foreach (var keybindingsEntry in _keybindingsEntries) {
         keybindingsEntry.refreshTexts();
      }
   }

   private void initialize () {
      // Destroy any existing sections and entries 
      // Keyboard
      foreach (var entry in entryKeyboardParent.GetComponentsInChildren<KeybindingsSection>()) {
         Destroy(entry.gameObject);
      }
      foreach (var entry in entryKeyboardParent.GetComponentsInChildren<KeybindingsEntry>()) {
         Destroy(entry.gameObject);
      }
      // Gamepad
      foreach (var entry in entryGamepadParent.GetComponentsInChildren<KeybindingsSection>()) {
         Destroy(entry.gameObject);
      }
      foreach (var entry in entryGamepadParent.GetComponentsInChildren<KeybindingsEntry>()) {
         Destroy(entry.gameObject);
      }

      // Create all entries for every defined action
      _keybindingsEntries = new List<KeybindingsEntry>();
      foreach (var rebindActionMap in _rebindActionMaps) {
         Instantiate(keybindingsSectionPref, entryKeyboardParent).initialize(rebindActionMap.name);
         Instantiate(keybindingsSectionPref, entryGamepadParent).initialize(rebindActionMap.name);
         rebindActionMap.Init();
         
         foreach (var rebindAction in rebindActionMap.rebindActions) {
            _keybindingsEntries.Add(Instantiate(entryPref, entryKeyboardParent).initialize(this, rebindAction, true));
            _keybindingsEntries.Add(Instantiate(entryPref, entryGamepadParent).initialize(this, rebindAction, false));
         }
      }

      _initialized = true;
   }

   [Serializable]
   private class RebindActionMap {
      // Action map key
      public string key = default;
      // Display name of the action map
      public string name = default;
      // Rebind actions
      public RebindAction[] rebindActions = new RebindAction[0];
      
      public void Init () {
         foreach (var rebindAction in rebindActions) {
            rebindAction.Init(key);
         }
      }
   }

   [Serializable]
   public class RebindAction {
      // Action key
      public string key;
      // Display name of the action
      public string name;
      // Input action name
      [HideInInspector] [NonSerialized] 
      public InputAction inputAction;

      public void Init (string sectionKey) {
         inputAction = InputManager.self.inputMaster.asset.FindActionMap(sectionKey, true).FindAction(key, true);
      }
   }   
   
   #region Private Variables
   // Has the panel been initialized yet
   private bool _initialized = default;

   [SerializeField, Reorderable] 
   private RebindActionMap[] _rebindActionMaps = new RebindActionMap[0];

   private List<KeybindingsEntry> _keybindingsEntries = new List<KeybindingsEntry>();
   #endregion
}
