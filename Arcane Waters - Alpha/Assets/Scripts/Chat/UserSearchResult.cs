using System;

public class UserSearchResult {
   #region Public Variables

   // The userId of the user
   public int userId;

   // The name of the user
   public string name;

   // The level of the user
   public int level;

   // The area of the user
   public string area;

   // The biome of the user
   public Biome.Type biome;

   // Is the user online?
   public bool isOnline;

   // Is the user in the same server of the calling user?
   public bool isSameServer;

   // Is the user a friend
   public bool isFriend;

   // Last Time Online
   public DateTime lastTimeOnline;

   #endregion
}
