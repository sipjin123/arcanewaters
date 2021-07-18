public class PvpShopData {
   #region Public Variables

   // Auto generated id
   public int itemId;

   // Name of the item
   public string itemName;

   // How much the item cost
   public int itemCost;

   // The resource path of the sprite image
   public string spritePath;

   // The type of shop item
   public PvpShopItemType shopItemType;

   public enum PvpShopItemType {
      None = 0,
      Ship = 1,
      Powerup = 2,
   }

   #endregion
}