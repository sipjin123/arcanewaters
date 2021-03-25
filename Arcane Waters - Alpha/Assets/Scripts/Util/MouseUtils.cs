using UnityEngine;
using UnityEngine.InputSystem;

public static class MouseUtils {
   public static Vector2 mousePosition  {
      get {
         return Mouse.current.position.ReadValue();
      }
   }
}