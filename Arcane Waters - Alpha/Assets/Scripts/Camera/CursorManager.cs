using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CursorManager : MonoBehaviour {
   #region Public Variables

   // The texture we want to use
   public Texture2D tex;

   #endregion

   private void Awake () {
      // We have to use software mode in order to use a large cursor
      // Cursor.SetCursor(tex, Vector2.zero, CursorMode.ForceSoftware);
   }

   #region Private Variables

   #endregion
}
