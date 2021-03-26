using UnityEngine.InputSystem;

// TODO: Cleanup this script after confirming cloud server fix
public static class KeyUtils {

   #region Keyboard keys

   public static bool GetKey (Key key) {
      if (Util.isBatch()) {
         return false;
      }

      return Keyboard.current[key].isPressed;
   }

   public static bool GetKeyDown (Key key) {
      if (Util.isBatch()) {
         return false;
      }

      return Keyboard.current[key].wasPressedThisFrame;
   }

   public static bool GetKeyUp (Key key) {
      if (Util.isBatch()) {
         return false;
      }

      return Keyboard.current[key].wasReleasedThisFrame;
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