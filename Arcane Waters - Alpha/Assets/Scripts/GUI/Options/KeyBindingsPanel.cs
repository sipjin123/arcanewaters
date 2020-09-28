using UnityEngine;
using System.Collections.Generic;
using System;
using SubjectNerd.Utilities;

public class KeyBindingsPanel : Panel
{
   #region Public Variables

   // Graphic that covers the UI while we wait for user to press a key
   public GameObject inputBlocker;

   // Keybindings list entry
   public KeybindingsEntry entryPref;

   // Parent for list entries
   public Transform entryParent;

   #endregion

   private void OnEnable () {
      InputManager.keyBindingChanged += bindingChanged;
   }

   private void OnDisable () {
      InputManager.keyBindingChanged -= bindingChanged;
   }

   public override void show () {
      if (!_initialized) {
         initialize();
      }

      base.show();

      // Set values for all entries
      foreach (KeybindingsEntry entry in _entries.Values) {
         BoundKeyAction binding = InputManager.getBinding(entry.action);
         entry.setPrimary(binding.primary);
         entry.setSecondary(binding.secondary);
      }

      // Disable blocker
      inputBlocker.SetActive(false);
      _waitingEntry = null;
   }

   private void bindingChanged (BoundKeyAction binding) {
      if (_entries.TryGetValue(binding.action, out KeybindingsEntry entry)) {
         entry.setPrimary(binding.primary);
         entry.setSecondary(binding.secondary);
      }
   }

   public void requestUserForKey (KeybindingsEntry entry, bool isPrimary) {
      _waitingEntry = entry;
      _waitingForPrimary = isPrimary;
      inputBlocker.SetActive(true);
   }

   public override void Update () {
      base.Update();

      if (_waitingEntry != null) {
         if (Input.anyKeyDown) {
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode))) {
               if (Input.GetKeyDown(key)) {
                  // Set the binding
                  InputManager.setBindingKey(_waitingEntry.action, key, _waitingForPrimary);

                  inputBlocker.SetActive(false);
                  _waitingEntry = null;
                  break;
               }
            }
         }
      }
   }

   private void initialize () {
      _entries = new Dictionary<KeyAction, KeybindingsEntry>();

      // Destroy any existing entries
      foreach (KeybindingsEntry entry in entryParent.GetComponentsInChildren<KeybindingsEntry>()) {
         Destroy(entry.gameObject);
      }

      // Create all entries for every defined action
      foreach (KeyActionNamePair pair in _actionsToShow) {
         _entries.Add(pair.keyAction, Instantiate(entryPref, entryParent).initialize(this, pair.keyAction, pair.name));
      }

      _initialized = true;
   }

   #region Private Variables

   // Has the panel been initialized yet
   private bool _initialized;

   // Bindings entries in the panel
   private Dictionary<KeyAction, KeybindingsEntry> _entries;

   // The entry that is currently waiting for user input
   private KeybindingsEntry _waitingEntry;

   // Are we waiting for primary or secondary key
   private bool _waitingForPrimary;

   // List of actions we show in the panel
   [SerializeField, Reorderable]
   private KeyActionNamePair[] _actionsToShow;

   [Serializable]
   private class KeyActionNamePair
   {
      // Display name of the action
      public string name;

      // Key action type
      public KeyAction keyAction;
   }

   #endregion
}
