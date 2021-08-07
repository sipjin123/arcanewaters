using System.Collections.Generic;
using Newtonsoft.Json;

namespace Steam.Purchasing
{
   public class InitTxnParameters : ApiResult
   {
      #region Public Variables

      // Order Id
      [JsonProperty("orderid")]
      public string orderId;

      // Transaction Id
      [JsonProperty("transid")]
      public string transId;

      // Steam Url (used as callback when the purchase is done through a web client)
      [JsonProperty("steamurl")]
      public string steamUrl;

      // Agreements
      public List<string> agreements;

      #endregion
   }
}
