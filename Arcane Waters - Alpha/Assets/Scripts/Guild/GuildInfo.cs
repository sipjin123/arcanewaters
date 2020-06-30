using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public enum GuildType {
   None = 0,
   Bot_Pirates = 1,
   Bot_Privateers = 2
}

public class GuildInfo {
   #region Public Variables

   // The maximum number of people in a guild
   public static int MAX_MEMBERS = 20;

   // The minimum length of the guild name
   public static int MIN_NAME_LENGTH = 3;

   // The maximum length of the guild name
   public static int MAX_NAME_LENGTH = 15;

   // The Guild ID
   public int guildId;

   // The Guild Name
   public string guildName;

   // The list of people in the guild
   public UserInfo[] guildMembers; 

   // The time at which the guild was created
   public long creationTime;
      
   #endregion

   public GuildInfo () {

   }

   public GuildInfo (int guildId) {
      this.guildId = guildId;
   }

   #region Private Variables
      
   #endregion
}
