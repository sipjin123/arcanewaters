using Newtonsoft.Json;

namespace Steam.Purchasing
{
   public class GetUserInfoResult : ApiResult
   {
      #region Public Variables

      // Parameters
      [JsonProperty("params")]
      public GetUserInfoParameters parameters;

      #endregion
   }
}
