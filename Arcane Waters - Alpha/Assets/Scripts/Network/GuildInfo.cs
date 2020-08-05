﻿using UnityEngine;
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
   public string iconBackPalettes;
   public string iconSigilPalettes;

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
      this.iconBackPalettes = DataUtil.getString(dataReader, "gldIconBackPalettes");
      this.iconSigilPalettes = DataUtil.getString(dataReader, "gldIconSigilPalettes");
      this.creationTime = DataUtil.getDateTime(dataReader, "gldCreationTime").ToBinary();
   }

#endif

   public GuildInfo (string guildName, string iconBorder, string iconBackground,
      string iconSigil, string iconBackPalettes, string iconSigilPalettes) {
      this.guildName = guildName;
      this.iconBorder = iconBorder;
      this.iconBackground = iconBackground;
      this.iconSigil = iconSigil;
      this.iconBackPalettes = iconBackPalettes;
      this.iconSigilPalettes = iconSigilPalettes;
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
