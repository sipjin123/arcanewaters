using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class ItemRow : MonoBehaviour, IPointerClickHandler {
   #region Public Variables

   // The components we manage
   public Image itemIcon;
   public Text itemText;
   public Image colorBox1;
   public Image colorBox2;
   public Image equippedIcon;
   public Text itemCount;
   public Image selectedBackgroundImage;

   // The item associated with this row
   public Item item;

   // The tooltip on the icon
   public Tooltipped tooltip;

   #endregion

   public void Update () {
      selectedBackgroundImage.color = (InventoryPanel.selectedItemId == item.id) ? Color.white : Color.clear;
   }

   public void OnPointerClick (PointerEventData eventData) {
      // If this row is clicked on, update the currently selected item
      if (InventoryPanel.selectedItemId == item.id) {
         InventoryPanel.selectedItemId = 0;
      } else {
         InventoryPanel.selectedItemId = item.id;

         // Show the item in the preview panel
         // InventoryPanel.self.previewPanel.showItem(item);
      }
   }

   #region Private Variables

   #endregion
}
