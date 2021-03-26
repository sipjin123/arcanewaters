using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MapCreationTool
{
   public class Keybindings
   {
      public event System.Action<Keybinding[]> BindingsChanged;

      public Keybinding[] defaultKeybindings { get; set; }

      private Dictionary<Command, Keybinding> keyDictionary = new Dictionary<Command, Keybinding>();

      public bool getAction (Command command) {
         if (keyDictionary.TryGetValue(command, out Keybinding bind)) {
            if (bind.primary != Key.None) {
               if (Keyboard.current[bind.primary].isPressed) {
                  return true;
               }
            }

            if (bind.secondary != Key.None) {
               if (Keyboard.current[bind.secondary].isPressed) {
                  return true;
               }
            }
         }
         return false;
      }

      public bool getActionDown (Command command) {
         if (keyDictionary.TryGetValue(command, out Keybinding bind)) {
            if (bind.primary != Key.None) {
               if (Keyboard.current[bind.primary].wasPressedThisFrame) {
                  return true;
               }
            }

            if (bind.secondary != Key.None) {
               if (Keyboard.current[bind.secondary].wasPressedThisFrame) {
                  return true;
               }
            }
         }
         return false;
      }

      public bool getActionUp (Command command) {
         if (keyDictionary.TryGetValue(command, out Keybinding bind)) {
            if (bind.primary != Key.None) {
               if (Keyboard.current[bind.primary].wasReleasedThisFrame) {
                  return true;
               }
            }

            if (bind.secondary != Key.None) {
               if (Keyboard.current[bind.secondary].wasReleasedThisFrame) {
                  return true;
               }
            }
         }
         return false;
      }

      public void setBindings (Keybinding[] bindings) {
         foreach (Keybinding binding in bindings) {
            if (keyDictionary.ContainsKey(binding.command)) {
               keyDictionary[binding.command] = new Keybinding {
                  command = binding.command,
                  primary = binding.primary,
                  secondary = binding.secondary
               };
            } else {
               keyDictionary.Add(binding.command, new Keybinding {
                  command = binding.command,
                  primary = binding.primary,
                  secondary = binding.secondary
               });
            }
         }

         BindingsChanged?.Invoke(bindings);
      }

      public void setKey (Command command, Key keyCode, bool primary) {
         List<Keybinding> previousChanged = new List<Keybinding>();

         foreach (Keybinding binding in keyDictionary.Values) {
            if (binding.primary == keyCode || binding.secondary == keyCode) {
               Keybinding newBind = new Keybinding {
                  command = binding.command,
                  primary = binding.primary == keyCode ? Key.None : binding.primary,
                  secondary = binding.secondary == keyCode ? Key.None : binding.secondary
               };

               previousChanged.Add(newBind);
            }
         }

         foreach (Keybinding binding in previousChanged) {
            keyDictionary[binding.command] = binding;
         }

         Keybinding newBinding = new Keybinding {
            command = command,
            primary = primary ? keyCode : Key.None,
            secondary = !primary ? keyCode : Key.None
         };

         if (keyDictionary.ContainsKey(command)) {
            newBinding.primary = primary ? newBinding.primary : keyDictionary[command].primary;
            newBinding.secondary = !primary ? newBinding.secondary : keyDictionary[command].secondary;

            keyDictionary[command] = newBinding;
         } else {
            keyDictionary.Add(command, newBinding);
         }

         previousChanged.Add(newBinding);

         BindingsChanged?.Invoke(previousChanged.ToArray());
      }

      public void setDefaults () {
         setBindings(defaultKeybindings);
      }

      public string serialize () {
         return JsonUtility.ToJson(new Serialized {
            bindings = keyDictionary.Select(pair => pair.Value).ToArray()
         });
      }

      public void applySerializedBindings (string data) {
         setBindings(JsonUtility.FromJson<Serialized>(data).bindings);
      }

      public void clearAll () {
         keyDictionary.Clear();
      }

      public enum Command
      {
         PanLeft = 0,
         PanRight = 1,
         PanUp = 2,
         PanDown = 3,
         ZoomIn = 4,
         ZoomOut = 5,
         BrushTool = 6,
         EraserTool = 7,
         FillTool = 8,
         SelectTool = 9,
         MoveTool = 10,
         SelectionAdd = 11,
         SelectionRemove = 12,
         CancelAction = 13
      }

      [System.Serializable]
      public class Serialized
      {
         public Keybinding[] bindings;
      }

      [System.Serializable]
      public struct Keybinding
      {
         public Command command;
         public Key primary;
         public Key secondary;
      }
   }
}