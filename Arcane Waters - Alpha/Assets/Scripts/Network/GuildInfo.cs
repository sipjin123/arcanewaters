using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class GuildInfo
{
   #region Public Variables

   // The Guild ID
   public int guildId;

   // The Guild Name
   public string guildName;

   // The guild icon layers
   public string iconBorder;
   public string iconBackground;
   public string iconSigil;

   // The guild icon palettes
   public string iconBackPalette1;
   public string iconBackPalette2;
   public string iconSigilPalette1;
   public string iconSigilPalette2;

   // The list of people in the guild
   public UserInfo[] guildMembers;

   // The time at which the guild was created
   public long creationTime;

   #endregion

   public GuildInfo () { }

#if IS_SERVER_BUILD

   public GuildInfo (MySqlDataReader dataReader) {
      this.guildId = DataUtil.getInt(dataReader, "gldId"); ;
      this.guildName = DataUtil.getString(dataReader, "gldName");
      this.iconBorder = DataUtil.getString(dataReader, "gldIconBorder");
      this.iconBackground = DataUtil.getString(dataReader, "gldIconBackground");
      this.iconSigil = DataUtil.getString(dataReader, "gldIconSigil");
      this.iconBackPalette1 = DataUtil.getString(dataReader, "gldIconBackPalette1");
      this.iconBackPalette2 = DataUtil.getString(dataReader, "gldIconBackPalette2");
      this.iconSigilPalette1 = DataUtil.getString(dataReader, "gldIconSigilPalette1");
      this.iconSigilPalette2 = DataUtil.getString(dataReader, "gldIconSigilPalette2");
      this.creationTime = DataUtil.getDateTime(dataReader, "gldCreationTime").ToBinary();
   }

#endif

   public GuildInfo (string guildName, string iconBorder, string iconBackground,
      string iconSigil, string iconBackPalette1, string iconBackPalette2, string iconSigilPalette1,
      string iconSigilPalette2) {
      this.guildName = guildName;
      this.iconBorder = iconBorder;
      this.iconBackground = iconBackground;
      this.iconSigil = iconSigil;
      this.iconBackPalette1 = iconBackPalette1;
      this.iconBackPalette2 = iconBackPalette2;
      this.iconSigilPalette1 = iconSigilPalette1;
      this.iconSigilPalette2 = iconSigilPalette2;
   }

   public override bool Equals (object rhs) {
      if (rhs is GuildInfo) {
         var other = rhs as GuildInfo;
         return (guildId == other.guildId);
      }
      return false;
   }

   public override int GetHashCode () {
      return 17 + 31 * guildId.GetHashCode();
   }

   #region Private Variables

   #endregion
}
