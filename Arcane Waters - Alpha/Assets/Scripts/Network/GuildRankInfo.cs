using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class GuildRankInfo
{
   #region Public Variables
     
   // PK in database
   public int id;

   // Guild ID which is unique to each guild (FK in database)
   public int guildId;

   // Rank ID determining each rank within guild
   public int rankId;

   // Name of the rank that will be displayed in guild settings
   public string rankName;

   // Priority - the lower the better, counting from 1 (0 is guild owner)
   public int rankPriority;

   // Permissions in form of integer bits
   public int permissions;

   public enum GuildPermission
   {
      Invite = 1,
      Kick = 2,
      OfficerChat = 4,
      Promote = 8,
      Demote = 16,
      EditRanks = 32
   }

   #endregion

#if IS_SERVER_BUILD

   public GuildRankInfo (MySqlDataReader dataReader) {
      this.id = dataReader.GetInt32("id");
      this.guildId = dataReader.GetInt32("guildId");
      this.rankId = dataReader.GetInt32("rankId");
      this.rankName = dataReader.GetString("rankName");
      this.rankPriority = dataReader.GetInt32("rankPriority");
      this.permissions = dataReader.GetInt32("permissions");
   }

#endif

   public GuildRankInfo () {

   }

   public static GuildRankInfo getDefaultOfficer (int guildId) {
      GuildRankInfo info = new GuildRankInfo();
      info.guildId = guildId;
      info.rankId = 1;
      info.rankName = "officer";
      info.rankPriority = 1;
      info.permissions = (int) GuildPermission.Invite +
                         (int) GuildPermission.Kick +
                         (int) GuildPermission.OfficerChat +
                         (int) GuildPermission.Promote +
                         (int) GuildPermission.Demote;

      return info;
   }

   public static GuildRankInfo getDefaultMember (int guildId) {
      GuildRankInfo info = new GuildRankInfo();
      info.guildId = guildId;
      info.rankId = 2;
      info.rankName = "member";
      info.rankPriority = 2;
      info.permissions = 0;

      return info;
   }

   public static bool canPerformAction(int permissions, GuildPermission action) {
      return ((permissions & (int) action) != 0);
   }

   #region Private Variables

   #endregion
}
