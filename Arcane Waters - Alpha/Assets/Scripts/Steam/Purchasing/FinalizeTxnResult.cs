using Newtonsoft.Json;

namespace Steam.Purchasing
{
   public class FinalizeTxnResult : ApiResult
   {
      #region Public Variables

      // Parameters
      [JsonProperty("params")]
      public FinalizeTxnParameters parameters;

      #endregion
   }
}
