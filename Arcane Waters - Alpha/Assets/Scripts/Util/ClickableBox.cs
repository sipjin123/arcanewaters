using System;
using UnityEngine;

public enum MouseButton
{
   Left = 0,
   Right = 1
}

public class ClickableBox : MonoBehaviour {
   #region Public Variables

   // An event triggered when a mouse button is pressed while hovering the box. Includes the used mouse button.
   public event Action<MouseButton> mouseButtonDown;

   // An event triggered when a mouse button is released while hovering the box. Includes the used mouse button.
   public event Action<MouseButton> mouseButtonUp;

   #endregion

   public void onMouseButtonDown (MouseButton button) {
      mouseButtonDown?.Invoke(button);
   }

   public void onMouseButtonUp (MouseButton button) {
      mouseButtonUp?.Invoke(button);
   }

   #region Private Variables
      
   #endregion
}
