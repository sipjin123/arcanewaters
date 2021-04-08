using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.InputSystem;

public class NewInputTester : MonoBehaviour {
   #region Public Variables

   // Input Master reference
   public InputMaster inputMaster;
   public Vector2 moveVector;

   #endregion

   private void Start () {
      D.debug("Keybind new input system");

      // TODO: Setup all gamepad action keybindings here after stabilizing the project by overridding all scripts referencing legacy input system
      inputMaster = new InputMaster();

      inputMaster.Player.Enable();
      inputMaster.Player.Jump.performed += func => jumpAction();
      inputMaster.Player.Interact.performed += func => interactAction();

      InputSystem.onDeviceChange += (device, change) =>
      {
         switch (change) {
            case InputDeviceChange.Added:
               Debug.Log("New device added: " + device);
               break;

            case InputDeviceChange.Removed:
               Debug.Log("Device removed: " + device);
               break;
         }
      };


      inputMaster.Player.Move.performed += func => moveAction(func.ReadValue<Vector2>());
      inputMaster.Player.Move.canceled += func => moveAction(new Vector2(0, 0), true);

      inputMaster.Player.MouseControl.performed += mfunc => mouseAction(mfunc.ReadValue<Vector2>());
      inputMaster.Player.MouseControl.canceled += mfunc => mouseAction(new Vector2(0, 0));
   }

   private void mouseAction (Vector2 mouseVal) {
      D.debug("Mouse Move Now: ");
   }

   private void jumpAction () {
      transform.position = Vector2.zero;
      D.debug("Jump!");
   }

   private void interactAction () {
      D.debug("Interact!");
   }

   private void Update () {
      float speed = 2.25f;
      transform.position += (Vector3) moveVector * speed * Time.deltaTime;

   }

   private void moveAction (Vector2 moveFactor, bool isCancelled = false) {
      if (isCancelled)
         D.debug("Cancel!");
      D.debug("Move!" + moveFactor);
      moveVector = moveFactor;

      return;
   }

   #region Private Variables

   #endregion
}
