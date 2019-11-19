using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

public class ItemCellInventory : ItemCell, IPointerDownHandler
{

   #region Public Variables

   // The background image
   public Image backgroundImage;

   // The canvas group component
   public CanvasGroup canvasGroup;

   #endregion

   public void Awake () {
      _normalBackgroundSprite = backgroundImage.sprite;
   }

   public override void OnPointerClick (PointerEventData eventData) {
      if (_interactable) {
         if (eventData.button == PointerEventData.InputButton.Left) {
            // Determine if two clicks where close enough in time to be considered a double click
            if (Time.time - _lastClickTime < DOUBLE_CLICK_DELAY) {
               InventoryPanel.self.tryEquipOrUseItem(getItem());
            } else {
               InventoryPanel.self.hideContextMenu();
            }
            _lastClickTime = Time.time;
         } else if (eventData.button == PointerEventData.InputButton.Right) {
            // Show the context menu
            InventoryPanel.self.showContextMenu(this);
         }
      }
   }

   public void OnPointerDown (PointerEventData eventData) {
      if (_interactable) {
         if (eventData.button == PointerEventData.InputButton.Left) {
            InventoryPanel.self.hideContextMenu();

            // Wait until the mouse has moved a little before start dragging
            // This is to avoid interference with double click
            StartCoroutine(trackDrag(Input.mousePosition));
         }
      }
   }

   private IEnumerator trackDrag (Vector3 clickPosition) {
      float sqrDistance = 0;

      // Check if the mouse left button is still pressed
      while (Input.GetMouseButton(0)) {
         // Calculate the squared distance between the mouse click and the current mouse position
         sqrDistance = Vector3.SqrMagnitude(Input.mousePosition - clickPosition);

         // Check if the distance is large enough
         if (sqrDistance > DISTANCE_UNTIL_START_DRAG) {
            // Begin the drag process
            InventoryPanel.self.tryGrabItem(this);
            break;
         }
         yield return null;
      }
   }

   public void show() {
      canvasGroup.alpha = 1f;
   }

   public void hide () {
      canvasGroup.alpha = 0f;
   }

   #region Private Variables

   // The time since the last left click on this cell
   private float _lastClickTime = float.MinValue;

   // The background sprite when the cell is not highlighted
   private Sprite _normalBackgroundSprite;

   // The delay between two clicks to be considered a double click
   private float DOUBLE_CLICK_DELAY = 0.5f;

   // The distance the mouse must move for the dragging process to begin (squared distance)
   private float DISTANCE_UNTIL_START_DRAG = 0.05f * 0.05f;

   #endregion
}