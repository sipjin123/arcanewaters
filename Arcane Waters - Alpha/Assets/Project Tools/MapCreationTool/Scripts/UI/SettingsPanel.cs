using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class SettingsPanel : UIPanel
   {
      [SerializeField]
      private BindingListEntry entryPref = null;

      [Space(5)]
      [SerializeField]
      private Button[] tabsButtons = new Button[0];
      [SerializeField]
      private GameObject[] tabs = new GameObject[0];
      [SerializeField]
      private RectTransform entriesContainer = null;
      [SerializeField]
      private GameObject bindListenCover = null;

      private Dictionary<Keybindings.Command, BindingListEntry> commandEntries = new Dictionary<Keybindings.Command, BindingListEntry>();

      private Action<Key> onAnyKey;

      protected override void Awake () {
         base.Awake();

         for (int i = 0; i < tabsButtons.Length; i++) {
            int index = i;
            tabsButtons[i].onClick.AddListener(() => openTab(index));
         }

         foreach (Keybindings.Command command in Enum.GetValues(typeof(Keybindings.Command))) {
            BindingListEntry entry = Instantiate(entryPref, entriesContainer);
            entry.name = command.ToString();
            entry.commandText.text = command.ToString();
            entry.primaryButton.onClick.AddListener(() => beginSetBind(command, true));
            entry.SecondaryButton.onClick.AddListener(() => beginSetBind(command, false));

            commandEntries.Add(command, entry);
         }
      }

      private void OnEnable () {
         Settings.keybindings.BindingsChanged += keyBindingsChanged;
      }

      private void OnDisable () {
         Settings.keybindings.BindingsChanged -= keyBindingsChanged;
      }

      private void Update () {
         // TODO: Check what this feature does after input manager upgrade
         if (onAnyKey != null) {
            foreach (Key keyCode in Enum.GetValues(typeof(Key))) {
               if (Keyboard.current[keyCode].isPressed) {
                  onAnyKey(keyCode);
                  break;
               }
            }
         }
      }

      private void keyBindingsChanged (Keybindings.Keybinding[] bindings) {
         foreach (Keybindings.Keybinding binding in bindings) {
            if (commandEntries.TryGetValue(binding.command, out BindingListEntry entry)) {
               entry.primaryButton.GetComponentInChildren<Text>().text = binding.primary.ToString();
               entry.SecondaryButton.GetComponentInChildren<Text>().text = binding.secondary.ToString();
            }
         }
      }

      private void beginSetBind (Keybindings.Command command, bool primary) {
         bindListenCover.SetActive(true);

         onAnyKey = (keyCode) => {
            bindListenCover.SetActive(false);
            onAnyKey = null;
            setBind(command, keyCode, primary);
         };
      }

      private void setBind (Keybindings.Command command, Key keyCode, bool primary) {
         Settings.keybindings.setKey(command, keyCode, primary);
         Settings.save();
      }

      public void restoreDefaults () {
         Settings.setDefaults();
         Settings.save();
      }

      public void open () {
         show();

         bindListenCover.SetActive(false);
         onAnyKey = null;

         openTab(0);
      }

      public void openTab (int index) {
         if (tabsButtons.Length > 0) {
            foreach (Button button in tabsButtons) {
               button.interactable = true;
            }
            tabsButtons[index].interactable = false;
         }

         if (tabs.Length > 0) {
            foreach (GameObject tab in tabs) {
               tab.SetActive(false);
            }
            tabs[index].SetActive(true);
         }
      }

      public void close () {
         onAnyKey = null;
         hide();
      }
   }
}
