using System;

// Steam generated Json format (Cannot be customized)

namespace SteamLoginSystem 
{
   [Serializable]
   public class AppOwnerShipResponse {
      // The response data
      public Appownership appownership;
   }

   [Serializable]
   public class Appownership {
      // If the user owns the app
      public bool ownsapp;

      // If the app is a permanent app in the library of the steam user
      public bool permanent;

      // The time the app was purchased
      public string timestamp;

      // The steam id of the owner
      public string ownersteamid;

      // If this is licensed
      public bool sitelicense;

      // The query result
      public string result;
   }
}