using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

public class ItemCell : MonoBehaviour, IPointerClickHandler
{

   #region Public Variables

   // The icon of the item
   public Image icon;

   // The shadow of the icon
   public Image iconShadow;

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
      _colorKey = null;
      switch (item.category) {
         case Item.Category.Weapon:
            icon.sprite = ImageManager.getSprite(item.getIconPath());
            _colorKey = new ColorKey(Global.player.gender, (Weapon.Type) item.itemTypeId);
            break;
         case Item.Category.Armor:
         case Item.Category.Helm:
            icon.sprite = ImageManager.getSprite(item.getIconPath());
            _colorKey = new ColorKey(Global.player.gender, (Armor.Type) item.itemTypeId);
            break;
         case Item.Category.Potion:
            icon.sprite = foodIcon;
            break;
         case Item.Category.Usable:
            icon.sprite = defaultItemIcon;
            break;
         case Item.Category.CraftingIngredients:
            icon.sprite = ImageManager.getSprite(item.getBorderlessIconPath());
            break;
         case Item.Category.Blueprint:
            icon.sprite = ImageManager.getSprite(item.getIconPath());
            break;
         default:
            icon.sprite = defaultItemIcon;
            break;
      }

      // Set the sprite of the shadow
      iconShadow.sprite = icon.sprite;

      // Recolor
      if (_colorKey != null) {
         recoloredSprite.recolor(_colorKey, item.color1, item.color2);
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

   public virtual void OnPointerClick (PointerEventData eventData) {
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

   public Sprite getItemSprite () {
      return icon.sprite;
   }

   public ColorKey getItemColorKey () {
      return _colorKey;
   }

   #region Private Variables

   // A reference to the item being displayed
   private Item _item;

   // Gets set to true when the text must react to pointer events
   protected bool _interactable = true;

   // The item color key
   private ColorKey _colorKey = null;

   #endregion

}