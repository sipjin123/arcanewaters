using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

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

   #endregion

   public void setRowForItem (Item item) {
      this.item = item;

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
               if (EquipmentXMLManager.self.getWeaponData(item.itemTypeId) == null) {
                  D.debug("Weapon with ID: {" + item.itemTypeId + "} does not exist!");
                  Destroy(gameObject);
                  return;
               }
               break;
            case Item.Category.Armor:
               if (EquipmentXMLManager.self.getArmorDataBySqlId(item.itemTypeId) == null) {
                  D.debug("Armor with ID: {" + item.itemTypeId + "} does not exist!");
                  Destroy(gameObject);
                  return;
               }
               break;
            case Item.Category.Hats:
               if (EquipmentXMLManager.self.getHatData(item.itemTypeId) == null) {
                  D.debug("Hat with ID: {" + item.itemTypeId + "} does not exist!");
                  Destroy(gameObject);
                  return;
               }
               break;
            case Item.Category.Blueprint:
               CraftableItemRequirements craftingData = CraftingManager.self.getCraftableData(item.itemTypeId);
               blueprintIndicator.SetActive(true);

               if (craftingData.resultItem.category == Item.Category.Armor) {
                  ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(craftingData.resultItem.itemTypeId);
                  if (armorData == null) {
                     itemName.text = AdventureShopScreen.UNKNOWN_ITEM;
                  } else {
                     path = armorData.equipmentIconPath;
                     itemName.text = armorData.equipmentName + " Design";
                  }
               } else if (craftingData.resultItem.category == Item.Category.Weapon) {
                  WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(craftingData.resultItem.itemTypeId);
                  if (weaponData == null) {
                     itemName.text = AdventureShopScreen.UNKNOWN_ITEM;
                  } else {
                     path = weaponData.equipmentIconPath;
                     itemName.text = weaponData.equipmentName + " Design";
                  }
               } else if (craftingData.resultItem.category == Item.Category.Hats) {
                  HatStatData hatData = EquipmentXMLManager.self.getHatData(craftingData.resultItem.itemTypeId);
                  if (hatData == null) {
                     itemName.text = AdventureShopScreen.UNKNOWN_ITEM;
                  } else {
                     path = hatData.equipmentIconPath;
                     itemName.text = hatData.equipmentName + " Design";
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

      float perkMultiplier = 1.0f - PerkManager.self.getPerkMultiplierAdditive(Perk.Category.ShopPriceReduction);
      goldAmount.text = ((int)(item.getSellPrice() * perkMultiplier)) + "";

      // Rarity stars
      Sprite[] rarityStars = Rarity.getRarityStars(item.getRarity());
      star1Image.sprite = rarityStars[0];
      star2Image.sprite = rarityStars[1];
      star3Image.sprite = rarityStars[2];

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
         WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(item.itemTypeId);
         if (weaponData != null) {
            itemStrength = "Damage = " + weaponData.weaponBaseDamage.ToString();
            itemName = weaponData.equipmentName;
         } else {
            D.debug("Missing weapon data with id: " + item.itemTypeId);
         }
      }
      if (item.category == Item.Category.Armor) {
         ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(item.itemTypeId);
         if (armorData != null) {
            itemStrength = "Defense = " + armorData.armorBaseDefense.ToString();
            itemName = armorData.equipmentName;
         } else {
            D.debug("Missing armor data with id: " + item.itemTypeId);
         }
      }
      if (item.category == Item.Category.Hats) {
         HatStatData hatData = EquipmentXMLManager.self.getHatData(item.itemTypeId);
         if (hatData != null) {
            itemName = hatData.equipmentName;
            itemStrength = "Defense = " + hatData.hatBaseDefense.ToString();
         } else {
            D.debug("Missing hat data with id: " + item.itemTypeId);
         }
      }

      // Set the tooltip Durability text
      string itemDurability = "Durability = " + item.durability.ToString();

      string tooltipText = itemName + System.Environment.NewLine + itemDescription + System.Environment.NewLine + System.Environment.NewLine + itemStrength + System.Environment.NewLine + itemDurability;
      return tooltipText;
   }

   #region Private Variables

   #endregion
}
