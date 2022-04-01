namespace Api
{
   public class ApiManager
   {
      #region Public Variables
      
      #if IS_SERVER_BUILD

      // The Api Client Id for the game
      public const string ARCANE_WATERS_API_CLIENT_ID = "6BEA0E4B-5FEB-4305-81EB-51846D311FCF";

      // The Api Client Id of the other game
      public const string OTHER_API_CLIENT_ID = "AA7637E9-D7D3-486D-9B50-5FA43C3BCC92";

      #else

      // The Api Client Id for the game
      public const string ARCANE_WATERS_API_CLIENT_ID = "";
      
      // The Api Client Id of the other game
      public const string OTHER_CLIENT_ID = "";

      #endif

      #endregion
   }
}
