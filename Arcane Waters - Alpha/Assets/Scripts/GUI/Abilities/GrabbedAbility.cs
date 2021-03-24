using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.InputSystem;

public class GrabbedAbility : MonoBehaviour {
   #region Public Variables

   #endregion

   public void activate () {
      gameObject.SetActive(true);

      // Place under the mouse
      transform.position = Mouse.current.position.ReadValue();
   }

   public void Update () {
      // Follow the mouse
      transform.position = Mouse.current.position.ReadValue();

      // Stop grabbing when the right click button is pressed
      if (Mouse.current.rightButton.isPressed) {
         AbilityPanel.self.stopGrabbingAbility();
         return;
      }

      // When the left click button is released, try to drop the ability
      if (!Mouse.current.leftButton.isPressed) {
         AbilityPanel.self.tryDropGrabbedAbility(Mouse.current.position.ReadValue());
         return;
      }
   }

   public void deactivate () {
      if (gameObject.activeSelf) {
         gameObject.SetActive(false);
      }
   }

   #region Private Variables

   #endregion
}