using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using TMPro;

public class ShortcutBox : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler
{
   #region Public Variables

   // The number associated with this shortcut
   public int slotNumber = 0;

   // The prefab we use for creating item cells
   public ItemCellInventory itemCellPrefab;

   // The container of the item cell
   public GameObject itemCellContainer;

   // The box button
   public Button button;

   // The zone where grabbed items can be dropped
   public ItemDropZone dropZone;

   // The tooltip panel
   public GameObject toolTipPanel;

   // The text of the tooltip
   public TextMeshProUGUI toolTipText;

   // Whether the item is selected
   public bool isSelected;

   // The sprite shown for the selected item
   public Sprite selectedSprite;

   // The common object used to display any grabbed item
   public GrabbedItem grabbedItem;

   #endregion

   private void Start () {
      // Look up components
      _button = GetComponent<Button>();
      _containerImage = GetComponent<Image>();

      itemCellContainer.DestroyChildren();

      stopGrabbingItem();
   }

   private void Update () {
      // Make the box highlighted if we've equipped the associated weapon
      _containerImage.sprite = _itemCell != null && _itemCell.getItem() != null && InventoryManager.isEquipped(_itemCell.getItem().id) 
         ? selectedSprite : ImageManager.self.blankSprite;
   }

   public void onShortcutPress () {
      if (_itemCell != null && button.interactable && _grabbedItemCell == null) {
         //SoundEffectManager.self.playSoundEffect(SoundEffectManager.SHORTCUT_SELECTION, transform);

         InventoryManager.tryEquipOrUseItem(_itemCell.getItem());
      }
   }

   public virtual void OnPointerClick (PointerEventData eventData) {
      if (_itemCell != null && ((InventoryPanel) PanelManager.self.get(Panel.Type.Inventory)).isShowing()
         && eventData.button == PointerEventData.InputButton.Right) {
         Global.player.rpc.Cmd_DeleteItemShortcut(slotNumber);
      }
   }

   public void onPointerEnter () {
      if (_itemCell != null) {
         toolTipPanel.SetActive(true);
      }
   }

   public void onPointerExit () {
      toolTipPanel.SetActive(false);
   }

   public void setItem (Item item) {
      clear();

      _itemCell = Instantiate(itemCellPrefab, itemCellContainer.transform, false);
      _itemCell.setCellForItem(item);

      if (item.itemTypeId != 0 && item.id != 0) {
         _itemCell.hideBackground();
         _itemCell.hideSelectedBox();

         subscribeClickEventsForCell(_itemCell);

         toolTipText.SetText(_itemCell.getItem().getName());
      }
   }

   private void subscribeClickEventsForCell (ItemCell cell) {
      if (cell == null) {
         D.error("Cannot subscribe click events because cell is null");
         return;
      }

      // Set the cell click events
      cell.leftClickEvent.RemoveAllListeners();
      cell.rightClickEvent.RemoveAllListeners();
      cell.doubleClickEvent.RemoveAllListeners();
      cell.onPointerEnter.RemoveAllListeners();
      cell.onPointerExit.RemoveAllListeners();
      cell.onDragStarted.RemoveAllListeners();

      cell.shiftClickEvent.AddListener(() => {
         if (ChatPanel.self != null) {
            ChatPanel.self.addItemInsertToInput(cell.getItem());
         }
      });

      cell.onDragStarted.AddListener(() => tryGrabItem(cell as ItemCellInventory));
   }

   public void tryGrabItem (ItemCellInventory itemCell) {
      if (itemCell == null || !PanelManager.self.get<InventoryPanel>(Panel.Type.Inventory).isShowing()) {
         return;
      }

      Item castedItem = itemCell.getItem();

      // Only equippable items can be grabbed
      if (castedItem.itemTypeId != 0 && castedItem.id != 0 && castedItem.canBeEquipped()) {
         _grabbedItemCell = itemCell;

         // Hide the cell being grabbed
         _grabbedItemCell.hide();

         // Initialize the common grabbed object
         grabbedItem.activate(castedItem, itemCell.getItemSprite(), tryDropGrabbedItem);
      }
   }

   public void tryDropGrabbedItem (Vector2 screenPosition) {
      if (_grabbedItemCell != null) {
         if (!PanelManager.self.get<InventoryPanel>(Panel.Type.Inventory).isShowing()) {
            stopGrabbingItem();
            return;
         }

         // Check if the item was dropped over a shortcut slot
         ShortcutBox box = PanelManager.self.itemShortcutPanel.getShortcutBoxAtPosition(screenPosition);
         if (box != null) {
            // If the box is not us, swap our items
            if (box.slotNumber != slotNumber) {
               Global.player.rpc.Cmd_SwapItemShortcut(slotNumber, box.slotNumber);
            }
            stopGrabbingItem();
            return;
         }

         // Items can also be dragged from shortcut to inventory, resulting in rebinding of item
         bool droppedInInventory = PanelManager.self.get<InventoryPanel>(Panel.Type.Inventory).inventoryDropZone.isInZone(screenPosition);

         if (droppedInInventory) {
            Global.player.rpc.Cmd_DeleteItemShortcut(slotNumber);

            stopGrabbingItem();
            return;
         }

         // Otherwise, simply stop grabbing
         stopGrabbingItem();
      }
   }

   public void stopGrabbingItem () {
      if (_grabbedItemCell != null) {
         // Restore the grabbed cell
         _grabbedItemCell.show();
         _grabbedItemCell = null;
      }

      // Deactivate the grabbed item
      grabbedItem.deactivate();
   }

   public void clear () {
      itemCellContainer.DestroyChildren();
      _itemCell = null;
      _containerImage.color = Color.white;
      toolTipText.SetText("");
   }

   public bool isInDropZone (Vector2 screenPoint) {
      return dropZone.isInZone(screenPoint);
   }

   public void OnPointerDown (PointerEventData eventData) {
      if (_itemCell != null) {
         _itemCell.OnPointerDown(eventData);
      }
   }

   public void OnPointerEnter (PointerEventData eventData) {
      if (_itemCell != null) {
         _itemCell.OnPointerEnter(eventData);
      }
   }

   public void OnPointerExit (PointerEventData eventData) {
      if (_itemCell != null) {
         _itemCell.OnPointerExit(eventData);
      }
   }

   #region Private Variables

   // Our associated Button
   protected Button _button;

   // Our container image
   protected Image _containerImage;

   // The item cell contained in the shortcut box
   protected ItemCellInventory _itemCell = null;

   // The cell from which an item was grabbed
   private ItemCellInventory _grabbedItemCell;

   #endregion
}
