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
      _keybindings[KeyAction.SpeedUp].primary = KeyCode.LeftShift;

      // Set sea battle keys
      _keybindings[KeyAction.FireMainCannon].primary = KeyCode.Space;
      _keybindings[KeyAction.SelectNextSeaTarget].primary = KeyCode.Tab;

      // Camera panning
      _keybindings[KeyAction.PanCamera].primary = KeyCode.Mouse2;

      _keybindings[KeyAction.Reply].primary = KeyCode.R;
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
         bool primary = Input.GetKey(boundAction.primary) && !isKeyDisabled(boundAction.primary);
         bool secondary = Input.GetKey(boundAction.secondary) && !isKeyDisabled(boundAction.secondary);
         return primary || secondary;
      }

      return false;
   }

   public static bool getKeyActionDown (KeyAction action) {
      if (self._keybindings.TryGetValue(action, out BoundKeyAction boundAction)) {
         bool primary = Input.GetKeyDown(boundAction.primary) && !isKeyDisabled(boundAction.primary);
         bool secondary = Input.GetKeyDown(boundAction.secondary) && !isKeyDisabled(boundAction.secondary);
         return primary || secondary;
      }

      return false;
   }

   public static bool getKeyActionUp (KeyAction action) {
      if (self._keybindings.TryGetValue(action, out BoundKeyAction boundAction)) {
         bool primary = Input.GetKeyUp(boundAction.primary) && !isKeyDisabled(boundAction.primary);
         bool secondary = Input.GetKeyUp(boundAction.secondary) && !isKeyDisabled(boundAction.secondary);
         return primary || secondary;
      }

      return false;
   }

   public static int getHorizontalAxis () {
      int axis = 0;

      if (getKeyAction(KeyAction.MoveLeft)) {
         axis = -1;
      } else if (getKeyAction(KeyAction.MoveRight)) {
         axis = 1;
      }

      return axis;
   }

   public static int getVerticalAxis () {
      int axis = 0;

      if (getKeyAction(KeyAction.MoveUp)) {
         axis = 1;
      } else if (getKeyAction(KeyAction.MoveDown)) {
         axis = -1;
      }

      return axis;
   }

   public static Vector2 getMovementInput () {
      return new Vector2(getHorizontalAxis(), getVerticalAxis());
   }

   public static bool isLeftClickKeyPressed () {
      if (isActionInputEnabled()) {
         return Input.GetKeyDown(KeyCode.Mouse0);
      }

      return false;
   }

   public static bool isRightClickKeyPressed () {
      if (isActionInputEnabled()) {
         // Define the set of keys that we want to allow as "action" keys
         return Input.GetKeyDown(KeyCode.Mouse1);
      }

      return false;
   }

   public static bool isActionKeyPressed () {
      if (isActionInputEnabled()) {
         // Define the set of keys that we want to allow as "action" keys
         return Input.GetKeyDown(KeyCode.E);
      }

      return false;
   }

   public static bool isJumpKeyPressed () {
      if (isActionInputEnabled()) {
         // Define the set of keys that we want to allow as "action" keys
         return Input.GetKeyDown(KeyCode.Space);
      } 

      return false;
   }

   public static bool isFireCannonKeyDown () {
      if (isActionInputEnabled()) {
         return getKeyActionDown(KeyAction.FireMainCannon);
      }

      return false;
   }

   public static bool isFireCannonMouseDown () {
      if (isActionInputEnabled()) {
         return Input.GetMouseButtonDown((int)MouseButton.Right);
      }

      return false;
   }

   public static bool isFireCannonMouse () {
      if (isActionInputEnabled()) {
         return Input.GetMouseButton((int) MouseButton.Right);
      }

      return false;
   }

   public static bool isFireCannonMouseUp () {
      if (isActionInputEnabled()) {
         return Input.GetMouseButtonUp((int) MouseButton.Right);
      }

      return false;
   }

   public static bool isSelectNextTargetKeyDown () {
      if (isActionInputEnabled()) {
         return getKeyActionDown(KeyAction.SelectNextSeaTarget);
      }

      return false;
   }

   public static bool isSpeedUpKeyPressed () {
      if (isActionInputEnabled()) {
         return getKeyActionDown(KeyAction.SpeedUp);
      }

      return false;
   }

   public static bool isSpeedUpKeyReleased () {
      if (isActionInputEnabled()) {
         return getKeyActionUp(KeyAction.SpeedUp);
      }

      return false;
   }

   public static bool isActionInputEnabled () {
      // Don't respond to action keys while the player is dead
      if (Global.player == null || Global.player.isDead()) {
         return false;
      }

      // Can't initiate actions while typing
      if (ChatManager.isTyping()) {
         return false;
      }

      return true;
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

   public static Vector2 getCameraPanningAxis () {
      return new Vector2(Input.mousePosition.x / Screen.width, Input.mousePosition.y / Screen.height);
   }

   private static bool isKeyDisabled (KeyCode keyCode) {
      return self._disabledKeys.Contains(keyCode);
   }

   public static void disableKey (KeyCode keyCode) {
      if (!self._disabledKeys.Contains(keyCode)) {
         self._disabledKeys.Add(keyCode);
      }
   }

   public static void enableKey (KeyCode keyCode) {
      if (self._disabledKeys.Contains(keyCode)) {
         self._disabledKeys.Remove(keyCode);
      }
   }

   #region Private Variables

   // List of keybindings, indexed by their action
   private Dictionary<KeyAction, BoundKeyAction> _keybindings = new Dictionary<KeyAction, BoundKeyAction>();

   // A list of keys that will be ignored when getting inputs
   private List<KeyCode> _disabledKeys = new List<KeyCode>();

   #endregion
}
