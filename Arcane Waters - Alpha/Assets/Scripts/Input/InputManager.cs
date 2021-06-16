using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using System.Collections;

public class InputManager : GenericGameManager {
   #region Public Variables

   // Event that is triggered when a binding changes
   public static event Action<BoundKeyAction> keyBindingChanged;

   // Singleton instance
   public static InputManager self;

   // Input Master reference
   public InputMaster inputMaster;

   // The joystick values for gamepad controls
   public Vector2 joystickNavigation;

   // The joystick values for gamepad mouse controls
   public Vector2 mouseJoystickNavigation;

   // The axis value that determines the joystick is active
   public const float JOYSTICK_ACTIVE_VALUE = .05f;

   // Reference to the input settings
   public InputSettings inputSettings;

   // If the scene is focused
   public bool isFocused;

   // Toggles the joystick to control the mouse
   public bool mouseJoystickToggle;

   // If gamepad sprint button is being held down
   public bool holdGamepadSprint;

   // Mouse control movement speed
   public float mouseSpeed = 10f;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;

      loadDefaultKeybindings();

      // Load all saved user keybindings
      foreach (BoundKeyAction boundKeyAction in _keybindings.Values) {
         boundKeyAction.loadLocal();
      }

      initializeInputMaster();
   }

   private void initializeInputMaster () {
      if (Util.isBatch()) {
         return;
      }

      if (Util.isCloudBuild()) {
         D.debug("Initializing input system as {DynamicUpdate} " +
            "SystLang: { " + Application.systemLanguage + "} " +
            "Layout: { " + Keyboard.current.keyboardLayout + "}");
         inputSettings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
      } else {
         D.debug("Initializing input system as {ManualUpdate} " +
            "SystLang: { " + Application.systemLanguage + "} " +
            "Layout: { " + Keyboard.current.keyboardLayout + "}");
         inputSettings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
      }

      // TODO: Setup all gamepad action keybindings here after stabilizing the project by overridding all scripts referencing legacy input system
      inputMaster = new InputMaster();

      inputMaster.Player.Enable();
      inputMaster.Player.ToggleMouseControl.performed += func => mouseToggleAction();
      inputMaster.Player.Jump.performed += func => jumpAction();
      inputMaster.Player.Interact.performed += func => interactAction();
      inputMaster.Player.MouseClick.performed += func => mouseClickAction();

      inputMaster.Player.Dash.performed += func => dashAction(true);
      inputMaster.Player.Dash.canceled += func => dashAction(false);

      inputMaster.Player.Move.performed += func => moveAction(func.ReadValue<Vector2>());
      inputMaster.Player.Move.canceled += func => moveAction(new Vector2(0, 0));

      inputMaster.Player.MouseControl.performed += mfunc => mouseAction(mfunc.ReadValue<Vector2>());
      inputMaster.Player.MouseControl.canceled += mfunc => mouseAction(new Vector2(0, 0));
   }

   private void Update () {
      if (!Util.isCloudBuild() && isFocused) {
         InputSystem.Update();
      }

      if (mouseJoystickToggle) {
         Vector2 newValue = MouseUtils.mousePosition + (mouseJoystickNavigation * mouseSpeed);
         InputState.Change(Mouse.current.position, newValue);
         Mouse.current.WarpCursorPosition(newValue);
      }
   }

   private void OnApplicationFocus (bool focus) {
      isFocused = focus;
   }

   private void dashAction (bool isActive) {
      holdGamepadSprint = isActive;
      D.adminLog("Dash! {" + isActive + "}", D.ADMIN_LOG_TYPE.Gamepad);
   }

   private void mouseClickAction () {
      StartCoroutine(CO_SimulateMouseClick());
   }

   private IEnumerator CO_SimulateMouseClick () {
      // Simulate mouse press down by accessing mouse command to trigger
      Mouse.current.CopyState<MouseState>(out var mouseState);
      mouseState.WithButton((UnityEngine.InputSystem.LowLevel.MouseButton) MouseButton.Left, true);
      InputState.Change(Mouse.current, mouseState);

      yield return new WaitForSeconds(.1f);

      // Simulate mouse release by accessing mouse command to trigger
      mouseState.WithButton((UnityEngine.InputSystem.LowLevel.MouseButton) MouseButton.Left, false);
      InputState.Change(Mouse.current, mouseState);
   }

   private void jumpAction () {
      D.adminLog("Jump!", D.ADMIN_LOG_TYPE.Gamepad);
   }

   private void interactAction () {
      D.adminLog("Interact!", D.ADMIN_LOG_TYPE.Gamepad);
   }

   private void mouseToggleAction () {
      mouseJoystickToggle = !mouseJoystickToggle;
   }

   private void moveAction (Vector2 moveVal) {
      if (Global.player == null) {
         return;
      }

      joystickNavigation = moveVal;
   }

   private void mouseAction (Vector2 mouseVal) {
      if (Global.player == null) {
         return;
      }

      if (Util.isCloudBuild()) {
         mouseJoystickNavigation = new Vector2(mouseVal.x, -mouseVal.y);
      } else {
         mouseJoystickNavigation = new Vector2(mouseVal.x, mouseVal.y);
      }
   }

   public void simulateDirectionPress (Direction direction, float seconds) {
      StartCoroutine(CO_SimulateDirectionPress(direction, seconds)); 
   }

   private IEnumerator CO_SimulateDirectionPress (Direction direction, float seconds) {
      _isMoveSimulated = true;
      _moveDirectionSimulated = direction;
      yield return new WaitForSeconds(seconds);
      _isMoveSimulated = false;
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
            return getKeyAction(KeyAction.MoveUp) || self.joystickNavigation.y > JOYSTICK_ACTIVE_VALUE || (_isMoveSimulated && _moveDirectionSimulated == Direction.North);
         case Direction.East:
            return getKeyAction(KeyAction.MoveRight) || self.joystickNavigation.x > JOYSTICK_ACTIVE_VALUE || (_isMoveSimulated && _moveDirectionSimulated == Direction.East);
         case Direction.South:
            return getKeyAction(KeyAction.MoveDown) || self.joystickNavigation.y < -JOYSTICK_ACTIVE_VALUE || (_isMoveSimulated && _moveDirectionSimulated == Direction.South);
         case Direction.West:
            return getKeyAction(KeyAction.MoveLeft) || self.joystickNavigation.x < -JOYSTICK_ACTIVE_VALUE || (_isMoveSimulated && _moveDirectionSimulated == Direction.West);
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

      if (getKeyAction(KeyAction.MoveLeft) || self.joystickNavigation.x < -JOYSTICK_ACTIVE_VALUE || (_isMoveSimulated && _moveDirectionSimulated == Direction.West)) {
         axis = -1;
      } else if (getKeyAction(KeyAction.MoveRight) || self.joystickNavigation.x > JOYSTICK_ACTIVE_VALUE || (_isMoveSimulated && _moveDirectionSimulated == Direction.East)) {
         axis = 1;
      }

      return axis;
   }

   public static int getVerticalAxis () {
      int axis = 0;

      if (getKeyAction(KeyAction.MoveUp) || self.joystickNavigation.y > JOYSTICK_ACTIVE_VALUE || (_isMoveSimulated && _moveDirectionSimulated == Direction.North)) {
         axis = 1;
      } else if (getKeyAction(KeyAction.MoveDown) || self.joystickNavigation.y < -JOYSTICK_ACTIVE_VALUE || (_isMoveSimulated && _moveDirectionSimulated == Direction.South)) {
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
         return getKeyAction(KeyAction.SpeedUp) || self.holdGamepadSprint;
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

   // Gets set to true when a direction movement is being simulated
   private static bool _isMoveSimulated = false;

   // The movement direction that is being simulated
   private static Direction _moveDirectionSimulated = Direction.East;

   #endregion
}
