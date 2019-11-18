using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

public class ItemCellInventory : ItemCell
{

   #region Public Variables

   #endregion

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

   #region Private Variables

   // The time since the last left click on this cell
   private float _lastClickTime = float.MinValue;

   // The delay between two clicks to be considered a double click
   private float DOUBLE_CLICK_DELAY = 0.5f;

   #endregion
}