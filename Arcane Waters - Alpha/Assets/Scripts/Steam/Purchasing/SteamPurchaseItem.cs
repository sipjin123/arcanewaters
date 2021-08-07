namespace Steam.Purchasing
{
   public class SteamPurchaseItem
   {
      #region Public Variables

      // The item id
      public string itemId;

      // The quantity
      public int quantity;

      // Total cost (must be quantity * unitary cost)
      public long totalCost;

      // The description
      public string description;

      // The category
      public string category;

      #endregion
   }
}
