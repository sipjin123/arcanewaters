using Newtonsoft.Json;

namespace Steam.Purchasing
{
   public class InitTxnResult : ApiResult
   {
      #region Public Variables

      // Parameters
      [JsonProperty("params")]
      public InitTxnParameters parameters;

      #endregion
   }
}
