using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class GrabbedAbility : MonoBehaviour {
   #region Public Variables

   #endregion

   public void activate () {
      gameObject.SetActive(true);

      // Place under the mouse
      transform.position = Input.mousePosition;
   }

   public void Update () {
      // Follow the mouse
      transform.position = Input.mousePosition;

      // Stop grabbing when the right click button is pressed
      if (Input.GetMouseButtonDown(1)) {
         AbilityPanel.self.stopGrabbingAbility();
         return;
      }

      // When the left click button is released, try to drop the ability
      if (!Input.GetMouseButton(0)) {
         AbilityPanel.self.tryDropGrabbedAbility(Input.mousePosition);
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