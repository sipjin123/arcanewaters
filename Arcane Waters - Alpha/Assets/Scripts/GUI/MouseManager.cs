using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class MouseManager : ClientMonoBehaviour
{
   #region Public Variables

   // Various mouse settings
   public Texture2D defaultCursorTexture;
   public Texture2D pressedCursorTexture;
   public Texture2D defaultHandTexture;
   public Texture2D pressedHandTexture;
   public CursorMode cursorMode = CursorMode.Auto;
   public Vector2 normalHotSpot = Vector2.zero;
   public Vector2 handHotSpot = Vector2.zero;

   // Self
   public static MouseManager self;

   // Pointer event for mouse hover
   public PointerEventData pointerEventData;

   #endregion

   protected override void Awake () {
      D.adminLog("MouseManager.Awake...", D.ADMIN_LOG_TYPE.Initialization);
      base.Awake();
      self = this;
      D.adminLog("MouseManager.Awake: OK", D.ADMIN_LOG_TYPE.Initialization);
   }

   private void Update () {
      // Check if the mouse is over interactable objects
      bool isOverInteractableObject = isMouseOverSomething();

      // Let the box know if it's been clicked
      if (_boxBeingHovered != null) {
         if (KeyUtils.GetButtonDown(MouseButton.Left)) {
            _boxBeingHovered.onMouseButtonDown(MouseButton.Left);
         } else if (KeyUtils.GetButtonUp(MouseButton.Left)) {
            _boxBeingHovered.onMouseButtonUp(MouseButton.Left);
         }
         
         if (KeyUtils.GetButtonDown(MouseButton.Right)) {
            _boxBeingHovered.onMouseButtonDown(MouseButton.Right);
         } else if (KeyUtils.GetButtonUp(MouseButton.Right)) {
            _boxBeingHovered.onMouseButtonUp(MouseButton.Right);
         }
      }

      // Determine our mouse texture based on whether or not we're pressing a button
      bool isPressing = KeyUtils.GetButton(MouseButton.Left) || KeyUtils.GetButton(MouseButton.Right);
      Vector2 hotSpot = isOverInteractableObject ? handHotSpot : normalHotSpot;
      Texture2D texToUse = defaultCursorTexture;

      // When an interactable is being hovered by the mouse, we show a hand
      if (isOverInteractableObject && isPressing) {
         texToUse = pressedHandTexture;
      } else if (isOverInteractableObject && !isPressing) {
         texToUse = defaultHandTexture;
      } else if (!isOverInteractableObject && isPressing) {
         texToUse = pressedCursorTexture;
      }

      Cursor.SetCursor(texToUse, hotSpot, cursorMode);
   }

   public bool isMouseOverSomething () {
      _boxBeingHovered = null;
      GameObject gameObjectUnderMouse = null;
      List<GameObject> gameObjectsUnderMouseList = new List<GameObject>();

      pointerEventData = new PointerEventData(EventSystem.current);
      pointerEventData.position = MouseUtils.mousePosition;

      // Create a list of Raycast Results
      List<RaycastResult> results = new List<RaycastResult>();
      EventSystem.current.RaycastAll(pointerEventData, results);

      // Search for clickable box
      foreach (RaycastResult result in results) {
         if (result.gameObject.GetComponent<ClickableBox>()) {
            gameObjectsUnderMouseList.Add(result.gameObject);
         }
      }

      foreach (GameObject gameObject in gameObjectsUnderMouseList) {
         if (gameObjectUnderMouse == null || gameObject.transform.position.z < gameObjectUnderMouse.transform.position.z) {
            gameObjectUnderMouse = gameObject;
         }
      }

      if (gameObjectUnderMouse == null) {
         return false;
      }

      // Only consider clickable boxes if no panel is opened
      if (!PanelManager.self.hasPanelInLinkedList()) {
         // Check if we're hovering over a clickable box
         _boxBeingHovered = gameObjectUnderMouse.GetComponent<ClickableBox>();

         if (_boxBeingHovered != null) {
            return true;
         }
      }

      // Otherwise, check if we're  hoving over a selectable, toggle or slider
      Selectable selectable = gameObjectUnderMouse.GetComponent<Selectable>();
      Toggle toggle = gameObjectUnderMouse.GetComponentInParent<Toggle>();
      Slider slider = gameObjectUnderMouse.GetComponentInParent<Slider>();

      if (selectable != null || toggle != null || slider != null) {
         return true;
      }

      return false;
   }

   public bool isHoveringOver (ClickableBox box) {
      return _boxBeingHovered == box;
   }

   #region Private Variables

   // The clickable box that we're currently hovering over
   protected ClickableBox _boxBeingHovered = null;

   #endregion
}
