using UnityEngine;
using UnityEngine.InputSystem;

public static class MouseUtils {
   public static Vector2 mousePosition  {
      get {
         if (Util.isBatch()) {
            return new Vector2(0, 0);
         }
         #if !(IS_SERVER_BUILD && CLOUD_BUILD)
         return Mouse.current.position.ReadValue();
         #else
         return new Vector2(0, 0);
         #endif
      }
   }
}