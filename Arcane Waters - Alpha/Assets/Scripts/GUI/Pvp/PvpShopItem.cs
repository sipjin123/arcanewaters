using System;

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

   public enum PvpShopItemType
   {
      None = 0,
      Powerup = 1,
      Ship = 2,
      Ability = 3,
      Stats = 4,
      Item = 5,
      CraftingIngredient = 6,
      LandPowerup = 7
   }

   public static PvpShopItem defaultConsumableItem () {
      PvpShopItem newShopItem = new PvpShopItem {
         itemCost = 100,
         itemId = (int) PvpConsumableItem.RepairTool,
         shopItemType = PvpShopItem.PvpShopItemType.Item,
         itemName = "Repair Tool",
         rarityType = Rarity.Type.Common,
         spritePath = "Sprites/GUI/PvpShop/icons/items_on",
      };

      return newShopItem;
   }

   #endregion
}

public enum PvpConsumableItem {
   None = 0,
   RepairTool = 1,
   Beacon = 2,
}