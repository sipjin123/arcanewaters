using System.Collections.Generic;
using Store;

public struct StoreResourcesResponse
{
   #region Public Variables

   // The store items
   public List<StoreItem> items;

   // The current player's user objects
   public UserObjects userObjects;

   // Whether the store is enabled
   public bool isStoreEnabled;

   // The message to display when the store is unavailable
   public string gemStoreIsDisabledMessage;

   #endregion
}
