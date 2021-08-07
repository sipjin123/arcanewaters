namespace Steam.Purchasing
{
   public class GetUserInfoParameters : ApiResult
   {
      #region Public Variables

      // State
      public string state;

      // Currency
      public string currency;

      // Country
      public string country;

      // Status
      public string status;

      // Status Types
      public enum UserStatus
      {
         // The user can't make purchases
         LockedFromPurchasing = 0,

         // The user is active
         Active = 1,

         // The user is trusted
         Trusted = 2
      }

      #endregion

      public UserStatus getStatus () {
         if (status.ToLower().Contains("active")) {
            return UserStatus.Active;
         }

         if (status.ToLower().Contains("locked")) {
            return UserStatus.LockedFromPurchasing;
         }

         if (status.ToLower().Contains("trusted")) {
            return UserStatus.Trusted;
         }

         return UserStatus.Active;
      }
   }
}
