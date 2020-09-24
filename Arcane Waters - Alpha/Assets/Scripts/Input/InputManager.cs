using UnityEngine;
using System.Collections.Generic;
using System;

public class InputManager : MonoBehaviour
{
   #region Public Variables

   // Event that is triggered when a binding changes
   public static event Action<BoundKeyAction> keyBindingChanged;

   // Singleton instance
   public static InputManager self;

   #endregion

   private void Awake () {
      self = this;

      loadDefaultKeybindings();

      // Load all saved user keybindings
      foreach (BoundKeyAction boundKeyAction in _keybindings.Values) {
         boundKeyAction.loadLocal();
      }
   }

   private void loadDefaultKeybindings () {
      // Create empty bindings for all defined actions
      _keybindings.Clear();
      foreach (KeyAction action in Enum.GetValues(typeof(KeyAction))) {
         _keybindings.Add(action, new BoundKeyAction { action = action });
      }

      // Set movement keys
      _keybindings[KeyAction.MoveUp].primary = KeyCode.W;
      _keybindings[KeyAction.MoveRight].primary = KeyCode.D;
      _keybindings[KeyAction.MoveDown].primary = KeyCode.S;
      _keybindings[KeyAction.MoveLeft].primary = KeyCode.A;
      _keybindings[KeyAction.MoveUp].secondary = KeyCode.UpArrow;
      _keybindings[KeyAction.MoveRight].secondary = KeyCode.RightArrow;
      _keybindings[KeyAction.MoveDown].secondary = KeyCode.DownArrow;
      _keybindings[KeyAction.MoveLeft].secondary = KeyCode.LeftArrow;
   }

   public static bool isPressingDirection (Direction direction) {
      switch (direction) {
         case Direction.North:
            return getKeyAction(KeyAction.MoveUp);
         case Direction.East:
            return getKeyAction(KeyAction.MoveRight);
         case Direction.South:
            return getKeyAction(KeyAction.MoveDown);
         case Direction.West:
            return getKeyAction(KeyAction.MoveLeft);
         case Direction.NorthEast:
            return isPressingDirection(Direction.North) && isPressingDirection(Direction.East);
         case Direction.SouthEast:
            return isPressingDirection(Direction.South) && isPressingDirection(Direction.East);
         case Direction.SouthWest:
            return isPressingDirection(Direction.South) && isPressingDirection(Direction.West);
         case Direction.NorthWest:
            return isPressingDirection(Direction.North) && isPressingDirection(Direction.West);
      }

      return false;
   }

   public static bool getKeyAction (KeyAction action) {
      if (self._keybindings.TryGetValue(action, out BoundKeyAction boundAction)) {
         return Input.GetKey(boundAction.primary) || Input.GetKey(boundAction.secondary);
      }

      return false;
   }

   public static bool getKeyActionDown (KeyAction action) {
      if (self._keybindings.TryGetValue(action, out BoundKeyAction boundAction)) {
         return Input.GetKeyDown(boundAction.primary) || Input.GetKeyDown(boundAction.secondary);
      }

      return false;
   }

   public static bool getKeyActionUp (KeyAction action) {
      if (self._keybindings.TryGetValue(action, out BoundKeyAction boundAction)) {
         return Input.GetKeyUp(boundAction.primary) || Input.GetKeyUp(boundAction.secondary);
      }

      return false;
   }

   public static bool isRightClickKeyPressed () {
      // Don't respond to action keys while the player is dead
      if (Global.player == null || Global.player.isDead()) {
         return false;
      }

      // Can't initiate actions while typing
      if (ChatManager.isTyping()) {
         return false;
      }

      // Define the set of keys that we want to allow as "action" keys
      return Input.GetKeyDown(KeyCode.Mouse1);
   }

   public static bool isActionKeyPressed () {
      // Don't respond to action keys while the player is dead
      if (Global.player == null || Global.player.isDead()) {
         return false;
      }

      // Can't initiate actions while typing
      if (ChatManager.isTyping()) {
         return false;
      }

      // Define the set of keys that we want to allow as "action" keys
      return Input.GetKeyDown(KeyCode.E);
   }

   public static bool isJumpKeyPressed () {
      // Don't respond to action keys while the player is dead
      if (Global.player == null || Global.player.isDead()) {
         return false;
      }

      // Can't initiate actions while typing
      if (ChatManager.isTyping()) {
         return false;
      }

      // Define the set of keys that we want to allow as "action" keys
      return Input.GetKeyDown(KeyCode.Space);
   }

   public static void setBindingKey (KeyAction action, KeyCode key, bool isPrimary) {
      // Unbind any actions that uses this key
      foreach (BoundKeyAction boundKeyAction in self._keybindings.Values) {
         bool changed = false;

         if (boundKeyAction.primary == key) {
            boundKeyAction.primary = KeyCode.None;
            changed = true;
         }

         if (boundKeyAction.secondary == key) {
            boundKeyAction.secondary = KeyCode.None;
            changed = true;
         }

         if (changed) {
            boundKeyAction.saveLocal();
            keyBindingChanged?.Invoke(boundKeyAction);
         }
      }

      // Bind the key
      if (self._keybindings.TryGetValue(action, out BoundKeyAction keyAction)) {
         if (isPrimary) {
            keyAction.primary = key;
         } else {
            keyAction.secondary = key;
         }

         keyAction.saveLocal();
         keyBindingChanged?.Invoke(keyAction);
      } else {
         D.error($"Could not find { action } action when binding a key.");
      }
   }

   public static BoundKeyAction getBinding (KeyAction action) {
      if (self._keybindings.TryGetValue(action, out BoundKeyAction boundAction)) {
         return boundAction;
      }

      D.error($"Could not find { action } action when looking for a binding.");
      return new BoundKeyAction { action = action };
   }

   #region Private Variables

   // List of keybindings, indexed by their action
   private Dictionary<KeyAction, BoundKeyAction> _keybindings = new Dictionary<KeyAction, BoundKeyAction>();

   #endregion
}
