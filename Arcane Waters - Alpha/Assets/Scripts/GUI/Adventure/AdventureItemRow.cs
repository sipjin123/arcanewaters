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
   public Tooltipped tooltip;

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
         }
         path = Item.isUsingEquipmentXML(item.category) ? item.iconPath : item.getIconPath(); 
         icon.sprite = ImageManager.getSprite(path);
         iconShadow.sprite = icon.sprite;
         itemName.text = Item.isUsingEquipmentXML(item.category) ? item.itemName : item.getName();
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

      // Sets the tooltip when hovering the image
      tooltip.text = Item.isUsingEquipmentXML(item.category) ? item.itemDescription : item.getDescription();

      // Associate a new function with the confirmation button
      buyButton.onClick.RemoveAllListeners();
      buyButton.onClick.AddListener(() => AdventureShopScreen.self.buyButtonPressed(item.id));
   }

   #region Private Variables
      
   #endregion
}
