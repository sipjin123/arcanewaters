using UnityEngine;
using UnityEngine.InputSystem;

public static class MouseUtils {
   public static Vector2 mousePosition  {
      get {
         if (Util.isBatch()) {
            return new Vector2(0, 0);
         }

         return Mouse.current.position.ReadValue();
      }
   }

   public static float mouseScrollY {
      get {
         if (Util.isBatch()) {
            return 0;
         }

         if (Mouse.current.scroll.y.ReadValue() > 0) {
            return 1;
         }  else if (Mouse.current.scroll.y.ReadValue() < 0) {
            return -1;
         }

         return 0;
      }
   }
}