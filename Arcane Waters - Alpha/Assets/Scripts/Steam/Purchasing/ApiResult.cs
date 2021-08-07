namespace Steam.Purchasing
{
   public class ApiResult
   {
      #region Public Variables

      // Errors
      public ApiResultError error;

      // Result
      public string result;

      #endregion

      public bool isSuccess () {
         return result.ToLower() == "ok";
      }
   }
}
