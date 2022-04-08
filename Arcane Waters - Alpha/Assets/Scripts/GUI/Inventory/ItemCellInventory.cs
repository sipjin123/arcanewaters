using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ItemCellInventory : ItemCell, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler {

   #region Public Variables

   // The equipment type of this item cell
   public EquipmentType equipmentType;

   // The canvas group component
   public CanvasGroup canvasGroup;

   #endregion

   public override void clear () {
      base.clear();
      hide();

      if (equipmentType != EquipmentType.None) {
         canvasGroup.alpha = 1f;
         canvasGroup.interactable = false;
         icon.enabled = true;
         iconShadow.enabled = true;
      }
      switch (equipmentType) {
         case EquipmentType.Weapon:
            icon.sprite = EquipmentXMLManager.self.blankWeaponIcon;
            iconShadow.sprite = EquipmentXMLManager.self.blankWeaponIcon;
            break;
         case EquipmentType.Armor:
            icon.sprite = EquipmentXMLManager.self.blankArmorIcon;
            iconShadow.sprite = EquipmentXMLManager.self.blankArmorIcon;
            break;
         case EquipmentType.Hat:
            icon.sprite = EquipmentXMLManager.self.blankHatIcon;
            iconShadow.sprite = EquipmentXMLManager.self.blankHatIcon;
            break;
         case EquipmentType.Ring:
            icon.sprite = EquipmentXMLManager.self.blankRingIcon;
            iconShadow.sprite = EquipmentXMLManager.self.blankRingIcon;
            break;
         case EquipmentType.Necklace:
            icon.sprite = EquipmentXMLManager.self.blankNecklaceIcon;
            iconShadow.sprite = EquipmentXMLManager.self.blankNecklaceIcon;
            break;
         case EquipmentType.Trinket:
            icon.sprite = EquipmentXMLManager.self.blankTrinketIcon;
            iconShadow.sprite = EquipmentXMLManager.self.blankTrinketIcon;
            break;
      }
   }

   public override void setCellForItem (Item item) {
      base.setCellForItem(item);
      show();
   }

   public void OnPointerEnter (PointerEventData eventData) {
      if (_interactable) {
         onPointerEnter?.Invoke();
         SoundEffectManager.self.playFmodSfx(SoundEffectManager.HOVER_CURSOR_ITEMS, transform.position);
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
            StartCoroutine(trackDrag(MouseUtils.mousePosition));
         }
      }
   }

   private IEnumerator trackDrag (Vector3 clickPosition) {
      float sqrDistance = 0;

      // Check if the mouse left button is still pressed
      while (KeyUtils.GetButton(MouseButton.Left)) {
         // Calculate the squared distance between the mouse click and the current mouse position
         sqrDistance = Vector3.SqrMagnitude((Vector3)MouseUtils.mousePosition - clickPosition);

         // Check if the distance is large enough
         if (sqrDistance > DISTANCE_UNTIL_START_DRAG) {
            // Begin the drag process
            onDragStarted?.Invoke();

            //SoundEffectManager.self.playSoundEffect(SoundEffectManager.INVENTORY_DRAG_START, transform);

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