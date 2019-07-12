using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class MouseManager : ClientMonoBehaviour {
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

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;
   }

   private void Update () {
      // Check if the mouse is over interactable objects
      bool isOverInteractableObject = isMouseOverSomething();

      // Determine our mouse texture based on whether or not we're pressing a button
      bool isPressing = Input.GetMouseButton(0) || Input.GetMouseButton(1);
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
      // Clear out the old list of boxes being hovered over
      _boxesBeingHovered.Clear();

      // Cycle over all of the things that mouse is over
      foreach (RaycastResult result in raycastMouse()) {
         if (result.isValid) {
            // Check if we're hovering over a clickable box
            ClickableBox clickableBox = result.gameObject.GetComponent<ClickableBox>();

            // If so, keep track of it
            if (clickableBox != null) {
               _boxesBeingHovered.Add(clickableBox);
               return true;
            }

            // Otherwise, check if we're  hoving over a selectable or toggle
            Selectable selectable = result.gameObject.GetComponent<Selectable>();
            Toggle toggle = result.gameObject.GetComponentInParent<Toggle>();

            if (selectable != null || toggle != null) {
               return true;
            }
         }
      }

      return false;
   }

   public List<RaycastResult> raycastMouse () {
      PointerEventData pointerData = new PointerEventData(EventSystem.current) {
         pointerId = -1,
      };
      pointerData.position = Input.mousePosition;
      
      List<RaycastResult> results = new List<RaycastResult>();
      EventSystem.current.RaycastAll(pointerData, results);

      return results;
   }

   public bool isHoveringOver (ClickableBox box) {
      return _boxesBeingHovered.Contains(box);
   }

   #region Private Variables

   // The List of Clickable Boxes that we're currently hovering over
   protected List<ClickableBox> _boxesBeingHovered = new List<ClickableBox>();

   #endregion
}
