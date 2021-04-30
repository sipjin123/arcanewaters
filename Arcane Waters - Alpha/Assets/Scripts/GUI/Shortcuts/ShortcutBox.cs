using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using TMPro;

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

   // The tooltip panel
   public GameObject toolTipPanel;

   // The text of the tooltip
   public TextMeshProUGUI toolTipText;

   // Whether the item is selected
   public bool isSelected;

   // The sprite shown for the selected item
   public Sprite selectedSprite;

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
      _containerImage.sprite = InventoryManager.isEquipped(_itemCell.getItem().id) ? selectedSprite : ImageManager.self.blankSprite;
   }

   public void onShortcutPress () {
      if (_itemCell != null && button.interactable) {
         SoundEffectManager.self.playSoundEffect(SoundEffectManager.SHORTCUT_SELECTION, transform);
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

   public void setItem(Item item) {
      clear();

      _itemCell = Instantiate(itemCellPrefab, itemCellContainer.transform, false);
      _itemCell.setCellForItem(item);

      if (item.itemTypeId != 0 && item.id != 0) {
         _itemCell.disablePointerEvents();
         _itemCell.hideBackground();
         _itemCell.hideItemCount();
         _itemCell.hideSelectedBox();

         toolTipText.SetText(_itemCell.getItem().getName());
      }
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

   #region Private Variables

   // Our associated Button
   protected Button _button;

   // Our container image
   protected Image _containerImage;

   // The item cell contained in the shortcut box
   protected ItemCell _itemCell = null;

   #endregion
}
