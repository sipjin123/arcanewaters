﻿using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

public class ItemCell : MonoBehaviour, IPointerClickHandler
{

   #region Public Variables

   // The icon of the item
   public Image icon;

   // The layout element component on the icon
   public LayoutElement iconLayoutElement;

   // The text containing the amount of stacked items
   public TextMeshProUGUI itemCountText;

   // The box visible when the cell is selected
   public Image selectedBox;

   // The tooltip showing the name of the item
   public Tooltipped tooltip;

   // The recolored sprite component on the icon
   public RecoloredSprite recoloredSprite;

   // The default icons, used when it is not defined by the item type
   public Sprite foodIcon;
   public Sprite defaultItemIcon;

   #endregion

   public void setCellForItem (Item item) {
      setCellForItem(item, item.count);
   }

   public void setCellForItem (Item item, int count) {
      // Retrieve the icon sprite and coloring depending on the type
      ColorKey colorKey = null;
      switch (item.category) {
         case Item.Category.Weapon:
            icon.sprite = ImageManager.getSprite(item.getIconPath());
            colorKey = new ColorKey(Global.player.gender, (Weapon.Type) item.itemTypeId);
            break;
         case Item.Category.Armor:
         case Item.Category.Helm:
            icon.sprite = ImageManager.getSprite(item.getIconPath());
            colorKey = new ColorKey(Global.player.gender, (Armor.Type) item.itemTypeId);
            break;
         case Item.Category.Potion:
            icon.sprite = foodIcon;
            break;
         case Item.Category.Usable:
            icon.sprite = defaultItemIcon;
            break;
         case Item.Category.CraftingIngredients:
            icon.sprite = ImageManager.getSprite(item.getIconPath());

            // Expand the size of the icon to fill the cell
            iconLayoutElement.preferredHeight = 60;
            iconLayoutElement.preferredWidth = 60;
            break;
         case Item.Category.Blueprint:
            icon.sprite = ImageManager.getSprite(item.getIconPath());
            break;
         default:
            icon.sprite = defaultItemIcon;
            break;
      }

      // Recolor
      if (colorKey != null) {
         recoloredSprite.recolor(colorKey, item.color1, item.color2);
      }

      // Show the item count when relevant
      if (count > 1) {
         itemCountText.SetText(count.ToString());
      } else {
         itemCountText.gameObject.SetActive(false);
      }

      // Set the tooltip
      tooltip.text = item.getTooltip();

      // Saves the item
      _item = item;

      // Hides the selection box
      hideSelectedBox();
   }

   public void OnPointerClick (PointerEventData eventData) {
      if (_interactable) {
         // If this cell is clicked on, update the currently selected item
         PanelManager.self.itemSelectionScreen.setSelectedItem(this);
      }
   }

   public void disablePointerEvents () {
      _interactable = false;
   }

   public void showSelectedBox () {
      selectedBox.enabled = true;
   }

   public void hideSelectedBox () {
      selectedBox.enabled = false;
   }

   public Item getItem () {
      return _item;
   }

   #region Private Variables

   // A reference to the item being displayed
   private Item _item;

   // Gets set to true when the text must react to pointer events
   protected bool _interactable = true;

   #endregion

}