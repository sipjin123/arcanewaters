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
   public ToolTipComponent tooltip;

   // The recolored sprite component on the icon
   public RecoloredSprite recoloredSprite;

   // The default icons, used when it is not defined by the item type
   public Sprite foodIcon;
   public Sprite defaultItemIcon;
   public Sprite disabledItemIcon;

   // The background image
   public Image backgroundImage;

   // The click events
   public UnityEvent leftClickEvent;
   public UnityEvent rightClickEvent;
   public UnityEvent doubleClickEvent;
   public UnityEvent onPointerEnter;
   public UnityEvent onPointerExit;
   public UnityEvent onDragStarted;

   // Item type id
   public int itemTypeId;

   // Item sprite id
   public int itemSpriteId;

   // The cahed item data
   public Item itemCache;

   // The item instance that is being displayed
   public ItemInstance targetItem;

   // The icon indicating this item is a blueprint
   public GameObject blueprintIcon;

   // If the item is disabled
   public bool isDisabledItem;

   // Color overlay depending on item durability
   public Color midDurabilityColor, lowDurabilityColor, highDurabilityColor;

   // Image reference of the item cell
   public Image itemCellImage;

   // The transparency of the durability notification
   public const float DURABILITY_NOTIF_ALPHA = 250;

   #endregion

   public virtual void clear () {
      itemCache = null;
      targetItem = null;
      itemSpriteId = 0;
      itemTypeId = 0;
      leftClickEvent.RemoveAllListeners();
      rightClickEvent.RemoveAllListeners();
      doubleClickEvent.RemoveAllListeners();
      onPointerEnter.RemoveAllListeners();
      onPointerExit.RemoveAllListeners();
      onDragStarted.RemoveAllListeners();
      icon.sprite = null;      
      iconShadow.sprite = null;

      icon.enabled = false;
      iconShadow.enabled = false;

      itemCountText.gameObject.SetActive(false);

      disablePointerEvents();
   }

   public virtual void setCellForItem (Item item) {
      icon.enabled = true;
      iconShadow.enabled = true;
      enablePointerEvents();

      itemCache = item;
      setCellForItem(item, item.count);
   }

   public void setCellForItem (Item item, int count) {
      // Retrieve the icon sprite and coloring depending on the type
      switch (item.category) {
         case Item.Category.Weapon:
            WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(item.itemTypeId);
            if (weaponData == null) {
               D.debug("Failed to process weapon data of item type: " + item.itemTypeId);
               icon.sprite = disabledItemIcon;
               isDisabledItem = true;
               itemCountText.gameObject.SetActive(false);
               tooltip.message = "Disabled Item";
               return;
            } else {
               Weapon newWeapon = WeaponStatData.translateDataToWeapon(weaponData);
               newWeapon.id = item.id;
               newWeapon.paletteNames = item.paletteNames;
               newWeapon.durability = item.durability;
               item = newWeapon;

               icon.sprite = ImageManager.getSprite(weaponData.equipmentIconPath);
               itemSpriteId = weaponData.weaponType;
               itemTypeId = item.itemTypeId;
            }
            break; 
         case Item.Category.Armor:
            ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(item.itemTypeId);
            if (armorData == null) {
               D.debug("Failed to process armor data of item type: " + item.itemTypeId);
               icon.sprite = disabledItemIcon;
               isDisabledItem = true;
               itemCountText.gameObject.SetActive(false);
               tooltip.message = "Disabled Item";
               return;
            } else {
               Armor newArmor = ArmorStatData.translateDataToArmor(armorData);
               newArmor.id = item.id;
               newArmor.paletteNames = item.paletteNames;
               newArmor.durability = item.durability;
               item = newArmor;

               icon.sprite = ImageManager.getSprite(armorData.equipmentIconPath);
               itemSpriteId = armorData.armorType;
               itemTypeId = item.itemTypeId;
            }
            break;
         case Item.Category.Hats:
            HatStatData hatData = EquipmentXMLManager.self.getHatData(item.itemTypeId);
            if (hatData == null) {
               D.debug("Failed to process hat data of item type: " + item.itemTypeId);
               icon.sprite = disabledItemIcon;
               isDisabledItem = true;
               itemCountText.gameObject.SetActive(false);
               tooltip.message = "Disabled Item";
               return;
            } else {
               Hat newHat = HatStatData.translateDataToHat(hatData);
               newHat.id = item.id;
               newHat.paletteNames = item.paletteNames;
               newHat.durability = item.durability;
               item = newHat;

               icon.sprite = ImageManager.getSprite(hatData.equipmentIconPath);
               itemSpriteId = hatData.hatType;
               itemTypeId = item.itemTypeId;
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
            icon.sprite =  ImageManager.getSprite(CraftingIngredients.getBorderlessIconPath(ingredientType));
            break;
         case Item.Category.Blueprint:
            blueprintIcon.SetActive(true);
            if (item.data.Contains(Blueprint.ARMOR_DATA_PREFIX)) {
               D.debug("Blueprint processing armor");
               ArmorStatData fetchedArmorData = EquipmentXMLManager.self.getArmorDataBySqlId(item.itemTypeId);
               if (fetchedArmorData == null) {
                  D.debug("Failed to fetch Armor Data for: " + item.itemTypeId);
                  Destroy(gameObject);
                  return;
               } else {
                  icon.sprite = ImageManager.getSprite(fetchedArmorData.equipmentIconPath);
               }
            } else if (item.data.Contains(Blueprint.WEAPON_DATA_PREFIX)) {
               D.debug("Blueprint processing weapon");
               WeaponStatData fetchedWeaponData = EquipmentXMLManager.self.getWeaponData(item.itemTypeId);
               if (fetchedWeaponData == null) {
                  D.debug("Failed to fetch Weapon Data for: " + item.itemTypeId);
                  Destroy(gameObject);
                  return;
               } else {
                  icon.sprite = ImageManager.getSprite(fetchedWeaponData.equipmentIconPath);
               }
            } else if (item.data.Contains(Blueprint.HAT_DATA_PREFIX)) {
               D.debug("Blueprint processing hat");
               HatStatData fetchedHatData = EquipmentXMLManager.self.getHatData(item.itemTypeId);
               if (fetchedHatData == null) {
                  D.debug("Failed to fetch Hat Data for: " + item.itemTypeId);
                  Destroy(gameObject);
                  return;
               } else {
                  icon.sprite = ImageManager.getSprite(fetchedHatData.equipmentIconPath);
               }
            } else {
               D.debug("Unknown blueprint item! " + item.data);
               Destroy(gameObject);
            }

            if (icon.sprite == ImageManager.self.blankSprite) {
               D.debug("Could not retrieve Blueprint: " + item.category + " : " + item.itemTypeId + " : " + item.data);
            }
            break;
         default:
            D.editorLog("Failed to process Uncategorized item: " + item.itemTypeId, Color.red);
            icon.sprite = defaultItemIcon;
            break;
      }

      // Set the sprite of the shadow
      iconShadow.sprite = icon.sprite;

      // Recolor
      recoloredSprite.recolor(item.paletteNames);

      // Show the item count when relevant
      if (count > 1 || item.category == Item.Category.CraftingIngredients) {
         itemCountText.SetText(count.ToString());
         itemCountText.gameObject.SetActive(true);
      } else {
         itemCountText.gameObject.SetActive(false);
      }

      // Set the tooltip
      tooltip.message = item.category == Item.Category.Blueprint ? EquipmentXMLManager.self.getItemName(item) : item.getTooltip();
      tooltip.message += Item.isUsingEquipmentXML(item.category) ? "\nDurability = " + item.durability : "";
      updateCellColor(item.durability);

      // Saves the item
      _item = item;

      // Hides the selection box
      hideSelectedBox();
   }

   private void updateCellColor (int durability) {
      // Prototype UI feeback if an item durability is low or high
      if (itemCellImage) {
         if (durability < 25) {
            lowDurabilityColor.a = DURABILITY_NOTIF_ALPHA;
            itemCellImage.color = lowDurabilityColor;
         } else if (durability <= 50) {
            lowDurabilityColor.a = DURABILITY_NOTIF_ALPHA;
            itemCellImage.color = midDurabilityColor;
         } else {
            lowDurabilityColor.a = 0;
            itemCellImage.color = highDurabilityColor;
         }
      }
   }

   public void setCellForItem (ItemInstance item) {
      targetItem = item;
      icon.sprite = ImageManager.getSprite(item.getDefinition().iconPath);

      // Set the sprite of the shadow
      iconShadow.sprite = icon.sprite;

      // Recolor
      recoloredSprite.recolor(item.palettes);

      // Show the item count when relevant
      if (item.count > 1 || item.getDefinition().category == ItemDefinition.Category.CraftingIngredients) {
         itemCountText.SetText(item.count.ToString());
         itemCountText.gameObject.SetActive(true);
      } else {
         itemCountText.gameObject.SetActive(false);
      }

      // Set the tooltip
      tooltip.message = item.getTooltip();

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

   public void enablePointerEvents () {
      _interactable = true;
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
 