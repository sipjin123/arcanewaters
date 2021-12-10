using UnityEngine;
using System;
using System.Collections.Generic;
using SubjectNerd.Utilities;
using UnityEngine.InputSystem;

public class KeyBindingsPanel : Panel
{
   #region Public Variables

   // Graphic that covers the UI while we wait for user to press a key
   public GameObject inputBlocker;

   // Keybinding section entry
   public KeybindingsSection keybindingsSectionPref;
   // Keybindings list entry
   public KeybindingsEntry entryPref;

   // Parent for list entries
   public Transform entryParent;

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
      foreach (var entry in entryParent.GetComponentsInChildren<KeybindingsSection>()) {
         Destroy(entry.gameObject);
      }
      foreach (var entry in entryParent.GetComponentsInChildren<KeybindingsEntry>()) {
         Destroy(entry.gameObject);
      }

      // Create all entries for every defined action
      _keybindingsEntries = new List<KeybindingsEntry>();
      foreach (var rebindActionMap in _rebindActionMaps) {
         Instantiate(keybindingsSectionPref, entryParent).initialize(rebindActionMap.name);
         rebindActionMap.Init();
         
         foreach (var rebindAction in rebindActionMap.rebindActions) {
            _keybindingsEntries.Add(Instantiate(entryPref, entryParent).initialize(this, rebindAction));
         }
      }

      _initialized = true;
   }

   [Serializable]
   private class RebindActionMap {
      // Action map key
      public string key;
      // Display name of the action map
      public string name;
      // Rebind actions
      public RebindAction[] rebindActions;
      
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
   private bool _initialized;

   [SerializeField, Reorderable] 
   private RebindActionMap[] _rebindActionMaps;

   private List<KeybindingsEntry> _keybindingsEntries;
   #endregion
}
