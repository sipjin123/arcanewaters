using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class ShortcutBox : MonoBehaviour, IPointerClickHandler
{
   #region Public Variables

   // The number associated with this shortcut
   public int slotNumber = 0;

   // The prefab we use for creating item cells
   public ItemCell itemCellPrefab;

   // The container of the item cell
   public GameObject itemCellContainer;

   // The box button
   public Button button;

   // The zone where grabbed items can be dropped
   public ItemDropZone dropZone;

   #endregion

   private void Start () {
      // Look up components
      _button = GetComponent<Button>();
      _containerImage = GetComponent<Image>();

      itemCellContainer.DestroyChildren();
   }

   private void Update () {
      if (_itemCell == null || _itemCell.getItem() == null) {
         return;
      }

      // Make the box highlighted if we've equipped the associated weapon
      _containerImage.color = Color.white;
      if (InventoryManager.isEquipped(_itemCell.getItem().id)) {
         _containerImage.color = Util.getColor(255, 160, 160);
      }
   }

   public void onShortcutPress () {
      if (_itemCell != null && button.interactable) {
         InventoryManager.tryEquipOrUseItem(_itemCell.getItem());
      }
   }

   public virtual void OnPointerClick (PointerEventData eventData) {
      if (_itemCell != null && ((InventoryPanel) PanelManager.self.get(Panel.Type.Inventory)).isShowing()
         && eventData.button == PointerEventData.InputButton.Right) {
         Global.player.rpc.Cmd_DeleteItemShortcut(slotNumber);
      }
   }

   public void setItem(Item item) {
      clear();

      _itemCell = Instantiate(itemCellPrefab, itemCellContainer.transform, false);
      _itemCell.setCellForItem(item);

      _itemCell.disablePointerEvents();
      _itemCell.hideBackground();
      _itemCell.hideItemCount();
      _itemCell.hideSelectedBox();
   }

   public void clear () {
      itemCellContainer.DestroyChildren();
      _itemCell = null;
      _containerImage.color = Color.white;
   }

   public bool isInDropZone (Vector2 screenPoint) {
      return dropZone.isInZone(screenPoint);
   }

   #region Private Variables

   // Our associated Button
   protected Button _button;

   // Our container image
   protected Image _containerImage;

   // The item cell contained in the shortcut box
   protected ItemCell _itemCell = null;

   #endregion
}
