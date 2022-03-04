using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using System.Collections;
using UnityEngine.InputSystem.UI;

/*
Actions configuration:
   All action maps with actions are configured in the Assets/Input/InputMaster.asset
   Current action maps with actions available here https://docs.google.com/spreadsheets/d/10K_nSLf71ZXn7G9dPaPcgZxuHOL0IOVLKC3AVZovnRw/edit#gid=0

      General		
	      MoveUp	W
	      MoveLeft	A
	      MoveDown	S
	      MoveRight	D
	      Interact	LMB
		      
      Land		
	      Jump	Space
	      Action	RMB
	      Sprint	LShift
		      
      LandBattle		
	      Ability1	1
	      Ability2	2
	      Ability3	3
	      Ability4	4
	      Ability5	5
	      StanceDefense	F1
	      StanceBalanced	F2
	      StanceAttack	F3
	      NextTarget	Tab
		      
      Sea		
	      FireCannon	RMB
	      Dash	LShift

      Pvp			
	      Stat		Tab
		      
      Hud		
	      Shortcut1	1
	      Shortcut2	2
	      Shortcut3	3
	      Shortcut4	4
	      Shortcut5	5
	      ShowPlayerName	LAlt
		      
      UIShotcuts		
	      Abilities	U
	      Chat	Enter
	      FriendsList	F
	      GuildInfo	G
	      Inventory	I
	      Mail	K
	      Map	M
	      Options	Esc
	      ShipList	L
	      Store	B
	      TradeHistory	T
	      VisitFriend	N
	      ChatReply	R
		      
      UIControl		
	      Move	WASD
	      Equip	E
	      Use	F
	      Close	Esc
		      
      Chat		
	      SendMessage	Enter
	      ExitChat	Esc
	      HistoryUp	ArrowUp
	      HistoryDown	ArrowDown
	      SelectChat	Tab
         Autocomplete		Tab	      
 
Bindings configuration:
   Each action should have four bindings with following indexes (used for controls rebinding logic)
      * (0) Keyboard Primary
      * (1) Keyboard Secondary
      * (2) Gamepad Primary
      * (3) Gamepad Secondary

Usage:
   Check if action has been performed in the current frame:
      InputManager.self.inputMaster.Land.Jump.WasPerformedThisFrame()
   or read value
      InputManager.self.inputMaster.Land.Jump.ReadValue<float>()
   or check if action is pressed
      InputManager.self.inputMaster.Land.Jump.IsPressed()

   Subscribe on action performed event:
      InputManager.self.inputMaster.Land.Jump.performed += _ => { Debug.Log("Jump"); };
   or
      InputManager.self.inputMaster.Land.Jump.performed += OnJump;

   Enable or disable needed action map:
      InputManager.self.inputMaster.Land.Enable();
      InputManager.self.inputMaster.Land.Disable();
*/

public class InputManager : GenericGameManager {
   #region Public Variables

   // Singleton instance
   public static InputManager self;

   // Input Master reference
   public InputMaster inputMaster;
   
   // Controls binding types
   public enum BindingType {Keyboard, Gamepad};

   // Controls bindings ids
   public enum BindingId {KeyboardPrimary, KeyboardSecondary, GamepadPrimary, GamepadSecondary};
   
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

   // Action map states bulk control
   public ActionMapStates actionMapStates;
   
   // Is movement simulated for stress testing
   public bool IsMoveSimulated { get { return _isMoveSimulated; } }
   
   // Is any gamepad connected
   public bool isGamepadConnected;
   
   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
      // loadDefaultKeybindings();

      // Load all saved user keybindings
      // foreach (BoundKeyAction boundKeyAction in _keybindings.Values) {
      //    boundKeyAction.loadLocal();
      // }

      initializeInputMaster();
   }

   private void initializeInputMaster () {
      // TODO: Setup all gamepad action keybindings here after stabilizing the project by overridding all scripts referencing legacy input system
      inputMaster = new InputMaster();
      
      // Restore user custom bindings
      restoreCustomBindings();

      // inputMaster.Player.ToggleMouseControl.performed += func => mouseToggleAction();
      // inputMaster.Player.MouseClick.performed += func => mouseClickAction();

      // inputMaster.Sea.Dash.performed += func => dashAction(true);
      // inputMaster.Sea.Dash.canceled += func => dashAction(false);

      // inputMaster.Player.Move.performed += func => moveAction(func.ReadValue<Vector2>());
      // inputMaster.Player.Move.canceled += func => moveAction(new Vector2(0, 0));

      // inputMaster.Player.MouseControl.performed += mfunc => mouseAction(mfunc.ReadValue<Vector2>());
      // inputMaster.Player.MouseControl.canceled += mfunc => mouseAction(new Vector2(0, 0));

      if (Util.forceServerBatchInEditor) {
         return;
      }
      if (Util.isBatch()) {
         actionMapStates.DisableAll();
         inputMaster.Disable();
         foreach (var obj in FindObjectsOfType<InputSystemUIInputModule>()) {
            // Disable InputSystemUIInputModule
            obj.enabled = false;
            // Disable whole event system
            obj.gameObject.SetActive(false); 
         }
      } 
      else {
         inputMaster.General.Enable();
         inputMaster.Land.Enable();
         inputMaster.LandBattle.Disable();
         inputMaster.Sea.Enable();
         inputMaster.Pvp.Enable();
         inputMaster.Hud.Enable();
         inputMaster.UIShotcuts.Enable();
         inputMaster.UIControl.Disable();
         inputMaster.Chat.Disable();
      }
      
      if (Util.isBatch()) {
         inputSettings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
      } 
      else {
         if (Util.isCloudBuild()) {
            //D.debug("Initializing input system as {DynamicUpdate} " +
            //   "SystLang: { " + Application.systemLanguage + "} " +
            //   "Layout: { " + Keyboard.current.keyboardLayout + "}");
            inputSettings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
         } else {
            //D.debug("Initializing input system as {ManualUpdate} " +
            //   "SystLang: { " + Application.systemLanguage + "} " +
            //   "Layout: { " + Keyboard.current.keyboardLayout + "}");
            inputSettings.updateMode = InputSettings.UpdateMode.ProcessEventsManually;
         }      
      }
   }

   private void Update () {
      if (!isFocused) {
         return;
      }
      
      if (!Util.isCloudBuild()) {
         InputSystem.Update();
      }

      isGamepadConnected = Gamepad.all.Count > 0;

      if (mouseJoystickToggle) {
         Vector2 newValue = MouseUtils.mousePosition + (mouseJoystickNavigation * mouseSpeed);
         InputState.Change(Mouse.current.position, newValue);
         Mouse.current.WarpCursorPosition(newValue);
      }
   }

   private void OnApplicationFocus (bool focus) {
      isFocused = focus;
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

   #region Auto move functions
   public void simulateDirectionPress (Direction direction, float seconds) {
      StartCoroutine(CO_SimulateDirectionPress(direction, seconds)); 
   }

   private IEnumerator CO_SimulateDirectionPress (Direction direction, float seconds) {
      _isMoveSimulated = true;
      _moveDirectionSimulated = direction;
      yield return new WaitForSeconds(seconds);
      _isMoveSimulated = false;
   }
   #endregion

   private void saveCustomBindings() {
      var rebinds = inputMaster.SaveBindingOverridesAsJson();
      PlayerPrefs.SetString("rebinds", rebinds);
   }

   private void restoreCustomBindings() {
      var rebinds = PlayerPrefs.GetString("rebinds");
      inputMaster.LoadBindingOverridesFromJson(rebinds);
   }   

   public void rebindAction(InputAction inputAction, BindingType bindingType, BindingId bindingId, Action callback) {
      bool actionMapStatus = inputAction.actionMap.enabled;
      inputAction.actionMap.Disable();
      
      string cancelKey = "";
      switch (bindingType) {
         case BindingType.Keyboard:
            cancelKey = "<Keyboard>/escape";
            break;
         case BindingType.Gamepad:
            cancelKey = "<Gamepad>/select";
            break;
      }
        
      inputAction.PerformInteractiveRebinding()
         .WithControlsHavingToMatchPath(bindingType.ToString())
         .WithTargetBinding((int)bindingId)
         .WithCancelingThrough(cancelKey)
         .OnComplete(_ => {
            saveCustomBindings();
            if (actionMapStatus) {
               inputAction.actionMap.Enable();
            }
            callback?.Invoke();
         })
         .OnCancel(_ => {
            if (actionMapStatus) {
               inputAction.actionMap.Enable();
            }            
            callback?.Invoke();
         })
         .Start();
   }

   public void restoreDefaults () {
      foreach (var actionMap in inputMaster.asset.actionMaps) {
         actionMap.RemoveAllBindingOverrides();
      }
      saveCustomBindings();
   }

   public struct ActionMapStates {
      public void Save() {
         _general = self.inputMaster.General.enabled;
         _land = self.inputMaster.Land.enabled;
         _landBattle = self.inputMaster.LandBattle.enabled;
         _sea = self.inputMaster.Sea.enabled;
         _pvp = self.inputMaster.Pvp.enabled;
         _hud = self.inputMaster.Hud.enabled;
         _uiShotcuts = self.inputMaster.UIShotcuts.enabled;
         _uiControl = self.inputMaster.UIControl.enabled;
         _chat = self.inputMaster.Chat.enabled;
      }

      public void Restore() {
         if (_general) self.inputMaster.General.Enable(); else self.inputMaster.General.Disable();
         if (_land) self.inputMaster.Land.Enable(); else self.inputMaster.Land.Disable();
         if (_landBattle) self.inputMaster.LandBattle.Enable(); else self.inputMaster.LandBattle.Disable();
         if (_sea) self.inputMaster.Sea.Enable(); else self.inputMaster.Sea.Disable();
         if (_pvp) self.inputMaster.Pvp.Enable(); else self.inputMaster.Pvp.Disable();
         if (_hud) self.inputMaster.Hud.Enable(); else self.inputMaster.Hud.Disable();
         if (_uiShotcuts) self.inputMaster.UIShotcuts.Enable(); else self.inputMaster.UIShotcuts.Disable();
         if (_uiControl) self.inputMaster.UIControl.Enable(); else self.inputMaster.UIControl.Disable();
         if (_chat) self.inputMaster.Chat.Enable(); else self.inputMaster.Chat.Disable();
      }

      public void DisableAll() {
         self.inputMaster.General.Disable();
         self.inputMaster.Land.Disable();
         self.inputMaster.LandBattle.Disable();
         self.inputMaster.Sea.Disable();
         self.inputMaster.Pvp.Disable();
         self.inputMaster.Hud.Disable();
         self.inputMaster.UIShotcuts.Disable();
         self.inputMaster.UIControl.Disable();
         self.inputMaster.Chat.Disable();         
      }

      #region Private Variables
      private bool _general;
      private bool _land;
      private bool _landBattle;
      private bool _sea;
      private bool _pvp;
      private bool _hud;
      private bool _uiShotcuts;
      private bool _uiControl;
      private bool _chat;
      #endregion      
   }
   
   public static Direction? isPressingAnyDirection () {
      var directions = Enum.GetValues(typeof(Direction));
      
      foreach (Direction direction in directions) {
         if (isPressingDirection(direction)) {
            return direction;
         }
      }

      return null;
   }

   public static bool isPressingDirection (Direction direction) {
      #region Perf tests automove simulation
      if (_isMoveSimulated) {
         switch (direction) {
            case Direction.North:
               return _moveDirectionSimulated == Direction.North;
            case Direction.East:
               return _moveDirectionSimulated == Direction.East;
            case Direction.South:
               return _moveDirectionSimulated == Direction.South;
            case Direction.West:
               return _moveDirectionSimulated == Direction.West; 
         }
      }
      #endregion
      
      // Skip main logic for batch mode 
      if (Util.isBatch()) {
         return false;
      }

      // No direction move when game lost focus
      if (!self.isFocused) {
         if (self.inputMaster.General.enabled) self.inputMaster.General.Disable();
         return false;
      } 
      else {
         if (!self.inputMaster.General.enabled) self.inputMaster.General.Enable();
      }

      switch (direction) {
         case Direction.North:
            return self.inputMaster.General.MoveUp.IsPressed() || self.joystickNavigation.y > JOYSTICK_ACTIVE_VALUE;
         case Direction.East:
            return self.inputMaster.General.MoveRight.IsPressed() || self.joystickNavigation.x > JOYSTICK_ACTIVE_VALUE;
         case Direction.South:
            return self.inputMaster.General.MoveDown.IsPressed() || self.joystickNavigation.y < -JOYSTICK_ACTIVE_VALUE;
         case Direction.West:
            return self.inputMaster.General.MoveLeft.IsPressed() || self.joystickNavigation.x < -JOYSTICK_ACTIVE_VALUE;
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

   private static int getHorizontalAxis () {
      int axis = 0;

      if (self.inputMaster.General.MoveLeft.IsPressed() || self.joystickNavigation.x < -JOYSTICK_ACTIVE_VALUE || (_isMoveSimulated && _moveDirectionSimulated == Direction.West)) {
         axis = -1;
      } else if (self.inputMaster.General.MoveRight.IsPressed() || self.joystickNavigation.x > JOYSTICK_ACTIVE_VALUE || (_isMoveSimulated && _moveDirectionSimulated == Direction.East)) {
         axis = 1;
      }

      return axis;
   }

   private static int getVerticalAxis () {
      int axis = 0;

      if (self.inputMaster.General.MoveUp.IsPressed() || self.joystickNavigation.y > JOYSTICK_ACTIVE_VALUE || (_isMoveSimulated && _moveDirectionSimulated == Direction.North)) {
         axis = 1;
      } else if (self.inputMaster.General.MoveDown.IsPressed() || self.joystickNavigation.y < -JOYSTICK_ACTIVE_VALUE || (_isMoveSimulated && _moveDirectionSimulated == Direction.South)) {
         axis = -1;
      }

      return axis;
   }

   public static Vector2 getMovementInput () {
      // Do not block movement input when auto move simulated
      if (Util.isBatch() && !Util.isAutoMove()) {
         return Vector2.zero;
      }
      return new Vector2(getHorizontalAxis(), getVerticalAxis());
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

      // Don't respond to action keys if input is globally disabled
      return self._isInputEnabled;
   }

   public static Vector2 getCameraPanningAxis () {
      if (Util.isBatch()) {
         return Vector2.zero;
      }
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

   public static void toggleInput(bool enable) {
      if (self == null) {
         return;
      }

      self._isInputEnabled = enable;
   }

   public static bool isInputEnabled () {
      if (self == null) {
         return true;
      }

      return self._isInputEnabled;
   }

   #region Private Variables

   // A list of keys that will be ignored when getting inputs
   private List<Key> _disabledKeys = new List<Key>();

   // Gets set to true when a direction movement is being simulated
   private static bool _isMoveSimulated = false;

   // The movement direction that is being simulated
   private static Direction _moveDirectionSimulated = Direction.East;

   // Is input enabled?
   private bool _isInputEnabled = true;

   #endregion
}
