using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.Events;

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

   // The background image
   public Image backgroundImage;

   // The click events
   public UnityEvent leftClickEvent;
   public UnityEvent rightClickEvent;
   public UnityEvent doubleClickEvent;

   // Item type id
   public int itemTypeId;

   // Item sprite id
   public int itemSpriteId;

   // The cahed item data
   public Item itemCache;

   #endregion

   public void setCellForItem (Item item) {
      itemCache = item;
      setCellForItem(item, item.count);
   }

   public void setCellForItem (Item item, int count) {
      // Retrieve the icon sprite and coloring depending on the type
      switch (item.category) {
         case Item.Category.Weapon:
            WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(item.itemTypeId);
            Weapon newWeapon = WeaponStatData.translateDataToWeapon(weaponData);
            newWeapon.id = item.id;
            item = newWeapon;

            icon.sprite = ImageManager.getSprite(weaponData.equipmentIconPath);
            itemSpriteId = weaponData.weaponType;
            itemTypeId = item.itemTypeId;
            break;
         case Item.Category.Armor:
            item = Armor.castItemToArmor(item);
            if (item.data.Length < 1) {
               ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(item.itemTypeId);
               icon.sprite = ImageManager.getSprite(armorData.equipmentIconPath);
            } else {
               ArmorStatData armorData = ArmorStatData.getStatData(item.data, item.itemTypeId);
               icon.sprite = ImageManager.getSprite(armorData.equipmentIconPath);
            }
            break;
         case Item.Category.Hats:
            item = Hat.castItemToHat(item);
            if (item.data.Length < 1) {
               HatStatData hatData = EquipmentXMLManager.self.getHatData(item.itemTypeId);
               icon.sprite = ImageManager.getSprite(hatData.equipmentIconPath);
            } else {
               HatStatData hatData = HatStatData.getStatData(item.data, item.itemTypeId);
               icon.sprite = ImageManager.getSprite(hatData.equipmentIconPath);
            }
            break;
         case Item.Category.Potion:
            item = item.getCastItem();
            icon.sprite = foodIcon;
            break;
         case Item.Category.Usable:
            item = item.getCastItem();
            icon.sprite = defaultItemIcon;
            break;
         case Item.Category.CraftingIngredients:
            item = item.getCastItem();
            CraftingIngredients.Type ingredientType = (CraftingIngredients.Type) item.itemTypeId;
            icon.sprite =  ImageManager.getSprite(CraftingIngredients.getIconPath(ingredientType));
            break;
         case Item.Category.Blueprint:
            item = item.getCastItem();
            icon.sprite = ImageManager.getSprite(item.getIconPath());
            break;
         default:
            D.editorLog("Failed to process Uncategorized item: " + item.itemTypeId, Color.red);
            icon.sprite = defaultItemIcon;
            break;
      }

      // Set the sprite of the shadow
      iconShadow.sprite = icon.sprite;

      // Recolor
      recoloredSprite.recolor(item.paletteName1, item.paletteName2);

      // Show the item count when relevant
      if (count > 1 || item.category == Item.Category.CraftingIngredients) {
         itemCountText.SetText(count.ToString());
         itemCountText.gameObject.SetActive(true);
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
      // Invoke the click events
      if (_interactable) {
         if (eventData.button == PointerEventData.InputButton.Left) {
            // Determine if two clicks where close enough in time to be considered a double click
            if (Time.time - _lastClickTime < DOUBLE_CLICK_DELAY) {
               doubleClickEvent.Invoke();
            } else {
               leftClickEvent.Invoke();
            }
            _lastClickTime = Time.time;
         } else if (eventData.button == PointerEventData.InputButton.Right) {
            rightClickEvent.Invoke();
         }
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

   public void hideBackground () {
      backgroundImage.enabled = false;
   }

   public void hideItemCount () {
      itemCountText.gameObject.SetActive(false);
   }

   #region Private Variables

   // A reference to the item being displayed
   protected Item _item;

   // Gets set to true when the text must react to pointer events
   protected bool _interactable = true;

   // The time since the last left click on this cell
   private float _lastClickTime = float.MinValue;

   // The delay between two clicks to be considered a double click
   private float DOUBLE_CLICK_DELAY = 0.5f;

   #endregion

}
 