using Newtonsoft.Json;

namespace Steam.Purchasing
{
   public class FinalizeTxnParameters : ApiResult
   {
      #region Public Variables

      // Order Id
      [JsonProperty("orderid")]
      public string orderId;

      // Transaction Id
      [JsonProperty("transid")]
      public string transId;

      #endregion
   }
}
