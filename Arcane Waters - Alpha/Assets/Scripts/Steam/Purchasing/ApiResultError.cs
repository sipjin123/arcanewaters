using Newtonsoft.Json;

namespace Steam.Purchasing
{
   public class ApiResultError
   {
      #region Public Variables

      // The error code
      [JsonProperty("errorcode")]
      public string errorCode;

      // The error description
      [JsonProperty("errordesc")]
      public string errorDesc;

      #endregion
   }
}
