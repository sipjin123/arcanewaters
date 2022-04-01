namespace Rewards
{
   public class RewardCode
   {
      #region Public Variables

      // The id of the code
      public int id;

      // The id of the player this code is meant for
      public string userId;

      // The value of the code
      public string code;

      // The identifier of the entity that produced the code
      public string producerId;

      // The identifier of the entity that is meant to use the code
      public string consumerId;

      #endregion
   }
}