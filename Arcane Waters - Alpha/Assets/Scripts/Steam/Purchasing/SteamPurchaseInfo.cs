using System.Collections.Generic;

namespace Steam.Purchasing
{
   public class SteamPurchaseInfo
   {
      #region Public Variables

      // The order Id
      public ulong orderId;

      // The steam id
      public ulong steamId;

      // The app Id
      public uint appId;

      // The language
      public string language;

      // The currency
      public string currency;

      // The items
      public List<SteamPurchaseItem> items;

      #endregion

      public SteamPurchaseInfo (ulong orderId, ulong steamId, uint appId, string language, string currency, List<SteamPurchaseItem> items) {
         this.orderId = orderId;
         this.steamId = steamId;
         this.appId = appId;
         this.language = language;
         this.currency = currency;
         this.items = items;
      }
   }
}
