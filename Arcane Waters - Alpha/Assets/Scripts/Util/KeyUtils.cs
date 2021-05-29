using UnityEngine.InputSystem;

// TODO: Cleanup this script after confirming cloud server fix
public static class KeyUtils {

   #region Keyboard keys

   public static bool GetKey (Key key) {
      if (Util.isBatch()) {
         return false;
      }

      if (key == Key.None) {
         return false;
      }

      try {
         return Keyboard.current[key].isPressed;
      } catch {
         // TODO: Remove after input system polishing
         D.debug("Invalid key to press: " + key);
         return false;
      }
   }

   public static bool GetKeyDown (Key key) {
      if (Util.isBatch()) {
         return false;
      }

      if (key == Key.None) {
         return false;
      }

      try {
         return Keyboard.current[key].wasPressedThisFrame;
      } catch {
         // TODO: Remove after input system polishing
         D.debug("Invalid key to press down: " + key);
         return false;
      }
   }

   public static bool GetKeyUp (Key key) {
      if (Util.isBatch()) {
         return false;
      }

      if (key == Key.None) {
         return false;
      }

      try {
         return Keyboard.current[key].wasReleasedThisFrame;
      } catch {
         // TODO: Remove after input system polishing
         D.debug("Invalid key to release: " + key);
         return false;
      }
   }

   public static bool GetEnterKey() {
      return GetKey(Key.Enter) || GetKey(Key.NumpadEnter);
   }

   public static bool GetEnterKeyDown () {
      return GetKeyDown(Key.Enter) || GetKeyDown(Key.NumpadEnter);
   }

   public static bool GetEnterKeyUp () {
      return GetKeyUp(Key.Enter) || GetKeyUp(Key.NumpadEnter);
   }

   #endregion

   #region Mouse keys

   public static bool GetButton (MouseButton mouseButtonKey) {
      if (Util.isBatch()) {
         return false;
      }

      switch (mouseButtonKey) {
         case MouseButton.Left:
            return Mouse.current.leftButton.isPressed;
         case MouseButton.Right:
            return Mouse.current.rightButton.isPressed;
      }
      return false;
   }

   public static bool GetButtonDown (MouseButton mouseButtonKey) {
      if (Util.isBatch()) {
         return false;
      }

      switch (mouseButtonKey) {
         case MouseButton.Left:
            return Mouse.current.leftButton.wasPressedThisFrame;
         case MouseButton.Right:
            return Mouse.current.rightButton.wasPressedThisFrame;
      }
      return false;
   }

   public static bool GetButtonUp (MouseButton mouseButtonKey) {
      if (Util.isBatch()) {
         return false;
      }

      switch (mouseButtonKey) {
         case MouseButton.Left:
            return Mouse.current.leftButton.wasReleasedThisFrame;
         case MouseButton.Right:
            return Mouse.current.rightButton.wasReleasedThisFrame;
      }
      return false;
   }

   #endregion
}