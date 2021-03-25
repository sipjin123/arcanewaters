using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ItemCellInventory : ItemCell, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{

   #region Public Variables

   // The canvas group component
   public CanvasGroup canvasGroup;

   #endregion

   public override void clear () {
      base.clear();
      hide();
   }

   public override void setCellForItem (Item item) {
      base.setCellForItem(item);
      show();
   }

   public void OnPointerEnter (PointerEventData eventData) {
      if (_interactable) {
         onPointerEnter?.Invoke();
      }
   }

   public void OnPointerExit (PointerEventData eventData) {
      if (_interactable) {
         onPointerExit?.Invoke();
      }
   }

   public void OnPointerDown (PointerEventData eventData) {
      if (_interactable) {
         if (eventData.button == PointerEventData.InputButton.Left) {
            // Wait until the mouse has moved a little before start dragging
            // This is to avoid interference with double click
            StartCoroutine(trackDrag(KeyUtils.getMousePosition()));
         }
      }
   }

   private IEnumerator trackDrag (Vector3 clickPosition) {
      float sqrDistance = 0;

      // Check if the mouse left button is still pressed
      while (KeyUtils.isLeftButtonPressed()) {
         // Calculate the squared distance between the mouse click and the current mouse position
         sqrDistance = Vector3.SqrMagnitude((Vector3)KeyUtils.getMousePosition() - clickPosition);

         // Check if the distance is large enough
         if (sqrDistance > DISTANCE_UNTIL_START_DRAG) {
            // Begin the drag process
            onDragStarted?.Invoke();
            break;
         }
         yield return null;
      }
   }

   public void show() {
      Util.enableCanvasGroup(canvasGroup);
   }

   public void hide () {
      Util.disableCanvasGroup(canvasGroup);
   }

   #region Private Variables

   // The distance the mouse must move for the dragging process to begin (squared distance)
   private float DISTANCE_UNTIL_START_DRAG = 0.05f * 0.05f;

   #endregion
}