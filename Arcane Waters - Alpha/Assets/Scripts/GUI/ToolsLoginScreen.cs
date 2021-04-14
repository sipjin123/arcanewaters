using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.InputSystem;

public class ToolsLoginScreen : MonoBehaviour {
   #region Public Variables

   // The account name field
   public InputField accountInputField;

   // The password field
   public InputField passwordInputField;

   // The login button
   public Button loginButton;

   #endregion

   private void Awake () {
      _canvas = GetComponentInParent<Canvas>();
   }

   private void Update () {
      if (_canvas.enabled) {
         // If they press Enter in the password field, activate the Play button
         if (KeyUtils.GetKeyDown(Key.Enter) && Util.isSelected(passwordInputField) && passwordInputField.text != "" && passwordInputField.text.Length > 0 && accountInputField.text.Length > 0) {
            Util.clickButton(loginButton);
         }

         // Check for an assortment of keys
         bool moveToNextField = KeyUtils.GetKeyDown(Key.Tab) || KeyUtils.GetKeyDown(Key.Enter) || KeyUtils.GetKeyDown(Key.DownArrow);

         // If we're in the account field, let us move to the password field
         if (moveToNextField && Util.isSelected(accountInputField)) {
            Util.select(passwordInputField);
         }
      }
   }

   #region Private Variables

   // The canvas that is parent of this object
   private Canvas _canvas;

   #endregion
}
