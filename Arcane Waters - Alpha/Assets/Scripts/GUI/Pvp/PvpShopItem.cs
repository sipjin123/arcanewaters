using System;
using System.Collections.Generic;

[Serializable]
public class PvpShopItem {
   #region Public Variables

   // If this item is disabled
   public bool isDisabled;

   // Auto generated id
   public int itemId;

   // Name of the item
   public string itemName;

   // The item category and type
   public int itemCategory;
   public int itemType;

   // How much the item cost
   public int itemCost;

   // The resource path of the sprite image
   public string spritePath;

   // The serialized data
   public string itemData;

   // The type of shop item
   public PvpShopItemType shopItemType;

   // The rarity type
   public Rarity.Type rarityType;

   // The repair value in percentage, on this case 40% of ships health will be repaired
   public const float REPAIR_VALUE = .4f;

   public enum PvpShopItemType {
      None = 0,
      Ship = 1,
      Powerup = 2,
      Item = 3,
      Ability = 4,
      Stats = 5,
      CraftingIngredient = 6,
      LandPowerup = 7
   }

   public static PvpShopItem generateItemCopy (PvpShopItem item) {
      PvpShopItem newShopItem = new PvpShopItem();
      newShopItem.isDisabled = item.isDisabled;
      newShopItem.itemId = item.itemId;
      newShopItem.itemName = item.itemName;
      newShopItem.itemCategory = item.itemCategory;
      newShopItem.itemType = item.itemType;
      newShopItem.itemCost = item.itemCost;
      newShopItem.spritePath = item.spritePath;
      newShopItem.itemData = item.itemData;
      newShopItem.shopItemType = item.shopItemType;
      newShopItem.rarityType = item.rarityType;
      return newShopItem;
   }

   public static PvpShopItem defaultConsumableItem () {
      PvpShopItem newShopItem = new PvpShopItem {
         itemCost = 100,
         itemId = (int) PvpConsumableItem.RepairTool,
         shopItemType = PvpShopItem.PvpShopItemType.Item,
         itemName = "Repair Tool",
         rarityType = Rarity.Type.Common,
         spritePath = "Sprites/GUI/PvpShop/icons/repair",
      };

      return newShopItem;
   }

   public static List<PvpShopItem> defaultLandPowerups () {
      List<PvpShopItem> defaultList = new List<PvpShopItem>();

      // The items are hardcoded if there are no declared land powerups in the shop
      PvpShopItem damageBoost = new PvpShopItem {
         itemCost = 100,
         itemId = (int) LandPowerupType.DamageBoost,
         shopItemType = PvpShopItem.PvpShopItemType.LandPowerup,
         itemName = LandPowerupType.DamageBoost.ToString(),
         rarityType = Rarity.Type.Common,
         spritePath = "Sprites/Powerups/LandPowerUpIcons",
      };
      PvpShopItem defenseBoost = new PvpShopItem {
         itemCost = 100,
         itemId = (int) LandPowerupType.DefenseBoost,
         shopItemType = PvpShopItem.PvpShopItemType.LandPowerup,
         itemName = LandPowerupType.DefenseBoost.ToString(),
         rarityType = Rarity.Type.Common,
         spritePath = "Sprites/Powerups/LandPowerUpIcons",
      };
      PvpShopItem speedBoost = new PvpShopItem {
         itemCost = 100,
         itemId = (int) LandPowerupType.SpeedBoost,
         shopItemType = PvpShopItem.PvpShopItemType.LandPowerup,
         itemName = LandPowerupType.SpeedBoost.ToString(),
         rarityType = Rarity.Type.Common,
         spritePath = "Sprites/Powerups/LandPowerUpIcons",
      };

      PvpShopItem experienceBoost = new PvpShopItem {
         itemCost = 100,
         itemId = (int) LandPowerupType.ExperienceBoost,
         shopItemType = PvpShopItem.PvpShopItemType.LandPowerup,
         itemName = LandPowerupType.ExperienceBoost.ToString(),
         rarityType = Rarity.Type.Common,
         spritePath = "Sprites/Powerups/LandPowerUpIcons",
      };
      PvpShopItem lootDropBoost = new PvpShopItem {
         itemCost = 100,
         itemId = (int) LandPowerupType.LootDropBoost,
         shopItemType = PvpShopItem.PvpShopItemType.LandPowerup,
         itemName = LandPowerupType.LootDropBoost.ToString(),
         rarityType = Rarity.Type.Common,
         spritePath = "Sprites/Powerups/LandPowerUpIcons",
      };
      PvpShopItem meleeDamageBoost = new PvpShopItem {
         itemCost = 100,
         itemId = (int) LandPowerupType.MeleeDamageBoost,
         shopItemType = PvpShopItem.PvpShopItemType.LandPowerup,
         itemName = LandPowerupType.MeleeDamageBoost.ToString(),
         rarityType = Rarity.Type.Common,
         spritePath = "Sprites/Powerups/LandPowerUpIcons",
      };
      PvpShopItem rangeDamageBoost = new PvpShopItem {
         itemCost = 100,
         itemId = (int) LandPowerupType.RangeDamageBoost,
         shopItemType = PvpShopItem.PvpShopItemType.LandPowerup,
         itemName = LandPowerupType.RangeDamageBoost.ToString(),
         rarityType = Rarity.Type.Common,
         spritePath = "Sprites/Powerups/LandPowerUpIcons",
      };

      defaultList.Add(damageBoost);
      defaultList.Add(defenseBoost);
      defaultList.Add(speedBoost);
      defaultList.Add(lootDropBoost);
      defaultList.Add(experienceBoost);
      defaultList.Add(meleeDamageBoost);
      defaultList.Add(rangeDamageBoost);
      return defaultList;
   }

   #endregion
}

public enum PvpConsumableItem {
   None = 0,
   RepairTool = 1,
   Beacon = 2,
}