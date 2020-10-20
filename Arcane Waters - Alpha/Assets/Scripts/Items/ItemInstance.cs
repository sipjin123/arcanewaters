using System;
using System.Collections.Generic;

/// <summary>
/// Represents an instance of an item inside the game. Actual information and behavior of an item is described in 'ItemDefinition' class.
/// </summary>
[Serializable]
public class ItemInstance
{
   #region Public Variables

   // Unique identifier of item's instance
   public int id;

   // Unique identifier of the this item's description
   public int itemDefinitionId;

   // User id of the owner of the item
   public int ownerUserId;

   // The number of these items that are stacked together
   public int count = 1;

   // Name of palettes that changes color of item
   public string palettes;

   // The rarity of the item
   public Rarity.Type rarity = Rarity.Type.None;

   #endregion

   public ItemInstance () { }

   public ItemInstance (int itemDefinitionId, int ownerUserId, int count) {
      this.itemDefinitionId = itemDefinitionId;
      this.ownerUserId = ownerUserId;
      this.count = count;
   }

#if IS_SERVER_BUILD

   public ItemInstance (MySql.Data.MySqlClient.MySqlDataReader dataReader) {
      id = DataUtil.getInt(dataReader, "id");
      itemDefinitionId = DataUtil.getInt(dataReader, "itemDefinitionId");
      ownerUserId = DataUtil.getInt(dataReader, "userId");
      count = DataUtil.getInt(dataReader, "count");
      palettes = DataUtil.getString(dataReader, "palettes");
      rarity = (Rarity.Type) DataUtil.getInt(dataReader, "rarity");
   }

#endif

   public string getName () {
      // Add any special handling if needed

      return getDefinition().name;
   }

   public string getDescription () {
      // Add any special handling if needed

      return getDefinition().description;
   }

   public string getTooltip () {
      switch (getDefinition().category) {
         // TODO: add tooltips for various item categories
         default:
            return getDescription();
      }
   }

   // Hardcoded item rewards for when player gets his first house/farm
   // 2 Tables, 6 Chairs, 5 Trees, 1 Stumps, 2 Bushes
   public static List<ItemInstance> getFirstFarmRewards (int receiverUserId) {
      return new List<ItemInstance> {
         new ItemInstance(7, receiverUserId, 5),
         new ItemInstance(8, receiverUserId, 1),
         new ItemInstance(9, receiverUserId, 2)
      };
   }

   public static List<ItemInstance> getFirstHouseRewards (int receiverUserId) {
      return new List<ItemInstance> {
         new ItemInstance(11, receiverUserId, 2),
         new ItemInstance(10, receiverUserId, 6)
      };
   }

   public T getDefinition<T> () where T : ItemDefinition {
      return getDefinition() as T;
   }

   public ItemDefinition getDefinition () {
      // If definition is assigned, try getting it
      if (_definition == null && ItemDefinitionManager.self != null) {
         _definition = ItemDefinitionManager.self.getDefinition(itemDefinitionId);
      }

      return _definition;
   }

   #region Private Variables

   // The definition of this item, describing it's behavior
   // We are not serializing this field, so it would be left out when trasferred over network
   // 'getDefinition' method should be relied upon to reattach it, based on 'itemDefinitionId'
   [NonSerialized]
   protected ItemDefinition _definition;

   #endregion
}
