public class ShopData {
   // Basic info of the shop data
   public string shopName;
   public string shopIconPath = "";
   public string shopGreetingText = "No Greeting Text Setup";
   public string areaAttachment = "";

   public ShopItemData[] shopItems = new ShopItemData[0];
}

public class ShopItemData {
   // Name of the item
   public string itemName;

   // Image path of the item
   public string itemIconPath = "";

   // What category the item is (Ship/Item/Crop)
   public ShopToolPanel.ShopCategory shopItemCategory;

   // Item category if there is one
   public int shopItemCategoryIndex;

   // Item Type 
   public int shopItemTypeIndex;

   // Cost of the item Min
   public int shopItemCostMin;

   // Cost of the item Max
   public int shopItemCostMax;

   // Count of the item Min
   public int shopItemCountMin;

   // Count of the item Max
   public int shopItemCountMax;

   // Chance to Drop
   public float dropChance;
}