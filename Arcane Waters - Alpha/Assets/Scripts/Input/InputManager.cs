﻿using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class InputManager : MonoBehaviour
{
   #region Public Variables

   // Event that is triggered when a binding changes
   public static event Action<BoundKeyAction> keyBindingChanged;

   // Singleton instance
   public static InputManager self;

   // Input Master reference
   public InputMaster inputMaster;

   // The joystick values for gamepad controls
   public Vector2 joystickNavigation;

   // The axis value that determines the joystick is active
   public const float JOYSTICK_ACTIVE_VALUE = .05f;

   #endregion

   private void Awake () {
      D.adminLog("InputManager.Awake...", D.ADMIN_LOG_TYPE.Initialization);
      self = this;

      loadDefaultKeybindings();

      // Load all saved user keybindings
      foreach (BoundKeyAction boundKeyAction in _keybindings.Values) {
         boundKeyAction.loadLocal();
      }

      initializeInputMaster();
      D.adminLog("InputManager.Awake: OK", D.ADMIN_LOG_TYPE.Initialization);
   }

   private void initializeInputMaster () {
      #if IS_SERVER_BUILD && CLOUD_BUILD
      D.debug("InputSystem::This is a server build, input system will be disabled!");
      #else
      D.debug("InputSystem::This is a client build, input system will be enabled!");
      #endif

      if (Util.isBatch()) {
         return;
      }

      // TODO: Setup all gamepad action keybindings here after stabilizing the project by overridding all scripts referencing legacy input system
      inputMaster = new InputMaster();

      inputMaster.Player.Enable();
      inputMaster.Player.Jump.performed += func => jumpAction();
      inputMaster.Player.Interact.performed += func => interactAction();
      inputMaster.Player.Move.performed += func => moveAction(func.ReadValue<Vector2>());
      inputMaster.Player.Move.canceled += func => moveAction(new Vector2(0, 0));
   }

   private void jumpAction () {
      D.adminLog("Jump!", D.ADMIN_LOG_TYPE.Gamepad);
   }

   private void interactAction () {
      D.adminLog("Interact!", D.ADMIN_LOG_TYPE.Gamepad);
   }

   private void moveAction (Vector2 moveFactor) {
      joystickNavigation = moveFactor;
   }

   private void loadDefaultKeybindings () {
      // Create empty bindings for all defined actions
      _keybindings.Clear();
      foreach (KeyAction action in Enum.GetValues(typeof(KeyAction))) {
         _keybindings.Add(action, new BoundKeyAction { action = action });
      }

      // Set movement keys
      _keybindings[KeyAction.MoveUp].primary = Key.W;
      _keybindings[KeyAction.MoveRight].primary = Key.D;
      _keybindings[KeyAction.MoveDown].primary = Key.S;
      _keybindings[KeyAction.MoveLeft].primary = Key.A;
      _keybindings[KeyAction.MoveUp].secondary = Key.UpArrow;
      _keybindings[KeyAction.MoveRight].secondary = Key.RightArrow;
      _keybindings[KeyAction.MoveDown].secondary = Key.DownArrow;
      _keybindings[KeyAction.MoveLeft].secondary = Key.LeftArrow;
      _keybindings[KeyAction.SpeedUp].primary = Key.LeftShift;

      // Set sea battle keys
      _keybindings[KeyAction.FireMainCannon].primary = Key.Space;
      _keybindings[KeyAction.SelectNextSeaTarget].primary = Key.Tab;

      // TODO: Setup mouse button key bindings
      // Camera panning
      //_keybindings[KeyAction.PanCamera].primary = Mouse.current.middleButton;

      _keybindings[KeyAction.Reply].primary = Key.R;
   }

   public static bool isPressingDirection (Direction direction) {
      switch (direction) {
         case Direction.North:
            return getKeyAction(KeyAction.MoveUp) || self.joystickNavigation.y > JOYSTICK_ACTIVE_VALUE;
         case Direction.East:
            return getKeyAction(KeyAction.MoveRight) || self.joystickNavigation.x > JOYSTICK_ACTIVE_VALUE;
         case Direction.South:
            return getKeyAction(KeyAction.MoveDown) || self.joystickNavigation.y < -JOYSTICK_ACTIVE_VALUE;
         case Direction.West:
            return getKeyAction(KeyAction.MoveLeft) || self.joystickNavigation.x < -JOYSTICK_ACTIVE_VALUE;
         case Direction.NorthEast:
            return (self.joystickNavigation.y > JOYSTICK_ACTIVE_VALUE && self.joystickNavigation.x > JOYSTICK_ACTIVE_VALUE) 
               || (isPressingDirection(Direction.North) && isPressingDirection(Direction.East));
         case Direction.SouthEast:
            return (self.joystickNavigation.y < -JOYSTICK_ACTIVE_VALUE && self.joystickNavigation.x > JOYSTICK_ACTIVE_VALUE) 
               || (isPressingDirection(Direction.South) && isPressingDirection(Direction.East));
         case Direction.SouthWest:
            return (self.joystickNavigation.y < -JOYSTICK_ACTIVE_VALUE && self.joystickNavigation.x < -JOYSTICK_ACTIVE_VALUE) 
               || (isPressingDirection(Direction.South) && isPressingDirection(Direction.West));
         case Direction.NorthWest:
            return (self.joystickNavigation.y > JOYSTICK_ACTIVE_VALUE && self.joystickNavigation.x < -JOYSTICK_ACTIVE_VALUE)
               || (isPressingDirection(Direction.North) && isPressingDirection(Direction.West));
      }

      return false;
   }

   public static bool getKeyAction (KeyAction action) {
      if (self._keybindings.TryGetValue(action, out BoundKeyAction boundAction)) {
         bool primary = boundAction.primary != Key.None && KeyUtils.GetKey(boundAction.primary) && (!isKeyDisabled(boundAction.primary));
         bool secondary = boundAction.secondary != Key.None && KeyUtils.GetKey(boundAction.secondary) && !isKeyDisabled(boundAction.secondary);
         return primary || secondary;
      }

      return false;
   }

   public static bool getKeyActionDown (KeyAction action) {
      if (self._keybindings.TryGetValue(action, out BoundKeyAction boundAction)) {
         bool primary = boundAction.primary != Key.None && KeyUtils.GetKeyDown(boundAction.primary) && !isKeyDisabled(boundAction.primary);
         bool secondary = boundAction.secondary != Key.None && KeyUtils.GetKeyDown(boundAction.secondary) && !isKeyDisabled(boundAction.secondary);
         return primary || secondary;
      }

      return false;
   }

   public static bool getKeyActionUp (KeyAction action) {
      if (self._keybindings.TryGetValue(action, out BoundKeyAction boundAction)) {
         bool primary = boundAction.primary != Key.None && KeyUtils.GetKeyUp(boundAction.primary) && !isKeyDisabled(boundAction.primary);
         bool secondary = boundAction.secondary != Key.None && KeyUtils.GetKeyUp(boundAction.secondary) && !isKeyDisabled(boundAction.secondary);
         return primary || secondary;
      }

      return false;
   }

   public static int getHorizontalAxis () {
      int axis = 0;

      if (getKeyAction(KeyAction.MoveLeft) || self.joystickNavigation.x < -JOYSTICK_ACTIVE_VALUE) {
         axis = -1;
      } else if (getKeyAction(KeyAction.MoveRight) || self.joystickNavigation.x > JOYSTICK_ACTIVE_VALUE) {
         axis = 1;
      }

      return axis;
   }

   public static int getVerticalAxis () {
      int axis = 0;

      if (getKeyAction(KeyAction.MoveUp) || self.joystickNavigation.y > JOYSTICK_ACTIVE_VALUE) {
         axis = 1;
      } else if (getKeyAction(KeyAction.MoveDown) || self.joystickNavigation.y < -JOYSTICK_ACTIVE_VALUE) {
         axis = -1;
      }

      return axis;
   }

   public static Vector2 getMovementInput () {
      return new Vector2(getHorizontalAxis(), getVerticalAxis());
   }

   public static bool isLeftClickKeyPressed () {
      if (isActionInputEnabled()) {
         return KeyUtils.GetButtonDown(MouseButton.Left);
      }

      return false;
   }

   public static bool isRightClickKeyPressed () {
      if (isActionInputEnabled()) {
         // Define the set of keys that we want to allow as "action" keys
         return KeyUtils.GetButtonDown(MouseButton.Right);
      }

      return false;
   }

   public static bool isActionKeyPressed () {
      if (isActionInputEnabled()) {
         // Define the set of keys that we want to allow as "action" keys
         return KeyUtils.GetKeyDown(Key.E);
      }

      return false;
   }

   public static bool isJumpKeyPressed () {
      if (isActionInputEnabled()) {
         // Define the set of keys that we want to allow as "action" keys
         return KeyUtils.GetKeyDown(Key.Space);
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
         return KeyUtils.GetButtonDown(MouseButton.Right);
      }

      return false;
   }

   public static bool isFireCannonMouse () {
      if (isActionInputEnabled()) {
         return KeyUtils.GetButton(MouseButton.Right);
      }

      return false;
   }

   public static bool isFireCannonMouseUp () {
      if (isActionInputEnabled()) {
         return KeyUtils.GetButtonUp(MouseButton.Right);
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

   public static bool isSpeedUpKeyDown () {
      if (isActionInputEnabled()) {
         return getKeyAction(KeyAction.SpeedUp);
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

   public static void setBindingKey (KeyAction action, Key key, bool isPrimary) {
      // Unbind any actions that uses this key
      foreach (BoundKeyAction boundKeyAction in self._keybindings.Values) {
         bool changed = false;

         if (boundKeyAction.primary == key) {
            boundKeyAction.primary = Key.None;
            changed = true;
         }

         if (boundKeyAction.secondary == key) {
            boundKeyAction.secondary = Key.None;
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
      return new Vector2(MouseUtils.mousePosition.x / Screen.width, MouseUtils.mousePosition.y / Screen.height);
   }

   private static bool isKeyDisabled (Key keyCode) {
      return self._disabledKeys.Contains(keyCode);
   }

   public static void disableKey (Key keyCode) {
      if (!self._disabledKeys.Contains(keyCode)) {
         self._disabledKeys.Add(keyCode);
      }
   }

   public static void enableKey (Key keyCode) {
      if (self._disabledKeys.Contains(keyCode)) {
         self._disabledKeys.Remove(keyCode);
      }
   }

   #region Private Variables

   // List of keybindings, indexed by their action
   private Dictionary<KeyAction, BoundKeyAction> _keybindings = new Dictionary<KeyAction, BoundKeyAction>();

   // A list of keys that will be ignored when getting inputs
   private List<Key> _disabledKeys = new List<Key>();

   #endregion
}
