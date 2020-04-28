using System;

// Steam generated Json format (Cannot be customized)

namespace SteamLoginSystem
{
   [Serializable]
   public class AuthenticateTicketResponse {
      // The json data response
      public Response response;
   }

   [Serializable]
   public class Response {
      // The detailed response of the content
      public Params newParams;
   }

   [Serializable]
   public class Params {
      // The result of the query
      public string result;

      // The steam id of the application
      public string steamid;

      // The steam id of the user
      public string ownersteamid;

      // If this is a banned user // Check for more info >> https://partner.steamgames.com/doc/features/anticheat/vac_integration
      public bool vacbanned;

      // If is publisher banned
      public bool publisherbanned;

   }
}