using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class AdventureItemRow : MonoBehaviour {
   #region Public Variables

   // The icon of the item
   public Image icon;

   // The shadow of the icon
   public Image iconShadow;

   // The recolored sprite component on the icon
   public RecoloredSprite recoloredSprite;

   // The item name
   public Text itemName;

   // The item count
   public TextMeshProUGUI itemCountField;

   // The rarity stars
   public Image star1Image;
   public Image star2Image;
   public Image star3Image;

   // The gold amount
   public Text goldAmount;

   // The Button
   public Button buyButton;

   // The Item associated with this row
   [HideInInspector]
   public Item item;

   // The tooltip on the image
   public ToolTipComponent tooltip;

   // An icon that is enabled if the item is a blueprint
   public GameObject blueprintIndicator;

   // Name of the blueprint item;
   public string blueprintName;

   // Description of the blueprint item
   public string blueprintDescription;

   // Strength value of the blueprint item
   public string blueprintStrength;

   #endregion

   public void setRowForItem (Item item) {
      string rawItemData = item.data;
      this.item = item;
      bool displayStars = true;

      string path = ""; 
      if (item.category == Item.Category.CraftingIngredients) {
         CraftingIngredients.Type ingredientType = (CraftingIngredients.Type) item.itemTypeId;
         path = CraftingIngredients.getIconPath(ingredientType);
         icon.sprite = ImageManager.getSprite(path);
         iconShadow.sprite = icon.sprite;
         itemName.text = CraftingIngredients.getName(ingredientType);
      } else {
         switch (item.category) {
            case Item.Category.Weapon:
               WeaponStatData newWeaponData = EquipmentXMLManager.self.getWeaponData(item.itemTypeId);
               if (newWeaponData == null) {
                  D.debug("Weapon with ID: {" + item.itemTypeId + "} does not exist!");
                  Destroy(gameObject);
                  return;
               } else {
                  Weapon newWeapon = WeaponStatData.translateDataToWeapon(newWeaponData);
                  newWeapon.id = item.id;
                  newWeapon.paletteNames = item.paletteNames;
                  newWeapon.durability = item.durability;
                  newWeapon.count = item.count;
                  item = newWeapon;
                  item.data = rawItemData;
               }

               // Disable rarity display for seeds
               if (newWeaponData.actionType == Weapon.ActionType.PlantCrop) {
                  displayStars = false;
               }
               break;
            case Item.Category.Armor:
               ArmorStatData newArmorData = EquipmentXMLManager.self.getArmorDataBySqlId(item.itemTypeId);
               if (newArmorData == null) {
                  D.debug("Armor with ID: {" + item.itemTypeId + "} does not exist!");
                  Destroy(gameObject);
                  return;
               } else {
                  Armor newWeapon = ArmorStatData.translateDataToArmor(newArmorData);
                  newWeapon.id = item.id;
                  newWeapon.paletteNames = item.paletteNames;
                  newWeapon.durability = item.durability;
                  item = newWeapon;
                  item.data = rawItemData;
               }
               break;
            case Item.Category.Hats:
               HatStatData newHatData = EquipmentXMLManager.self.getHatData(item.itemTypeId);
               if (newHatData == null) {
                  D.debug("Hat with ID: {" + item.itemTypeId + "} does not exist!");
                  Destroy(gameObject);
                  return;
               } else {
                  Hat newHat = HatStatData.translateDataToHat(newHatData);
                  newHat.id = item.id;
                  newHat.paletteNames = item.paletteNames;
                  newHat.durability = item.durability;
                  item = newHat;
                  item.data = rawItemData;
               }
               break;
            case Item.Category.Blueprint:
               CraftableItemRequirements craftingData = CraftingManager.self.getCraftableData(item.itemTypeId);
               blueprintIndicator.SetActive(true);

               if (craftingData.resultItem.category == Item.Category.Armor) {
                  ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(craftingData.resultItem.itemTypeId);
                  if (armorData == null) {
                     itemName.text = AdventureShopScreen.UNKNOWN_ITEM;
                     blueprintName = itemName.text;
                  } else {
                     path = armorData.equipmentIconPath;
                     itemName.text = armorData.equipmentName + " Design";
                     blueprintName = itemName.text;
                     blueprintDescription = armorData.equipmentDescription;
                     blueprintStrength = "Defense = " + armorData.armorBaseDefense.ToString();
                  }
               } else if (craftingData.resultItem.category == Item.Category.Weapon) {
                  WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(craftingData.resultItem.itemTypeId);
                  if (weaponData == null) {
                     itemName.text = AdventureShopScreen.UNKNOWN_ITEM;
                     blueprintName = itemName.text;
                  } else {
                     path = weaponData.equipmentIconPath;
                     itemName.text = weaponData.equipmentName + " Design";
                     blueprintName = itemName.text;
                     blueprintDescription = weaponData.equipmentDescription;
                     blueprintStrength = "Damage = " + weaponData.weaponBaseDamage.ToString();
                  }
               } else if (craftingData.resultItem.category == Item.Category.Hats) {
                  HatStatData hatData = EquipmentXMLManager.self.getHatData(craftingData.resultItem.itemTypeId);
                  if (hatData == null) {
                     itemName.text = AdventureShopScreen.UNKNOWN_ITEM;
                     blueprintName = itemName.text;
                  } else {
                     path = hatData.equipmentIconPath;
                     itemName.text = hatData.equipmentName + " Design";
                     blueprintName = itemName.text;
                     blueprintDescription = hatData.equipmentDescription;
                     blueprintStrength = "Defense = " + hatData.hatBaseDefense.ToString();
                  }
               }

               item.itemName = itemName.text;
               icon.sprite = ImageManager.getSprite(path);
               iconShadow.sprite = icon.sprite;
               break;
            default:
               D.debug("Unknown Ingredient!");
               break;
         }
         if (item.category != Item.Category.Blueprint) {
            path = Item.isUsingEquipmentXML(item.category) ? item.iconPath : item.getIconPath();
            icon.sprite = ImageManager.getSprite(path);
            iconShadow.sprite = icon.sprite;
            itemName.text = Item.isUsingEquipmentXML(item.category) ? item.itemName : item.getName();
         }
      }

      // Show the item count when relevant
      if (item.count > 1) {
         itemCountField.SetText(item.count.ToString());
         itemCountField.gameObject.SetActive(true);
      } else {
         itemCountField.gameObject.SetActive(false);
      }

      float perkMultiplier = 1.0f - PerkManager.self.getPerkMultiplierAdditive(Perk.Category.ShopPriceReduction);
      goldAmount.text = ((int)(item.getSellPrice() * perkMultiplier)) + "";

      // Rarity stars
      Sprite[] rarityStars = Rarity.getRarityStars(item.getRarity());
      if (displayStars) {
         star1Image.sprite = rarityStars[0];
         star2Image.sprite = rarityStars[1];
         star3Image.sprite = rarityStars[2];
      } else {
         star1Image.sprite = ImageManager.self.blankSprite;
         star2Image.sprite = ImageManager.self.blankSprite;
         star3Image.sprite = ImageManager.self.blankSprite;
      }

      // Recolor
      recoloredSprite.recolor(item.paletteNames);

      tooltip.message = getTooltipText(item);

      // Associate a new function with the confirmation button
      buyButton.onClick.RemoveAllListeners();
      buyButton.onClick.AddListener(() => AdventureShopScreen.self.buyButtonPressed(item.id));
   }

   public string getTooltipText (Item item) {
      // Set the tooltip Description text
      string itemDescription = Item.isUsingEquipmentXML(item.category) ? item.itemDescription : item.getDescription();

      // Set the tooltip Strength text
      string itemStrength = "";
      string itemName = "";
      if (item.category == Item.Category.Weapon) {
         Weapon weapon = (Weapon) item;
         WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(item.itemTypeId);
         if (weaponData != null) {
            itemStrength = weapon.getTooltip();// "Damage = " + weaponData.weaponBaseDamage.ToString();
            itemName = weaponData.equipmentName;
         } else {
            D.debug("Missing weapon data with id: " + item.itemTypeId);
         }
      }
      if (item.category == Item.Category.Armor) {
         Armor armor = (Armor) item;
         ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(item.itemTypeId);
         if (armorData != null) {
            itemStrength = armor.getTooltip();// "Defense = " + armorData.armorBaseDefense.ToString();
            itemName = armorData.equipmentName;
         } else {
            D.debug("Missing armor data with id: " + item.itemTypeId);
         }
      }
      if (item.category == Item.Category.Hats) {
         Hat hat = (Hat) item;
         HatStatData hatData = EquipmentXMLManager.self.getHatData(item.itemTypeId);
         if (hatData != null) {
            itemStrength = hat.getTooltip();// "Defense = " + hatData.hatBaseDefense.ToString();
            itemName = hatData.equipmentName;
         } else {
            D.debug("Missing hat data with id: " + item.itemTypeId);
         }
      }
      if (item.category == Item.Category.Blueprint) {
         itemName = blueprintName;
         itemDescription = blueprintDescription;
         itemStrength = blueprintStrength;
      }

      // Set the tooltip Durability text
      string itemDurability = "Durability = " + item.durability.ToString();
      string tooltipInfo = itemName + System.Environment.NewLine + itemDescription + System.Environment.NewLine + System.Environment.NewLine;

      // Clear other info for equipment which is already populated using the method getTooltip()
      if (Item.isUsingEquipmentXML(item.category)) {
         itemDescription = "";
         itemName = "";
         tooltipInfo = "";
      }
      string tooltipText = tooltipInfo + itemStrength + System.Environment.NewLine + itemDurability;
      return tooltipText;
   }

   #region Private Variables

   #endregion
}
