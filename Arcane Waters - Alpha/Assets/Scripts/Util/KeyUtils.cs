using UnityEngine;
using UnityEngine.InputSystem;

public static class KeyUtils {
   // Keyboard keys
   public static bool GetKey (Key key) {
      return Keyboard.current[key].isPressed;
   }

   public static bool GetKeyDown (Key key) {
      return Keyboard.current[key].wasPressedThisFrame;
   }

   public static bool GetKeyUp (Key key) {
      return Keyboard.current[key].wasReleasedThisFrame;
   }

   // Left mouse button
   public static bool isLeftButtonPressed () {
      return Mouse.current.leftButton.isPressed;
   }

   public static bool isLeftButtonPressedDown () {
      return Mouse.current.leftButton.wasPressedThisFrame;
   }

   public static bool isLeftButtonPressedUp () {
      return Mouse.current.leftButton.wasReleasedThisFrame;
   }

   // Right mouse button
   public static bool isRightButtonPressed () {
      return Mouse.current.rightButton.isPressed;
   }

   public static bool isRightButtonPressedDown () {
      return Mouse.current.rightButton.wasPressedThisFrame;
   }

   public static bool isRightButtonPressedUp () {
      return Mouse.current.rightButton.wasReleasedThisFrame;
   }

   // Mouse Utilities
   public static Vector2 getMousePosition () {
      return Mouse.current.position.ReadValue();
   }
}